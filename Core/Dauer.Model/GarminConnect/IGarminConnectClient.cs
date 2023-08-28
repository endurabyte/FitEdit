using System.Net;

namespace Dauer.Model.GarminConnect;

/// <summary>
/// Inspired by https://github.com/Abasz/garmin-connect-client
/// </summary>
public interface IGarminConnectClient
{
  GarminConnectConfig Config { get; set; }

  /// <summary>
  /// Authenticates this instance.
  /// </summary>
  /// <returns>Tuple of Cookies and HTTP handler</returns>
  Task<(CookieContainer, HttpClientHandler)> Authenticate();

  /// <summary>
  /// Downloads the activity file.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="fileFormat">The file format.</param>
  /// <returns>Stream</returns>
  Task<Stream> DownloadActivityFile(long activityId, ActivityFileType fileFormat);

  /// <summary>
  /// Loads the activities.
  /// </summary>
  /// <param name="limit">The limit.</param>
  /// <param name="start">The start.</param>
  /// <param name="from">From.</param>
  /// <returns>List of activities</returns>
  Task<List<Activity>> LoadActivities(int limit, int start, DateTime from);

  /// <summary>
  /// Loads the activity.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <returns>Activity</returns>
  Task<Activity> LoadActivity(long activityId);

  /// <summary>
  /// Loads the activity types.
  /// </summary>
  /// <returns>List of activities</returns>
  Task<List<ActivityType>> LoadActivityTypes();

  /// <summary>
  /// Sets the activity description.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="description">The description.</param>
  /// <returns>The task</returns>
  Task SetActivityDescription(long activityId, string description);

  /// <summary>
  /// Sets the name of the activity.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="activityName">Name of the activity.</param>
  /// <returns>The task</returns>
  Task SetActivityName(long activityId, string activityName);

  /// <summary>
  /// Sets the type of the activity.
  /// </summary>
  /// <param name="activityId">The activity identifier.</param>
  /// <param name="activityType">Type of the activity.</param>
  /// <returns>The task</returns>
  Task SetActivityType(long activityId, ActivityType activityType);

  /// <summary>
  /// Uploads the activity.
  /// </summary>
  /// <param name="fileName">Name of the file.</param>
  /// <param name="fileFormat">The file format.</param>
  /// <returns>Tuple of result and activity id</returns>
  Task<(bool Success, long ActivityId)> UploadActivity(string fileName, FileFormat fileFormat);
}