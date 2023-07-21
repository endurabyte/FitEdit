namespace Dauer.Api.Services;

public interface IEmailService
{
  Task<bool> AddContactAsync(string email);
  Task<bool> SendEmailAsync(string to, string subject, string body);
}
