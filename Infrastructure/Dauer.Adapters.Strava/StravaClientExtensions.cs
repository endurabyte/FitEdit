using Dauer.Model;
using Dauer.Model.Strava;

namespace Dauer.Adapters.Strava;

public static class StravaClientExtensions
{
  /// <summary>
  /// Download the given FIT files in parallel. Rate limit so we don't get blocked.
  /// For each file, call the given function to persist (save) the file.
  /// </summary>
  public static async Task DownloadInParallelAsync(this IStravaClient strava, List<(long, LocalActivity)> mapped, Func<LocalActivity, Task> persist)
  {
    var workInterval = TimeSpan.FromSeconds(10);
    var restInterval = TimeSpan.FromSeconds(10);
    var start = DateTime.UtcNow;

    Log.Info($"Downloading {mapped.Count} activities...");

    await Parallel.ForEachAsync(mapped, async ((long id, LocalActivity la) tup, CancellationToken ct) =>
    {
      if (DateTime.UtcNow - start > workInterval)
      {
        await Task.Delay(restInterval, ct);
        start = DateTime.UtcNow;
      }

      byte[] bytes = await strava.DownloadActivityFileAsync(tup.id, ct);
      Log.Info($"Downloaded {bytes.Length} bytes for activity \"{tup.la.Name}\" ({tup.id})");

      tup.la.File = new FileReference("strava-export.fit", bytes);
      //await persist(tup.la);
    });
  }
}
