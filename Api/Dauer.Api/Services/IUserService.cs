using Dauer.Api.Model;

namespace Dauer.Api.Services;

public interface IUserService
{
  Task<User?> FindAsync(string? email);
  Task AddOrUpdate(User? user);
}