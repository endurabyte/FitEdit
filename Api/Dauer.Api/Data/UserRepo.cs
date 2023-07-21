using Dauer.Api.Model;

namespace Dauer.Api.Data;

public class UserRepo : IUserRepo
{
  private readonly ILogger<UserRepo> log_;
  private readonly AppDbContext db_;

  public UserRepo(ILogger<UserRepo> log, AppDbContext db)
  {
    log_ = log;
    db_ = db;
  }

  /// <summary>
  /// Add or update the given user.
  /// We assume only the email is given, and we need to look up the rest of the user's information.
  /// We can get a new user from Cognito or Stripe. Either way, it could already exist. 
  /// </summary>
  public async Task AddOrUpdateAsync(Model.User user)
  {
    User? entity = user.MapEntity();
    User? existing = await FindUserAsync(entity.Email).ConfigureAwait(false);

    if (existing != null)
    {
      log_.LogInformation("Merging existing user {existing} with {user}", existing, user);

      existing.Merge(entity);
      db_.User.Update(existing);
    }
    else
    {
      db_.User.Add(user.MapEntity());
    }

    await db_.SaveChangesAsync().ConfigureAwait(false);
  }

  public Task<Model.User?> FindAsync(string? email)
  {
    var existing = email == null 
      ? null 
      : db_.User.FirstOrDefault(u => u.Email != null && u.Email.ToLower() == email.ToLower());
    return Task.FromResult(existing?.MapModel());
  }


  private Task<User?> FindUserAsync(string? email)
  {
    var existing = email == null 
      ? null 
      : db_.User.FirstOrDefault(u => u.Email != null && u.Email.ToLower() == email.ToLower());
    return Task.FromResult(existing);
  }

  public bool Exists(string email) => db_.Set<Data.User>().Any(u => u.Email == email);
}