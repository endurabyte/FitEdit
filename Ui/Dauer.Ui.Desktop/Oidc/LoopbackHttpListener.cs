using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;

namespace Dauer.Ui.Desktop.Oidc;

public class LoopbackHttpListener : IDisposable
{
  private TimeSpan Timeout = TimeSpan.FromMinutes(5);
  private readonly string successHtml_;
  private readonly string errorHtml_;

  private readonly IWebHost host_;
  private readonly TaskCompletionSource<string> source_ = new();

  public LoopbackHttpListener(string successHtml, string errorHtml, int port, string? path = null)
  {
    successHtml_ = successHtml;
    errorHtml_ = errorHtml;

    path ??= string.Empty;

    if (path.StartsWith("/")) path = path[1..];

    string urls = $"http://localhost:{port}/{path}";

    host_ = new WebHostBuilder()
        .UseKestrel()
        .UseUrls(urls)
        .Configure(Configure)
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

  private void Configure(IApplicationBuilder app)
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

  public Task<string> WaitForCallbackAsync()
  {
    Task.Run(async () =>
    {
      await Task.Delay(Timeout);
      source_.TrySetCanceled();
    });

    return source_.Task;
  }
}
