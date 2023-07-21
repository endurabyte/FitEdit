using Dauer.Api.Model;

namespace Dauer.Api.Services;

public interface ICognitoService
{
  Task<bool> SignUpAsync(User user);
}
