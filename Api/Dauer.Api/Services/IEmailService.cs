using Dauer.Api.Model;

namespace Dauer.Api.Services;

public interface IEmailService
{
  Task<bool> AddContactAsync(User user);
  Task<bool> SendEmailAsync(string to, string subject, string body);
}
