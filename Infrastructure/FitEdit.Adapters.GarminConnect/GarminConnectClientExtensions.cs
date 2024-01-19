#nullable enable
using FitEdit.Model;
using FitEdit.Model.GarminConnect;
using FitEdit.Model.Extensions;
using System.Collections.Concurrent;

namespace FitEdit.Adapters.GarminConnect;

public static class GarminConnectClientExtensions
{
  /// <summary>
  /// Get all activities for the given user.
  /// It can be slow as it paginates through the entire activities list.
  /// 
  /// <para/>
  /// Does not include the FIT files. That must be downloaded separately.
  /// </summary>
  public static async Task<List<GarminActivity>> GetAllActivitiesAsync(this IGarminConnectClient garmin, UserTask task)
  {
    // Get total count of activities to download, and get the year of the earliest activity e.g.
    // It will be Jan 1 e.g. 2015-01-01 even though my earliest activity is 2015-03-21.
    GarminFitnessStats? lifetimeStats = await garmin.GetLifetimeFitnessStats(task.CancellationToken);
    List<GarminFitnessStats>? annualStats = (await garmin.GetYearyFitnessStats(task.CancellationToken))
      ?.OrderByDescending(year => year.CountOfActivities)
      ?.ToList();

    // Handle the case that we didn't get yearly stats.
    // There are probably not many activities before 2010.
    // It's an OK fallback. We might miss activities earlier than 2010,
    // and we will issue an extra request for each year back to 2010, even if it has no activities.
    DateTime earliestYear = annualStats?.LastOrDefault()?.Date ?? new DateTime(2010, 1, 1);

    long total = lifetimeStats?.CountOfActivities ?? -1;

    string totalStr = $"{(total < 0 ? "all" : $"{total}")}";
    task.Status = $"Listing {totalStr} activities on Garmin...";

    //IDictionary<long, GarminActivity> all = await garmin.ListSerially(task, earliestYear, total);
    IDictionary<long, GarminActivity> all = await garmin.ListInParallel(task, earliestYear, total);

    List<GarminActivity> result = all.Values.OrderByDescending(act => act.GetStartTime()).ToList();
    task.Status = $"Found {result.Count} Garmin activities going back to {result.LastOrDefault()?.GetStartTime()} ";

    return result;
  }

  private static async Task<IDictionary<long, GarminActivity>> ListInParallel(this IGarminConnectClient garmin, UserTask task, DateTime earliestYear, long total, int chunkSize = 500)
  {
    ConcurrentDictionary<long, GarminActivity> all = new();

    List<(DateTime after, DateTime before)> ranges = GetRanges(earliestYear, DateTime.Today);

    // Download each year in parallel
    await Parallel.ForEachAsync(ranges, async (range, ct) =>
    {
      int chunk = 0;
      List<GarminActivity> some;

      do
      {
        some = await garmin.LoadActivities(chunkSize, chunk * chunkSize, range.after, range.before, ct);
        all.AddRange(some.Select(a => (a.ActivityId, a)));
        chunk++;

        if (some.Any())
        {
          task.Status = GetStatus(total, all);
        }

      } while (!task.IsCanceled && some.Any());
    });

    return all;
  }

  /// <summary>
  /// Return a list of yearlong date ranges.
  /// 
  /// <para/>
  /// The ranges start from the ending of the year of <paramref name="before"/>
  /// and end at the beginning of the year of <paramref name="after"/>.
  /// 
  /// <para/>
  /// Example: Given 2015-03-31 and 2023-10-18, returned list is 
  /// 
  /// <code>
  /// { 
  ///   (2023-01-01, 2023-12-31),
  ///   (2022-01-01, 2022-12-31),
  ///   (2021-01-01, 2021-12-31),
  ///   (2020-01-01, 2020-12-31),
  ///   (2019-01-01, 2019-12-31),
  ///   (2018-01-01, 2018-12-31),
  ///   (2017-01-01, 2017-12-31),
  ///   (2016-01-01, 2016-12-31),
  ///   (2015-01-01, 2015-12-31),
  /// }
  /// </code>
  /// </summary>
  private static List<(DateTime after, DateTime before)> GetRanges(DateTime after, DateTime before)
  {
    if (before <= after) { return new List<(DateTime, DateTime)>(); }

    int years = before.Year - after.Year + 1;

    return Enumerable.Range(0, years)
      .Select(i => (new DateTime(DateTime.Today.Year - i, 1, 1),
                    new DateTime(DateTime.Today.Year - i, 12, 31)))
      .ToList();
  }

  private static async Task<IDictionary<long, GarminActivity>> ListSerially(this IGarminConnectClient garmin, UserTask task, DateTime earliestYear, long total, int chunkSize = 500)
  { 
    Dictionary<long, GarminActivity> all = new();

    int chunk = 0;

    DateTime before = new(DateTime.Today.Year, 12, 31);
    DateTime after = before.AddYears(-1);

    while (!task.IsCanceled && before > earliestYear)
    {
      List<GarminActivity> some = await garmin.LoadActivities(chunkSize, chunk * chunkSize, after, before);
      all.AddRange(some.Select(a => (a.ActivityId, a)));
      chunk++;

      task.Status = GetStatus(total, all);

      if (some.Count < chunkSize)
      {
        before = after;
        after = before.AddYears(-1);
        chunk = 0;
        continue;
      }
    }

    return all;
  }

  private static string GetStatus(long total, IDictionary<long, GarminActivity> all) =>
    total > 0
      ? $"Found {all.Count} of {total} Garmin activities ({(double)all.Count / total * 100:#.#}%)."
      : $"Found {all.Count} Garmin activities";

  /// <summary>
  /// Download the given FIT files in parallel. Rate limit so we don't get blocked.
  /// For each file, call the given function to persist (save) the file.
  /// </summary>
  public static async Task DownloadInParallelAsync(this IGarminConnectClient garmin, UserTask task, List<(long, LocalActivity)> mapped, Func<LocalActivity, Task> persist)
  {
    var workInterval = TimeSpan.FromSeconds(10);
    var restInterval = TimeSpan.FromSeconds(10);
    var start = DateTime.UtcNow;

    task.Status = $"Downloading {mapped.Count} activities...";

    int i = 0;
    await Parallel.ForEachAsync(mapped, async ((long activityId, LocalActivity la) tup, CancellationToken ct) =>
    {
      if (DateTime.UtcNow - start > workInterval)
      {
        await Task.Delay(restInterval, ct);
        start = DateTime.UtcNow;
      }

      byte[] bytes = await garmin.DownloadActivityFile(tup.activityId, ActivityFileType.Fit);
      Interlocked.Increment(ref i);
      task.Status = $"{(double)i / mapped.Count * 100:#.#}% ({i} of {mapped.Count}) - Downloaded activity \"{tup.la.Name}\" ({tup.activityId})";

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
