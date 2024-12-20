﻿#nullable enable
using System.Reactive.Linq;
using DynamicData.Binding;
using System.Collections.Specialized;
using FitEdit.Data;
using ReactiveUI;
using FitEdit.Model;
using System.Collections.Concurrent;

namespace FitEdit.Ui.Extensions;

public static class FileServiceExtensions
{
  public static IDisposable SubscribeAdds(this IFileService fs, Action<UiFile> handle) =>
    fs.Files.ObserveCollectionChanges().Subscribe(x =>
     {
       if (x.EventArgs.Action != NotifyCollectionChangedAction.Add) { return; }
       if (x?.EventArgs?.NewItems == null) { return; }

       foreach (var file in x.EventArgs.NewItems.OfType<UiFile>())
       {
         handle(file);
         file.ObservableForProperty(x => x.FitFile).Subscribe(property => handle(property.Sender));
       }
     });

  public static IDisposable SubscribeRemoves(this IFileService fs, Action<UiFile> handle) =>
    fs.Files.ObserveCollectionChanges().Subscribe(x =>
     {
       if (x.EventArgs.Action != NotifyCollectionChangedAction.Remove) { return; }
       if (x?.EventArgs?.OldItems == null) { return; }

       foreach (var file in x.EventArgs.OldItems.OfType<UiFile>())
       {
         handle(file);
       }
     });

  /// <summary>
  /// Return only activities which do not already exist in the DB. 
  /// Also return activities which exist but do not have a FIT file.
  /// The returned LocalActivity on the tuple will be non-null if the activity already exists in the DB, else it will be null.
  /// </summary>
  public static async Task<List<(T, LocalActivity?)>> FilterExistingAsync<T>(this IFileService fileService, 
    NotifyBubble bubble, 
    List<(T t, string sourceId, string name, DateTime startTime)> ts)
  {
    var allExisting = new ConcurrentDictionary<string, LocalActivity>();

    int i = 0;

    // Get existing activites from DB
    await Parallel.ForEachAsync(ts, bubble.CancellationToken, async (tup, ct) =>
    {
      string sourceId = tup.sourceId;
      string? name = tup.name;
      DateTime startTime = tup.startTime;

      if (startTime == default)
      {
        Log.Error($"Could not find start time for activity {sourceId}. Name: \"{name}\"");
        return; // Treat activity as if it were new
      }

      LocalActivity? existing = await fileService.GetBySourceIdOrStartTimeAsync(sourceId, startTime);

      Interlocked.Increment(ref i);
      bubble.Status = $"Scanning local database ({i} of {ts.Count}) ({(double)i/ts.Count*100:#.#}%)";

      if (existing == null) { return; } // New activity
      allExisting[sourceId] = existing;
    });

    // Filter out ones that exist and have a FIT file, or have some other problem
    Dictionary<string, LocalActivity> existingWithFile = allExisting.Where(kvp =>
    {
      string sourceId = kvp.Key;
      LocalActivity existing = kvp.Value;

      // Activity exists and we have its file.
      if (existing?.File is not null) { return true; }

      // Conflict: We matched on date but not on Garmin activity ID
      if (existing?.SourceId is not null && existing.SourceId != sourceId)
      {
        Log.Error($"Matched activity on start time {existing.StartTime} but ID {existing.SourceId} conflicts with expected ID {sourceId}. Name: \"{existing.Name}\"");
        return true;
      }

      return false; // It's a new activity
    }).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);

    LocalActivity? maybeGetExisting(string sourceId) => 
      allExisting!.TryGetValue(sourceId, out LocalActivity? existing) 
        ? existing 
        : null;

    return ts
      .Where(tup => !existingWithFile.ContainsKey(tup.sourceId))
      .Select(tup => (tup.t, maybeGetExisting(tup.sourceId)))
      .ToList();
    
  }
}