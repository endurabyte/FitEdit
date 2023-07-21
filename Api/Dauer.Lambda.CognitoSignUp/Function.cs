using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Dauer.Lambda.CognitoSignUp;

public class Function
{
  private readonly HttpClient client_ = new();

  private readonly string? url_;
  private readonly string? key_;
  private readonly TimeSpan timeout_ = TimeSpan.FromSeconds(20);

  private readonly string? stageUrl_;
  private readonly string? stageKey_;
  private readonly TimeSpan stageTimeout_ = default; // Default: don't forward to stage

  public Function()
  {
    url_ = Environment.GetEnvironmentVariable("API_URL");
    key_ = Environment.GetEnvironmentVariable("API_KEY");
    timeout_ = TimeSpan.TryParse(Environment.GetEnvironmentVariable("TIMEOUT"), out TimeSpan timeout) 
      ? timeout 
      : timeout_;

    stageUrl_ = Environment.GetEnvironmentVariable("STAGE_API_URL");
    stageKey_ = Environment.GetEnvironmentVariable("STAGE_API_KEY");
    stageTimeout_ = TimeSpan.TryParse(Environment.GetEnvironmentVariable("STAGE_TIMEOUT"), out TimeSpan stageTimeout) 
      ? stageTimeout 
      : stageTimeout_;

    client_.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
  }

  public async Task<JsonElement> FunctionHandler(JsonElement elem, ILambdaContext context)
  {
    context.Logger.LogLine($"Cognito Pre sign-up lambda trigger: {elem}");

    await ForwardTo(url_, key_, timeout_, elem, context).ConfigureAwait(false);
    await ForwardTo(stageUrl_, stageKey_, stageTimeout_, elem, context).ConfigureAwait(false);
    return elem;
  }

  private async Task ForwardTo(string? url, string? key, TimeSpan timeout, JsonElement elem, ILambdaContext context)
  {
    if (url == null || key == null) { return; }
    if (timeout == default) { return; }

    var req = new HttpRequestMessage(HttpMethod.Post, url)
    {
      Content = new StringContent(elem.GetRawText(), Encoding.UTF8, "application/json")
    };
    req.Headers.Add("X-API-KEY", key);

    var cts = new CancellationTokenSource();
    cts.CancelAfter(timeout);
    HttpResponseMessage? response = await client_.SendAsync(req, cts.Token).ConfigureAwait(false);

    if (!response.IsSuccessStatusCode)
    {
      context.Logger.Log($"Failed to send Cognito event. Status code: {response.StatusCode}");
    }

    // throw an exception to deny the sign up
    //throw new Exception("FitEdit is still in development. Signups will be available soon!");
  }
}
