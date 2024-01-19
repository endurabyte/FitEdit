using System.Collections.Specialized;
using System.Net;
using System.Text;
using System.Text.Json;

namespace FitEdit.Ui.Infra.Authentication;

public class LoopbackHttpListener : IDisposable
{
  private readonly TimeSpan timeout_ = TimeSpan.FromMinutes(5);
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

  private readonly HttpListener listener_ = new();
  private readonly TaskCompletionSource<string> source_ = new();

  public LoopbackHttpListener(string successHtml, string errorHtml, int port)
  {
    successHtml_ = successHtml;
    errorHtml_ = errorHtml;
    port_ = port;

    string urls = $"http://localhost:{port}/";

    listener_.Prefixes.Add(urls);
    listener_.Start();

    // Start the listener task
    _ = Task.Run(ListenAsync);
  }

  public void Dispose()
  {
    listener_.Close();
  }

  private async Task ListenAsync()
  {
    while (listener_.IsListening)
    {
      try
      {
        HttpListenerContext context = await listener_.GetContextAsync();
        await HandleRequestAsync(context);
      }
      catch (HttpListenerException)
      {
      }
      catch (ObjectDisposedException)
      {
      }
    }
  }

  private async Task HandleRequestAsync(HttpListenerContext context)
  {
    if (context?.Request?.Url == null) { return; }
    string? path = context.Request.Url?.AbsolutePath;

    if (path == "/auth/callback")
    {
      await HandleCallback(context);
    }
    else if (path == "/auth/intercept")
    {
      await HandleIntercept(context);
    }

    context.Response.Close();
  }

  private async Task HandleCallback(HttpListenerContext context)
  {
    context.Response.StatusCode = 200;
    context.Response.ContentType = "text/html";
    await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(GetCallbackHtml(port_)));
    await context.Response.OutputStream.FlushAsync();
  }

  private async Task HandleIntercept(HttpListenerContext context)
  {
    context.Response.StatusCode = 200;
    context.Response.ContentType = "text/html";
    await context.Response.OutputStream.WriteAsync(Encoding.UTF8.GetBytes(successHtml_));
    await context.Response.OutputStream.FlushAsync();

    NameValueCollection query = context.Request.QueryString;
    string? accessToken = query.Get("access_token");
    string? refreshToken = query.Get("refresh_token");


    if (accessToken == null || refreshToken == null) { return; }

    _ = Task.Run(() => source_.TrySetResult(JsonSerializer.Serialize(new { AccessToken = accessToken, RefreshToken = refreshToken })));
  }

  public Task<string> WaitForCallbackAsync(CancellationToken ct)
  {
    Task.Run(async () =>
    {
      try
      {
        await Task.Delay(timeout_, ct);
      }
      catch (TaskCanceledException)
      {
      }
      source_.TrySetCanceled();
    }, ct);

    return source_.Task;
  }
}
