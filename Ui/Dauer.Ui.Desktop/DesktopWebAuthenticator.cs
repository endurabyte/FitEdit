using Dauer.Model;

namespace Dauer.Ui.Desktop;

public class DesktopWebAuthenticator : IWebAuthenticator
{
  public Task AuthenticateAsync()
  {
    Log.Info($"{nameof(DesktopWebAuthenticator)}.{nameof(AuthenticateAsync)}");

    //_ = Task.Run(async () =>
    //{
    //  string username = "dougslater@gmail.com";
    //  var client = new HttpClient();
    //  var request = new HttpRequestMessage(HttpMethod.Get, $"https://localhost:7117/Auth?username={username}");

    //  try
    //  {
    //    var response = await client.SendAsync(request);
    //    await Log($"Got response {response.StatusCode}");
    //    string responseContent = await response.Content.ReadAsStringAsync();
    //    await Log(responseContent);
    //  }
    //  catch (Exception e)
    //  {
    //    await Log($"{e}");
    //  }
    //});
    return Task.CompletedTask;
  }
}
