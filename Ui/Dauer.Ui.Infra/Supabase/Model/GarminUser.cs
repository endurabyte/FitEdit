using Postgrest.Attributes;
using Postgrest.Models;

namespace Dauer.Ui.Infra.Supabase.Model;

public class GarminUser : BaseModel
{
  [PrimaryKey]
  public string Id { get; set; } = "";
  [Column(nameof(AccessToken))]
  public string? AccessToken { get; set; }
}