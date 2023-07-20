using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Amazon.Lambda.Core;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace Dauer.Lambda.CognitoSignUp;

public class Function
{
  private readonly HttpClient client_ = new();
  private readonly string? apiUrl_;
  private readonly string? apiKey_;

  public Function()
  {
    apiUrl_ = Environment.GetEnvironmentVariable("API_URL");
    apiKey_ = Environment.GetEnvironmentVariable("API_KEY");

    LambdaLogger.Log($"API_URL: {apiUrl_}");
    LambdaLogger.Log($"API_KEY: {apiKey_}");

    client_.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
  }

  public async Task<JsonElement> FunctionHandler(JsonElement elem, ILambdaContext context)
  {
    context.Logger.LogLine($"Cognito Pre sign-up lambda trigger: {elem}");

    using var req = new HttpRequestMessage(HttpMethod.Post, apiUrl_);
    req.Content = new StringContent(elem.GetRawText(), Encoding.UTF8, "application/json");
    req.Headers.Add("X-API-KEY", apiKey_);

    HttpResponseMessage? response = await client_.SendAsync(req);
    if (!response.IsSuccessStatusCode)
    {
      context.Logger.Log($"Failed to send Cognito event. Status code: {response.StatusCode}");
    }

    // throw an exception to deny the sign up
    //throw new Exception("FitEdit is still in development. Signups will be available soon!");
    return elem;
  }
}
