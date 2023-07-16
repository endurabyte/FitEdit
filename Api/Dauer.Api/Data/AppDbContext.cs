using Microsoft.EntityFrameworkCore;

namespace Dauer.Api.Data;

public class AppDbContext : DbContext
{
  public DbSet<User> User { get; set; }

  public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
  {
  }

  public async Task InitAsync() => await Database.EnsureCreatedAsync().ConfigureAwait(false);
}
