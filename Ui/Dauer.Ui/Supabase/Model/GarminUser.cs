using Postgrest.Attributes;
using Postgrest.Models;

namespace Dauer.Ui.Supabase.Model;

public class GarminUser : BaseModel
{
  [PrimaryKey]
  public string Id { get; set; } = "";
  [Column("AccessToken")]
  public string? AccessToken { get; set; }
}