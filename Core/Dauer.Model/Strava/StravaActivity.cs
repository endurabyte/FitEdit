#nullable enable
using System.Text.Json.Serialization;

namespace Dauer.Model.Strava;

public class StravaActivity
{
  [JsonPropertyName("id")]
  public long Id { get; set; }

  [JsonPropertyName("name")]
  public string? Name { get; set; }

  [JsonPropertyName("type")]
  public string? Type { get; set; }

  [JsonPropertyName("display_type")]
  public string? DisplayType { get; set; }

  [JsonPropertyName("activity_type_display_name")]
  public string? ActivityTypeDisplayName { get; set; }

  [JsonPropertyName("private")]
  public bool Private { get; set; }

  [JsonPropertyName("bike_id")]
  public long? BikeId { get; set; }

  [JsonPropertyName("athlete_gear_id")]
  public long? AthleteGearId { get; set; }

  [JsonPropertyName("start_date")]
  public string? StartDate { get; set; }

  [JsonPropertyName("start_date_local_raw")]
  public long StartDateLocalRaw { get; set; }

  [JsonPropertyName("start_time")]
  public string? StartTime { get; set; }

  [JsonPropertyName("start_day")]
  public string? StartDay { get; set; }

  [JsonPropertyName("distance")]
  public string? Distance { get; set; }

  [JsonPropertyName("distance_raw")]
  public double DistanceRaw { get; set; }

  [JsonPropertyName("long_unit")]
  public string? LongUnit { get; set; }

  [JsonPropertyName("short_unit")]
  public string? ShortUnit { get; set; }

  [JsonPropertyName("moving_time")]
  public string? MovingTime { get; set; }

  [JsonPropertyName("moving_time_raw")]
  public double MovingTimeRaw { get; set; }

  [JsonPropertyName("elapsed_time")]
  public string? ElapsedTime { get; set; }

  [JsonPropertyName("elapsed_time_raw")]
  public double ElapsedTimeRaw { get; set; }

  [JsonPropertyName("trainer")]
  public bool Trainer { get; set; }

  [JsonPropertyName("static_map")]
  public string? StaticMap { get; set; }

  [JsonPropertyName("has_latlng")]
  public bool HasLatLng { get; set; }

  [JsonPropertyName("commute")]
  public bool? Commute { get; set; }

  [JsonPropertyName("elevation_gain")]
  public string? ElevationGain { get; set; }

  [JsonPropertyName("elevation_unit")]
  public string? ElevationUnit { get; set; }

  [JsonPropertyName("elevation_gain_raw")]
  public double ElevationGainRaw { get; set; }

  [JsonPropertyName("description")]
  public string? Description { get; set; }

  [JsonPropertyName("activity_url")]
  public string? ActivityUrl { get; set; }

  [JsonPropertyName("activity_url_for_twitter")]
  public string? ActivityUrlForTwitter { get; set; }

  [JsonPropertyName("twitter_msg")]
  public string? TwitterMsg { get; set; }

  [JsonPropertyName("is_new")]
  public bool IsNew { get; set; }

  [JsonPropertyName("is_changing_type")]
  public bool IsChangingType { get; set; }

  [JsonPropertyName("suffer_score")]
  public int? SufferScore { get; set; }

  [JsonPropertyName("workout_type")]
  public int? WorkoutType { get; set; }

  [JsonPropertyName("flagged")]
  public bool Flagged { get; set; }

  [JsonPropertyName("hide_power")]
  public bool HidePower { get; set; }

  [JsonPropertyName("hide_heartrate")]
  public bool HideHeartRate { get; set; }

  [JsonPropertyName("leaderboard_opt_out")]
  public bool LeaderboardOptOut { get; set; }

  [JsonPropertyName("visibility")]
  public string? Visibility { get; set; }
}
