using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Primitives;

namespace Dauer.Ui.Desktop.Oidc;

public class LoopbackHttpListener : IDisposable
{
  private TimeSpan Timeout = TimeSpan.FromMinutes(5);
  private readonly string successHtml_;
  private readonly string errorHtml_;
  private readonly int port_ = 0;

  public string GetCallbackHtml(int port) => @"
<!DOCTYPE html>
<html>
<head>
  <style>
      body {
        background-color: #222;
        color: #eee;
        font-size: calc(1rem + 1vw);
      }

      h1 {
        font-size: calc(2rem + 1vw);
        margin-bottom: 1rem;
        text-align: center;
        color: #7981fe;
      }
  </style>
  <script>
      window.onload = function () {
          // Parse the URL fragment
          var fragment = window.location.hash.substring(1);
          var params = new URLSearchParams(fragment);

          // Construct the URL with query parameters" + "\n"
      + $"var url = new URL('http://localhost:{port}/auth/intercept');" + "\n"
      + @"url.search = params.toString();
          window.location = url.toString();

          // Send the parameters to the server
          fetch(url.toString())
              .then(response => response.text())
              .then(result => {
                  // Handle the server response here
                  console.log(result);
              })
              .catch(error => console.log('error', error));
      };
  </script>
</head>
<body>
  <h1>Logging in to FitEdit...</h1>
</body>
</html>
";

  private readonly IWebHost host_;
  private readonly TaskCompletionSource<string> source_ = new();

  public LoopbackHttpListener(string successHtml, string errorHtml, int port)
  {
    successHtml_ = successHtml;
    errorHtml_ = errorHtml;
    port_ = port;

    string urls = $"http://localhost:{port}/";

    host_ = new WebHostBuilder()
        .UseKestrel()
        .UseUrls(urls)
        .Configure(CompositionRoot.UseSupabase ? ConfigureForSupabase : ConfigureForCognito)
        .Build();
    host_.Start();
  }

  public void Dispose()
  {
    Task.Run(async () =>
    {
      await Task.Delay(500);
      host_.Dispose();
    });
  }

  private void ConfigureForSupabase(IApplicationBuilder app)
  {
    app.Map($"/auth/callback", app => app.Run(async ctx =>
    {
      ctx.Response.StatusCode = 200;
      ctx.Response.ContentType = "text/html";
      await ctx.Response.WriteAsync(GetCallbackHtml(port_));
      await ctx.Response.Body.FlushAsync();
    }));

    app.Map($"/auth/intercept", app => app.Run(async ctx =>
    {
      ctx.Response.StatusCode = 200;
      ctx.Response.ContentType = "text/html";
      await ctx.Response.WriteAsync(successHtml_);
      await ctx.Response.Body.FlushAsync();

      string? accessToken = null;
      string? refreshToken = null;

      if (ctx.Request.Query.TryGetValue("access_token", out StringValues at))
      {
        accessToken = at.FirstOrDefault()?.ToString();
      }

      if (ctx.Request.Query.TryGetValue("refresh_token", out StringValues rt))
      {
        refreshToken = rt.FirstOrDefault()?.ToString();
      }

      _ = Task.Run(() => source_.TrySetResult(JsonSerializer.Serialize(new { AccessToken = accessToken, RefreshToken = refreshToken })));
    }));
  }

  // For use with DesktopWebAuthenticator
  private void ConfigureForCognito(IApplicationBuilder app)
  { 
    app.Run(async ctx =>
    {
      if (ctx.Request == null) { return; }

      if (ctx.Request.Method == "GET")
      {
        await SetResultAsync(ctx.Request.QueryString.Value, ctx);
      }
      else if (ctx.Request.Method == "POST")
      {
        string? contentType = ctx.Response.ContentType;
        if (contentType == null) { return; }

        if (contentType.Equals("application/x-www-form-urlencoded", StringComparison.OrdinalIgnoreCase))
        {
          ctx.Response.StatusCode = 415;
        }
        else
        {
          using var sr = new StreamReader(ctx.Request.Body, Encoding.UTF8);
          var body = await sr.ReadToEndAsync();
          await SetResultAsync(body, ctx);
        }
      }
      else
      {
        ctx.Response.StatusCode = 405;
      }
    });
  }

  private async Task SetResultAsync(string? value, HttpContext ctx)
  {
    try
    {
      ctx.Response.StatusCode = 200;
      ctx.Response.ContentType = "text/html";
      await ctx.Response.WriteAsync(successHtml_);
      await ctx.Response.Body.FlushAsync();

      source_.TrySetResult(value ?? string.Empty);
    }
    catch (Exception ex)
    {
      Console.WriteLine(ex.ToString());

      ctx.Response.StatusCode = 400;
      ctx.Response.ContentType = "text/html";
      await ctx.Response.WriteAsync(errorHtml_);
      await ctx.Response.Body.FlushAsync();
    }
  }

  public Task<string> WaitForCallbackAsync(CancellationToken ct)
  {
    Task.Run(async () =>
    {
      try
      {
        await Task.Delay(Timeout, ct);
      }
      catch (TaskCanceledException)
      {
      }
      source_.TrySetCanceled();
    }, ct);

    return source_.Task;
  }
}
