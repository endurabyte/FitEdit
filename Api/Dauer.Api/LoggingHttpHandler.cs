
/// <summary>
/// Intercept and log HTTP requests
/// </summary>
public class LoggingHttpHandler : DelegatingHandler
{
  public LoggingHttpHandler(HttpMessageHandler innerHandler)
      : base(innerHandler)
  {
  }

  protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
  {
    Console.WriteLine($"Request: {request}");
    if (request.Content != null)
    {
      Console.WriteLine(await request.Content.ReadAsStringAsync());
    }

    var response = await base.SendAsync(request, cancellationToken);

    Console.WriteLine($"Response: {response}");
    if (response.Content != null)
    {
      Console.WriteLine(await response.Content.ReadAsStringAsync());
    }

    return response;
  }
}

