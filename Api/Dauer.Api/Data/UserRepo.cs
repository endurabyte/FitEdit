namespace Dauer.Api.Data;

public interface IUserRepo
{
  Task AddAsync(User user);
}

public class UserRepo : IUserRepo
{
  private readonly AppDbContext db_;

  public UserRepo(AppDbContext db)
  {
    db_ = db;
  }

  public async Task AddAsync(User user)
  {
    db_.User.Add(user);
    await db_.SaveChangesAsync().ConfigureAwait(false);
  }
}