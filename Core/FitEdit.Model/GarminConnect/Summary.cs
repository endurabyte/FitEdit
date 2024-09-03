using System.Text.Json.Serialization;

namespace FitEdit.Model.GarminConnect;

/// <summary>
/// Summary
/// </summary>
public class Summary
{
  /// <summary>
  /// Gets or sets the start time local.
  /// </summary>
  /// <value>
  /// The start time local.
  /// </value>
  [JsonPropertyName("startTimeLocal")]
  public DateTimeOffset StartTimeLocal { get; set; }

  /// <summary>
  /// Gets or sets the start time GMT.
  /// </summary>
  /// <value>
  /// The start time GMT.
  /// </value>
  [JsonPropertyName("startTimeGMT")]
  public DateTimeOffset StartTimeGmt { get; set; }

  /// <summary>
  /// Gets or sets the start latitude.
  /// </summary>
  /// <value>
  /// The start latitude.
  /// </value>
  [JsonPropertyName("startLatitude")]
  public double StartLatitude { get; set; }

  /// <summary>
  /// Gets or sets the start longitude.
  /// </summary>
  /// <value>
  /// The start longitude.
  /// </value>
  [JsonPropertyName("startLongitude")]
  public double StartLongitude { get; set; }

  /// <summary>
  /// Gets or sets the distance.
  /// </summary>
  /// <value>
  /// The distance.
  /// </value>
  [JsonPropertyName("distance")]
  public long Distance { get; set; }

  /// <summary>
  /// Gets or sets the duration.
  /// </summary>
  /// <value>
  /// The duration.
  /// </value>
  [JsonPropertyName("duration")]
  public double Duration { get; set; }

  /// <summary>
  /// Gets or sets the duration of the moving.
  /// </summary>
  /// <value>
  /// The duration of the moving.
  /// </value>
  [JsonPropertyName("movingDuration")]
  public long MovingDuration { get; set; }

  /// <summary>
  /// Gets or sets the duration of the elapsed.
  /// </summary>
  /// <value>
  /// The duration of the elapsed.
  /// </value>
  [JsonPropertyName("elapsedDuration")]
  public double ElapsedDuration { get; set; }

  /// <summary>
  /// Gets or sets the elevation gain.
  /// </summary>
  /// <value>
  /// The elevation gain.
  /// </value>
  [JsonPropertyName("elevationGain")]
  public long ElevationGain { get; set; }

  /// <summary>
  /// Gets or sets the elevation loss.
  /// </summary>
  /// <value>
  /// The elevation loss.
  /// </value>
  [JsonPropertyName("elevationLoss")]
  public long ElevationLoss { get; set; }

  /// <summary>
  /// Gets or sets the maximum elevation.
  /// </summary>
  /// <value>
  /// The maximum elevation.
  /// </value>
  [JsonPropertyName("maxElevation")]
  public double MaxElevation { get; set; }

  /// <summary>
  /// Gets or sets the minimum elevation.
  /// </summary>
  /// <value>
  /// The minimum elevation.
  /// </value>
  [JsonPropertyName("minElevation")]
  public double MinElevation { get; set; }

  /// <summary>
  /// Gets or sets the average speed.
  /// </summary>
  /// <value>
  /// The average speed.
  /// </value>
  [JsonPropertyName("averageSpeed")]
  public double AverageSpeed { get; set; }

  /// <summary>
  /// Gets or sets the average moving speed.
  /// </summary>
  /// <value>
  /// The average moving speed.
  /// </value>
  [JsonPropertyName("averageMovingSpeed")]
  public double AverageMovingSpeed { get; set; }

  /// <summary>
  /// Gets or sets the maximum speed.
  /// </summary>
  /// <value>
  /// The maximum speed.
  /// </value>
  [JsonPropertyName("maxSpeed")]
  public double MaxSpeed { get; set; }

  /// <summary>
  /// Gets or sets the calories.
  /// </summary>
  /// <value>
  /// The calories.
  /// </value>
  [JsonPropertyName("calories")]
  public double Calories { get; set; }

  /// <summary>
  /// Gets or sets the average hr.
  /// </summary>
  /// <value>
  /// The average hr.
  /// </value>
  [JsonPropertyName("averageHR")]
  public long AverageHr { get; set; }

  /// <summary>
  /// Gets or sets the maximum hr.
  /// </summary>
  /// <value>
  /// The maximum hr.
  /// </value>
  [JsonPropertyName("maxHR")]
  public long MaxHr { get; set; }

  /// <summary>
  /// Gets or sets the average run cadence.
  /// </summary>
  /// <value>
  /// The average run cadence.
  /// </value>
  [JsonPropertyName("averageRunCadence")]
  public double AverageRunCadence { get; set; }

  /// <summary>
  /// Gets or sets the maximum run cadence.
  /// </summary>
  /// <value>
  /// The maximum run cadence.
  /// </value>
  [JsonPropertyName("maxRunCadence")]
  public long MaxRunCadence { get; set; }

  /// <summary>
  /// Gets or sets the average temperature.
  /// </summary>
  /// <value>
  /// The average temperature.
  /// </value>
  [JsonPropertyName("averageTemperature")]
  public double AverageTemperature { get; set; }

  /// <summary>
  /// Gets or sets the maximum temperature.
  /// </summary>
  /// <value>
  /// The maximum temperature.
  /// </value>
  [JsonPropertyName("maxTemperature")]
  public long MaxTemperature { get; set; }

  /// <summary>
  /// Gets or sets the minimum temperature.
  /// </summary>
  /// <value>
  /// The minimum temperature.
  /// </value>
  [JsonPropertyName("minTemperature")]
  public long MinTemperature { get; set; }

  /// <summary>
  /// Gets or sets the length of the stride.
  /// </summary>
  /// <value>
  /// The length of the stride.
  /// </value>
  [JsonPropertyName("strideLength")]
  public double StrideLength { get; set; }

  /// <summary>
  /// Gets or sets the training effect.
  /// </summary>
  /// <value>
  /// The training effect.
  /// </value>
  [JsonPropertyName("trainingEffect")]
  public double TrainingEffect { get; set; }

  /// <summary>
  /// Gets or sets the anaerobic training effect.
  /// </summary>
  /// <value>
  /// The anaerobic training effect.
  /// </value>
  [JsonPropertyName("anaerobicTrainingEffect")]
  public double AnaerobicTrainingEffect { get; set; }

  /// <summary>
  /// Gets or sets the aerobic training effect message.
  /// </summary>
  /// <value>
  /// The aerobic training effect message.
  /// </value>
  [JsonPropertyName("aerobicTrainingEffectMessage")]
  public string? AerobicTrainingEffectMessage { get; set; }

  /// <summary>
  /// Gets or sets the anaerobic training effect message.
  /// </summary>
  /// <value>
  /// The anaerobic training effect message.
  /// </value>
  [JsonPropertyName("anaerobicTrainingEffectMessage")]
  public string? AnaerobicTrainingEffectMessage { get; set; }

  /// <summary>
  /// Gets or sets the end latitude.
  /// </summary>
  /// <value>
  /// The end latitude.
  /// </value>
  [JsonPropertyName("endLatitude")]
  public double EndLatitude { get; set; }

  /// <summary>
  /// Gets or sets the end longitude.
  /// </summary>
  /// <value>
  /// The end longitude.
  /// </value>
  [JsonPropertyName("endLongitude")]
  public double EndLongitude { get; set; }

  /// <summary>
  /// Gets or sets a value indicating whether this instance is deco dive.
  /// </summary>
  /// <value>
  ///   <c>true</c> if this instance is deco dive; otherwise, <c>false</c>.
  /// </value>
  [JsonPropertyName("isDecoDive")]
  public bool IsDecoDive { get; set; }

  /// <summary>
  /// Gets or sets the average vertical speed.
  /// </summary>
  /// <value>
  /// The average vertical speed.
  /// </value>
  [JsonPropertyName("avgVerticalSpeed")]
  public long AvgVerticalSpeed { get; set; }

  /// <summary>
  /// Gets or sets the maximum vertical speed.
  /// </summary>
  /// <value>
  /// The maximum vertical speed.
  /// </value>
  [JsonPropertyName("maxVerticalSpeed")]
  public double MaxVerticalSpeed { get; set; }
}