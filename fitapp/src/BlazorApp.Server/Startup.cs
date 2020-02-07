using AutoMapper;
using BlazorApp.Server.Authorization;
using BlazorApp.Server.Data;
using BlazorApp.Server.Data.Interfaces;
using BlazorApp.Server.Data.Mapping;
using BlazorApp.Server.Helpers;
using BlazorApp.Server.Middleware;
using BlazorApp.Server.Models;
using BlazorApp.Server.Services;
using BlazorApp.Shared.AuthorizationDefinitions;
using Certes;
using FluffySpoon.AspNet.LetsEncrypt;
using FluffySpoon.AspNet.LetsEncrypt.Certes;
using IdentityServer4;
using IdentityServer4.AccessTokenValidation;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;

namespace BlazorApp.Server
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        private readonly IWebHostEnvironment _environment;

        public Startup(IConfiguration configuration, IWebHostEnvironment env)
        {
            Configuration = configuration;
            _environment = env;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });

            services.AddDbContext<ApplicationDbContext>(BuildDbContextOptions);

            AddLetsEncrypt(services);
            AddIdentity(services);
            AddCookies(services);

            services.AddControllers().AddNewtonsoftJson();
            services.AddSignalR();

            services.AddHttpsRedirection(options =>
            {
                options.RedirectStatusCode = StatusCodes.Status308PermanentRedirect;
                options.HttpsPort = 443;
            });
           
            services.AddSwaggerDocument(config =>
            {
                config.PostProcess = document =>
                {
                    document.Info.Version = "v0.2.3";
                    document.Info.Title = "Blazor Boilerplate";
                    document.Info.Description = "Blazor Boilerplate / Starter Template using the  (ASP.NET Core Hosted) (dotnet new blazorhosted) model. Hosted by an ASP.NET Core server";
                };
            });

            services.AddResponseCompression(opts =>
            {
                opts.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(
                    new[] { "application/octet-stream" });
            });

            services.AddSingleton<IHttpContextAccessor, HttpContextAccessor>();
            services.AddScoped<IUserSession, UserSession>();
            services.AddSingleton<IEmailConfiguration>(Configuration.GetSection("EmailConfiguration").Get<EmailConfiguration>());
            services.AddTransient<IAccountService, AccountService>();
            services.AddTransient<IEmailService, EmailService>();
            services.AddTransient<IUserProfileService, UserProfileService>();
            services.AddTransient<IApiLogService, ApiLogService>();
            services.AddTransient<ITodoService, ToDoService>();
            services.AddTransient<IMessageService, MessageService>();

            // DB Creation and Seeding
            services.AddTransient<IDatabaseInitializer, DatabaseInitializer>();

            //Automapper to map DTO to Models https://www.c-sharpcorner.com/UploadFile/1492b1/crud-operations-using-automapper-in-mvc-application/
            var automapperConfig = new MapperConfiguration(configuration =>
            {
                configuration.AddProfile(new MappingProfile());
            });

            var autoMapper = automapperConfig.CreateMapper();

            services.AddSingleton(autoMapper);
        }

        private void AddLetsEncrypt(IServiceCollection services)
        {
            services.AddFluffySpoonLetsEncryptRenewalService(new LetsEncryptOptions()
            {
                Email = "support@sltr.us",
                UseStaging = _environment.IsDevelopment(),
                Domains = new[] { Configuration["Domain"] },
                TimeUntilExpiryBeforeRenewal = TimeSpan.FromDays(30),
                TimeAfterIssueDateBeforeRenewal = TimeSpan.FromDays(7),
                CertificateSigningRequest = new CsrInfo()
                {
                    CountryName = "USA",
                    Locality = "US",
                    Organization = "SLTR",
                    OrganizationUnit = "Support",
                    State = "TN"
                }
            });
            services.AddFluffySpoonLetsEncryptFileCertificatePersistence();
            services.AddFluffySpoonLetsEncryptFileChallengePersistence();
        }

        private void AddIdentity(IServiceCollection services)
        {
            services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
            {
                options.User.AllowedUserNameCharacters = String.Empty;
                options.User.RequireUniqueEmail = true;
            })
                .AddRoles<IdentityRole<Guid>>()
                .AddEntityFrameworkStores<ApplicationDbContext>()
                .AddDefaultTokenProviders();

            services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>,
                AdditionalUserClaimsPrincipalFactory>();

            var authAuthority = $"https://{Configuration["Domain"]}".TrimEnd('/');

            // Adds IdentityServer
            var identityServerBuilder = services.AddIdentityServer(options =>
            {
                options.IssuerUri = authAuthority;
                options.Events.RaiseErrorEvents = true;
                options.Events.RaiseInformationEvents = true;
                options.Events.RaiseFailureEvents = true;
                options.Events.RaiseSuccessEvents = true;
            })
              .AddConfigurationStore(options =>
              {
                  options.ConfigureDbContext = builder => BuildDbContextOptions(builder);
              })
              .AddOperationalStore(options =>
              {
                  options.ConfigureDbContext = builder => BuildDbContextOptions(builder);

                  // this enables automatic token cleanup. this is optional.
                  options.EnableTokenCleanup = true;
                  options.TokenCleanupInterval = 3600; //In Seconds 1 hour
              })
              .AddAspNetIdentity<ApplicationUser>();

            //identityServerBuilder.AddSigningCredential(LetsEncryptRenewalService.Certificate);


            var authBuilder = services.AddAuthentication(options =>
            {
                options.DefaultScheme = IdentityServerAuthenticationDefaults.AuthenticationScheme;
            })
            .AddIdentityServerAuthentication(options =>
            {
                options.Authority = authAuthority;
                options.SupportedTokens = SupportedTokens.Jwt;
                options.RequireHttpsMetadata = true;
                options.ApiName = IdentityServerConfig.ApiName;
            });

            //https://docs.microsoft.com/en-us/aspnet/core/security/authentication/social/google-logins?view=aspnetcore-3.1
            if (Convert.ToBoolean(Configuration["ExternalAuthProviders:Google:Enabled"] ?? "false"))
            {
                authBuilder.AddGoogle(options =>
                {
                    options.SignInScheme = IdentityServerConstants.ExternalCookieAuthenticationScheme;

                    options.ClientId = Configuration["ExternalAuthProviders:Google:ClientId"];
                    options.ClientSecret = Configuration["ExternalAuthProviders:Google:ClientSecret"];
                });
            }

            services.Configure<ApiBehaviorOptions>(options => { options.SuppressModelStateInvalidFilter = true; });

            //Add Policies / Claims / Authorization - https://stormpath.com/blog/tutorial-policy-based-authorization-asp-net-core
            services.AddAuthorization(options =>
            {
                options.AddPolicy(Policies.IsAdmin, Policies.IsAdminPolicy());
                options.AddPolicy(Policies.IsUser, Policies.IsUserPolicy());
                options.AddPolicy(Policies.IsReadOnly, Policies.IsReadOnlyPolicy());
                options.AddPolicy(Policies.IsMyDomain, Policies.IsMyDomainPolicy());  // valid only on serverside operations
            });

            services.AddTransient<IAuthorizationHandler, DomainRequirementHandler>();

            services.Configure<IdentityOptions>(options =>
            {
                // Password settings
                options.Password.RequireDigit = false;
                options.Password.RequiredLength = 6;
                options.Password.RequireNonAlphanumeric = false;
                options.Password.RequireUppercase = false;
                options.Password.RequireLowercase = false;
                //options.Password.RequiredUniqueChars = 6;

                // Lockout settings
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(30);
                options.Lockout.MaxFailedAccessAttempts = 10;
                options.Lockout.AllowedForNewUsers = true;

                // Require Confirmed Email User settings
                if (Convert.ToBoolean(Configuration["BlazorApp:RequireConfirmedEmail"] ?? "false"))
                {
                    options.User.RequireUniqueEmail = false;
                    options.SignIn.RequireConfirmedEmail = true;
                }
            });
        }

        private static void AddCookies(IServiceCollection services)
        {
            services.ConfigureApplicationCookie(options =>
            {
                options.Cookie.HttpOnly = false;

                // Suppress redirect on API URLs in ASP.NET Core -> https://stackoverflow.com/a/56384729/54159
                options.Events = new CookieAuthenticationEvents()
                {
                    OnRedirectToAccessDenied = context =>
                    {
                        if (context.Request.Path.StartsWithSegments("/api"))
                        {
                            context.Response.StatusCode = (int)(HttpStatusCode.Unauthorized);
                        }

                        return Task.CompletedTask;
                    },
                    OnRedirectToLogin = context =>
                    {
                        context.Response.StatusCode = 401;
                        return Task.CompletedTask;
                    }
                };
            });
        }

        private void BuildDbContextOptions(DbContextOptionsBuilder builder)
        {
            var migrationsAssembly = typeof(Startup).GetTypeInfo().Assembly.GetName().Name;
            var useSqlServer = Convert.ToBoolean(Configuration["BlazorApp:UseSqlServer"] ?? "false");
            var dbConnString = useSqlServer
                ? Configuration.GetConnectionString("DefaultConnection")
                : $"Filename={Configuration.GetConnectionString("SqlLiteConnectionFileName")}";

            if (useSqlServer)
            {
                builder.UseSqlServer(dbConnString, sql => sql.MigrationsAssembly(migrationsAssembly));
            }
            else if (Convert.ToBoolean(Configuration["BlazorApp:UsePostgresServer"] ?? "true"))
            {
                builder.UseNpgsql(Configuration.GetConnectionString("PostgresConnection"), options => options.MigrationsAssembly(migrationsAssembly));
            }
            else
            {
                builder.UseSqlite(dbConnString, sql => sql.MigrationsAssembly(migrationsAssembly));
            }
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            EmailTemplates.Initialize(env);
            
            app.UseFluffySpoonLetsEncryptChallengeApprovalMiddleware();
            app.UseResponseCompression(); // This must be before the other Middleware if that manipulates Response

            // A REST API global exception handler and response wrapper for a consistent API
            // Configure API Loggin in appsettings.json - Logs most API calls. Great for debugging and user activity audits
            app.UseMiddleware<APIResponseRequestLoggingMiddleware>(Convert.ToBoolean(Configuration["BlazorApp:EnableAPILogging:Enabled"] ?? "true"));

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseBlazorDebugging();
            }
            else
            {
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts(); //HSTS Middleware (UseHsts) to send HTTP Strict Transport Security Protocol (HSTS) headers to clients.
            }

            app.UseStaticFiles();
            app.UseClientSideBlazorFiles<Client.Startup>();

            //app.UseHttpsRedirection(); // Redundant with AddHttpsRedirection
            app.UseRouting();
            //app.UseAuthentication(); //Removed for IS4
            app.UseIdentityServer();
            app.UseAuthorization();

            //Must be AFTER the Auth middleware to get the User/Identity info
            app.UseMiddleware<UserSessionMiddleware>();

            // NSwag
            app.UseOpenApi();
            app.UseSwaggerUi3();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapDefaultControllerRoute();
                endpoints.MapControllers();
                // new SignalR endpoint routing setup
                endpoints.MapHub<Hubs.ChatHub>("/chathub");
                endpoints.MapFallbackToClientSideBlazor<Client.Startup>("index.html");
            });
        }
    }
}
