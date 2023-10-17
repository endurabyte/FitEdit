#nullable enable
using Dauer.Model;
using Dauer.Model.GarminConnect;

namespace Dauer.Adapters.GarminConnect;

public static class GarminConnectClientExtensions
{
  /// <summary>
  /// Get all activities for the given user.
  /// It can be slow as it paginates through the entire activities list.
  /// 
  /// <para/>
  /// Does not include the FIT files. That must be downloaded separately.
  /// </summary>
  public static async Task<List<Activity>> GetAllActivitiesAsync(this IGarminConnectClient garmin, DateTime? before = null, int chunkSize = 500)
  {
    before ??= DateTime.UtcNow;

    int chunk = 0;

    List<Activity> all = new(chunkSize);
    List<Activity> some;

    long? activityCount = (await garmin.GetFitnessStats()).FirstOrDefault()?.CountOfActivities;

    Log.Info($"Listing {$"{activityCount}" ?? "all"} activities on Garmin...");

    do
    {
      some = await garmin.LoadActivities(chunkSize, chunk * chunkSize, before.Value);
      all.AddRange(some);
      chunk++;

      if (all.Any())
      {
        Log.Info($"Found {all.Count} activities. Oldest is from {all.LastOrDefault().GetStartTime()} ");
      }
      else
      {
        Log.Info("Did not find any activities");
      }
    } while (some.Count > 0);

    return all;
  }

  /// <summary>
  /// Download the given FIT files. Rate limit so we don't get blocked.
  /// </summary>
  public static async Task DownloadAsync(this IGarminConnectClient garmin, List<(long, LocalActivity)> mapped, Func<LocalActivity, Task> persist)
  {
    var workInterval = TimeSpan.FromSeconds(10);
    var restInterval = TimeSpan.FromSeconds(10);
    var start = DateTime.UtcNow;

    Log.Info($"Downloading {mapped.Count} activities...");

    await Parallel.ForEachAsync(mapped, async ((long activityId, LocalActivity la) tup, CancellationToken ct) =>
    {
      if (DateTime.UtcNow - start > workInterval)
      {
        await Task.Delay(restInterval, ct);
        start = DateTime.UtcNow;
      }

      byte[] bytes = await garmin.DownloadActivityFile(tup.activityId, ActivityFileType.Fit);
      Log.Info($"Downloaded {bytes.Length} bytes for activity \"{tup.la.Name}\" ({tup.activityId})");

      var fr = new FileReference("garmin-export.fit", bytes);
      List<FileReference> files = Zip.Unzip(fr);
      if (files.Any())
      {
        tup.la.File = files.First();
        await persist(tup.la);
      }
    });
  }
}
