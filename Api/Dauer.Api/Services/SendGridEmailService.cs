using System.Net;
using SendGrid;
using SendGrid.Helpers.Mail;

namespace Dauer.Api.Services;

public class SendGridEmailService : IEmailService
{
  private readonly string apiKey_;
  private readonly ILogger<SendGridEmailService> log_;

  public SendGridEmailService(string apiKey, ILogger<SendGridEmailService> log)
  {
    apiKey_ = apiKey;
    log_ = log;
  }

  public async Task SendEmailAsync(string to, string subject, string body)
  {
    var client = new SendGridClient(apiKey_);

    var fromAddr = new EmailAddress("support@fitedit.io", "FitEdit");
    var toAddr = new EmailAddress(to);
    var msg = MailHelper.CreateSingleEmail(fromAddr, toAddr, subject, body, body);
    var response = await client.SendEmailAsync(msg);

    if (response.StatusCode == HttpStatusCode.Accepted || response.StatusCode == HttpStatusCode.OK)
    {
      log_.LogInformation($"Sent email to {to}: {subject} {body}");
      return;
    }

    string respBody = await response.Body.ReadAsStringAsync();
    log_.LogError($"Could not send email to {to}: {response.StatusCode} {respBody}");
  }
}
