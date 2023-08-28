using Dauer.Model;

namespace Dauer.Ui.Infra.Authentication;

public class LoginRedirectContent
{
  private readonly string successDefaultHtml_ = "<h1>Login success. You can close this window</h1>";
  private readonly string successHtmlFile_ = "https://www.fitedit.io/login-success.html";
  public string SuccessHtml { get; set; }

  private readonly string errorDefaultHtml_ = "<h1>There was an error.</h1>";
  private readonly string errorHtmlFile_ = "https://www.fitedit.io/login-error.html";
  public string ErrorHtml { get; set; }

  public LoginRedirectContent()
  {
    SuccessHtml = successDefaultHtml_;
    ErrorHtml = errorDefaultHtml_;
  }

  public async Task<LoginRedirectContent> LoadContentAsync(CancellationToken ct)
  {
    // TODO reuse client
    var client = new HttpClient();

    try
    {
      SuccessHtml = await client.GetStringAsync(successHtmlFile_, ct);
      ErrorHtml = await client.GetStringAsync(errorHtmlFile_, ct);
    }
    catch (Exception e)
    {
      Log.Error(e);
    }

    return this;
  }

}
