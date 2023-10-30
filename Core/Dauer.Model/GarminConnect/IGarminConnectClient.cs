#nullable enable
namespace Dauer.Model.GarminConnect;

/// <summary>
/// Inspired by https://github.com/Abasz/garmin-connect-client
/// </summary>
public interface IGarminConnectClient
{
  GarminConnectConfig Config { get; set; }
  bool IsSignedIn { get;  }
  Dictionary<string, Cookie>? Cookies { get; set; }

  /// <summary>
  /// Progress 0-100 of the last call to <see cref="AuthenticateAsync"/>
  /// 100 shall not be a programmatic indicator of completion; for that, use the return value of <see cref="AuthenticateAsync"/>
  /// </summary>
  double AuthenticateProgress { get; }

  /// <summary>
  /// Authenticates this instance.
  /// </summary>
  /// <returns>Tuple of Cookies and HTTP handler</returns>
  Task<bool> AuthenticateAsync();

  Task<bool> LogoutAsync();

  /// <summary>
  /// Return true if the SESSIONID cookie is present and a request to Garmin Connect succeeds
  /// </summary>
  Task<bool> IsAuthenticatedAsync();

  /// <summary>
  /// Downloads the activity file.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="fileFormat">The file format.</param>
  /// <returns>Stream</returns>
  Task<byte[]> DownloadActivityFile(long activityId, ActivityFileType fileFormat);

  Task<List<GarminActivity>> LoadActivities(int limit, int start, DateTime after, DateTime before);

  /// <summary>
  /// Loads the activity.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <returns>Activity</returns>
  Task<GarminActivity> LoadActivity(long activityId);

  /// <summary>
  /// Loads the activity types.
  /// </summary>
  /// <returns>List of activities</returns>
  Task<List<ActivityType>> LoadActivityTypes();

  Task<GarminFitnessStats> GetLifetimeFitnessStats();
  Task<List<GarminFitnessStats>> GetYearyFitnessStats();

  /// <summary>
  /// Sets the activity description.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="description">The description.</param>
  /// <returns>The task</returns>
  Task<bool> SetActivityDescription(long activityId, string description);

  /// <summary>
  /// Sets the name of the activity.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="activityName">Name of the activity.</param>
  /// <returns>The task</returns>
  Task<bool> SetActivityName(long activityId, string activityName);

  /// <summary>
  /// Sets the type of the activity.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="activityType">Type of the activity.</param>
  /// <returns>The task</returns>
  Task<bool> SetActivityType(long activityId, ActivityType activityType);

  Task<bool> SetEventType(long activityId, ActivityType eventType);

  /// <summary>
  /// Delete the activity.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  Task<bool> DeleteActivity(long activityId);

  /// <summary>
  /// Uploads the activity from a file on the filesystem.
  /// </summary>
  /// <param name="fileName">Name of the file.</param>
  /// <param name="fileFormat">The file format.</param>
  /// <returns>Tuple of result and activity id</returns>
  Task<(bool Success, long ActivityId)> UploadActivity(string fileName, FileFormat fileFormat);

  /// <summary>
  /// Uploads the activity from a stream.
  /// </summary>
  /// <param name="stream">Stream of the file.</param>
  /// <param name="fileFormat">The file format.</param>
  /// <returns>Tuple of result and activity id</returns>
  Task<(bool Success, long ActivityId)> UploadActivity(Stream stream, FileFormat fileFormat);
}