namespace Dauer.Api.Data;

public interface IUserRepo
{
  bool Exists(string email);
  Task<Model.User?> FindAsync(string? email);
  Task AddOrUpdateAsync(Model.User user);
}
