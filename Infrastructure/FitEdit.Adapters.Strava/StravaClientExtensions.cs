using FitEdit.Model;
using FitEdit.Model.Strava;

namespace FitEdit.Adapters.Strava;

public static class StravaClientExtensions
{
  /// <summary>
  /// Download the given FIT files in parallel. Rate limit so we don't get blocked.
  /// For each file, call the given function to persist (save) the file.
  /// </summary>
  public static async Task DownloadInParallelAsync(this IStravaClient strava, NotifyBubble bubble, List<(long, LocalActivity)> mapped, Func<LocalActivity, Task> persist)
  {
    var workInterval = TimeSpan.FromSeconds(10);
    var restInterval = TimeSpan.FromSeconds(10);
    var start = DateTime.UtcNow;

    bubble.Status = $"Downloading {mapped.Count} activities...";

    int i = 0;
    await Parallel.ForEachAsync(mapped, async ((long id, LocalActivity la) tup, CancellationToken ct) =>
    {
      if (DateTime.UtcNow - start > workInterval)
      {
        await Task.Delay(restInterval, ct);
        start = DateTime.UtcNow;
      }

      byte[] bytes = await strava.DownloadActivityFileAsync(tup.id, ct);
      Interlocked.Increment(ref i);
      bubble.Status = $"{(double)i / mapped.Count * 100:#.#}% ({i} of {mapped.Count}) - Downloaded activity \"{tup.la.Name}\" ({tup.id})";

      tup.la.File = new FileReference("strava-export.fit", bytes);
      await persist(tup.la);
    });
  }
}
