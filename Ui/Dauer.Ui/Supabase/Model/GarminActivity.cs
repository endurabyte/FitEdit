using Postgrest.Attributes;
using Postgrest.Models;

namespace Dauer.Ui.Supabase.Model;

public class GarminActivity : BaseModel
{
  /// <summary>
  /// NOT Garmin activity ID; our own independent ID
  /// </summary>
  [PrimaryKey]
  public string Id { get; set; } = "";

  [Column(nameof(Name))]
  public string? Name { get; set; }
  [Column(nameof(Description))]
  public string? Description { get; set; }
  [Column(nameof(Type))]
  public string? Type { get; set; }
  [Column(nameof(DeviceName))]
  public string? DeviceName { get; set; }
  [Column(nameof(StartTime))]
  public long StartTime { get; set; }
  [Column(nameof(Duration))]
  public long Duration { get; set; }
  // From ManualActivity
  [Column(nameof(Distance))]
  public double Distance { get; set; }
  // From ActivityFile, ActivitySummary, ManualActivity
  [Column(nameof(Manual))]
  public bool Manual { get; set; }

  [Column(nameof(FileType))]
  public string? FileType { get; set; }

  [Column(nameof(BucketUrl))]
  public string? BucketUrl { get; set; }

  [Column(nameof(SupabaseUserId))]
  public string? SupabaseUserId { get; set; }

  [Column(nameof(GarminId))]
  public long GarminId { get; set; }

  [Column(nameof(LastUpdated))]
  public DateTime? LastUpdated { get; set; }
}