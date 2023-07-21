using System.Net;
using System.Text.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using static SendGrid.BaseClient;

namespace Dauer.Api.Services;

public class SendGridEmailService : IEmailService
{
  private readonly SendGridClient client_;
  private readonly ILogger<SendGridEmailService> log_;
  private static string customerListId_ = "283e5fbc-3af8-4e65-87eb-cd10d4f0d22c";

  public SendGridEmailService(SendGridClient client, ILogger<SendGridEmailService> log)
  {
    client_ = client;
    log_ = log;
  }

  public async Task<bool> AddContactAsync(string email)
  {
    var obj = new
    {
      list_ids = new[] { customerListId_ },
      contacts = new object[]
      {
        new
        {
          email,
        }
      }
    };

    var response = await client_.RequestAsync(
        method: Method.PUT,
        urlPath: "marketing/contacts",
        requestBody: JsonSerializer.Serialize(obj)
    );

    bool ok = response.StatusCode == HttpStatusCode.Accepted;
      
    if (ok)
    {
      log_.LogInformation($"Added contact {email}");
    }
    else
    {
      string respBody = await response.Body.ReadAsStringAsync();
      log_.LogError($"Could not add contact {email}: {response.StatusCode} {respBody}");
    }

    return ok;
  }

  public async Task<bool> SendEmailAsync(string to, string subject, string body)
  {

    var fromAddr = new EmailAddress("support@fitedit.io", "FitEdit");
    var toAddr = new EmailAddress(to);
    var msg = MailHelper.CreateSingleEmail(fromAddr, toAddr, subject, body, body);
    var response = await client_.SendEmailAsync(msg);

    bool ok = response.StatusCode == HttpStatusCode.Accepted;
      
    if (ok)
    {
      log_.LogInformation($"Sent email to {to}: {subject} {body}");
    }
    else
    {
      string respBody = await response.Body.ReadAsStringAsync();
      log_.LogError($"Could not send email to {to}: {response.StatusCode} {respBody}");
    }

    return ok;
  }
}
