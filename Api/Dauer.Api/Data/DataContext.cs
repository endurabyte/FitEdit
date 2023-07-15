using Microsoft.EntityFrameworkCore;

namespace Dauer.Api.Data;

public class DataContext : DbContext
{
  protected readonly IConfiguration Configuration;

  public DbSet<User> Users { get; set; }

  public DataContext(IConfiguration configuration)
  {
    Configuration = configuration;
  }

  protected override void OnConfiguring(DbContextOptionsBuilder options)
  {
    options.UseNpgsql(Configuration.GetConnectionString("Default"));
  }
}

public class User
{
  public string? Id { get; set; }
}