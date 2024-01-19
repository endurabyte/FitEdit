using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

/// <summary>
/// Metadata
/// </summary>
public class Metadata
{
  /// <summary>
  /// Gets or sets a value indicating whether this instance is original.
  /// </summary>
  /// <value>
  ///   <c>true</c> if this instance is original; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("isOriginal")]
  public bool IsOriginal { get; set; }

  /// <summary>
  /// Gets or sets the device application installation identifier.
  /// </summary>
  /// <value>
  /// The device application installation identifier.
  /// </value>
  [JsonPropertyName("deviceApplicationInstallationId")]
  public long DeviceApplicationInstallationId { get; set; }

  /// <summary>
  /// Gets or sets the agent application installation identifier.
  /// </summary>
  /// <value>
  /// The agent application installation identifier.
  /// </value>
  [JsonPropertyName("agentApplicationInstallationId")]
  public object AgentApplicationInstallationId { get; set; }

  /// <summary>
  /// Gets or sets the agent string.
  /// </summary>
  /// <value>
  /// The agent string.
  /// </value>
  [JsonPropertyName("agentString")]
  public object AgentString { get; set; }

  /// <summary>
  /// Gets or sets the file format.
  /// </summary>
  /// <value>
  /// The file format.
  /// </value>
  [JsonPropertyName("fileFormat")]
  public FileFormat FileFormat { get; set; }

  /// <summary>
  /// Gets or sets the associated course identifier.
  /// </summary>
  /// <value>
  /// The associated course identifier.
  /// </value>
  [JsonPropertyName("associatedCourseId")]
  public object AssociatedCourseId { get; set; }

  /// <summary>
  /// Gets or sets the last update date.
  /// </summary>
  /// <value>
  /// The last update date.
  /// </value>
  [JsonPropertyName("lastUpdateDate")]
  public DateTimeOffset LastUpdateDate { get; set; }

  /// <summary>
  /// Gets or sets the uploaded date.
  /// </summary>
  /// <value>
  /// The uploaded date.
  /// </value>
  [JsonPropertyName("uploadedDate")]
  public DateTimeOffset UploadedDate { get; set; }

  /// <summary>
  /// Gets or sets the video URL.
  /// </summary>
  /// <value>
  /// The video URL.
  /// </value>
  [JsonPropertyName("videoUrl")]
  public object VideoUrl { get; set; }

  /// <summary>
  /// Gets or sets the has polyline.
  /// </summary>
  /// <value>
  /// The has polyline.
  /// </value>
  [JsonPropertyName("hasPolyline")]
  public bool? HasPolyline { get; set; }

  /// <summary>
  /// Gets or sets the has chart data.
  /// </summary>
  /// <value>
  /// The has chart data.
  /// </value>
  [JsonPropertyName("hasChartData")]
  public bool? HasChartData { get; set; }

  /// <summary>
  /// Gets or sets the has hr time in zones.
  /// </summary>
  /// <value>
  /// The has hr time in zones.
  /// </value>
  [JsonPropertyName("hasHrTimeInZones")]
  public bool? HasHrTimeInZones { get; set; }

  /// <summary>
  /// Gets or sets the has power time in zones.
  /// </summary>
  /// <value>
  /// The has power time in zones.
  /// </value>
  [JsonPropertyName("hasPowerTimeInZones")]
  public bool? HasPowerTimeInZones { get; set; }

  /// <summary>
  /// Gets or sets the user information dto.
  /// </summary>
  /// <value>
  /// The user information dto.
  /// </value>
  [JsonPropertyName("userInfoDto")]
  public UserInfo UserInfoDto { get; set; }

  /// <summary>
  /// Gets or sets the chart availability.
  /// </summary>
  /// <value>
  /// The chart availability.
  /// </value>
  [JsonPropertyName("chartAvailability")]
  public ChartAvailability ChartAvailability { get; set; }

  /// <summary>
  /// Gets or sets the child ids.
  /// </summary>
  /// <value>
  /// The child ids.
  /// </value>
  [JsonPropertyName("childIds")]
  public object[] ChildIds { get; set; }

  /// <summary>
  /// Gets or sets the sensors.
  /// </summary>
  /// <value>
  /// The sensors.
  /// </value>
  [JsonPropertyName("sensors")]
  public Sensor[] Sensors { get; set; }

  /// <summary>
  /// Gets or sets the activity images.
  /// </summary>
  /// <value>
  /// The activity images.
  /// </value>
  [JsonPropertyName("activityImages")]
  public ActivityImage[] ActivityImages { get; set; }

  /// <summary>
  /// Gets or sets the manufacturer.
  /// </summary>
  /// <value>
  /// The manufacturer.
  /// </value>
  [JsonPropertyName("manufacturer")]
  public string Manufacturer { get; set; }

  /// <summary>
  /// Gets or sets the dive number.
  /// </summary>
  /// <value>
  /// The dive number.
  /// </value>
  [JsonPropertyName("diveNumber")]
  public object DiveNumber { get; set; }

  /// <summary>
  /// Gets or sets the lap count.
  /// </summary>
  /// <value>
  /// The lap count.
  /// </value>
  [JsonPropertyName("lapCount")]
  public long LapCount { get; set; }

  /// <summary>
  /// Gets or sets the associated workout identifier.
  /// </summary>
  /// <value>
  /// The associated workout identifier.
  /// </value>
  [JsonPropertyName("associatedWorkoutId")]
  public object AssociatedWorkoutId { get; set; }

  /// <summary>
  /// Gets or sets the is atp activity.
  /// </summary>
  /// <value>
  /// The is atp activity.
  /// </value>
  [JsonPropertyName("isAtpActivity")]
  public object IsAtpActivity { get; set; }

  /// <summary>
  /// Gets or sets the device meta data.
  /// </summary>
  /// <value>
  /// The device meta data.
  /// </value>
  [JsonPropertyName("deviceMetaDataDTO")]
  public DeviceMetaData DeviceMetaData { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether this <see cref="Metadata"/> is GCJ02.
  /// </summary>
  /// <value>
  ///   <c>true</c> if GCJ02; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("gcj02")]
  public bool Gcj02 { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [automatic calculate calories].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [automatic calculate calories]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("autoCalcCalories")]
  public bool AutoCalcCalories { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether this <see cref="Metadata"/> is favorite.
  /// </summary>
  /// <value>
  ///   <c>true</c> if favorite; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("favorite")]
  public bool Favorite { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [elevation corrected].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [elevation corrected]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("elevationCorrected")]
  public bool ElevationCorrected { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [personal record].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [personal record]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("personalRecord")]
  public bool PersonalRecord { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether [manual activity].
  /// </summary>
  /// <value>
  ///   <c>true</c> if [manual activity]; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("manualActivity")]
  public bool ManualActivity { get; set; }
}