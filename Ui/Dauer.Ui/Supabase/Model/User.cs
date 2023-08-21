using Postgrest.Attributes;
using Postgrest.Models;

namespace Dauer.Ui.Supabase.Model;

public class User : BaseModel
{
  [PrimaryKey]
  public string Id { get; set; } = "";
  [Column(nameof(Name))]
  public string? Name { get; set; }
  [Column(nameof(Email))]
  public string? Email { get; set; }
  [Column(nameof(Phone))]
  public string? Phone { get; set; }
  [Column(nameof(IsActive))]
  public bool? IsActive { get; set; }
}