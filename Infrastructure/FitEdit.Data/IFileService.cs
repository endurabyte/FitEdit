#nullable enable
using System.Collections.ObjectModel;
using FitEdit.Data.Fit;
using FitEdit.Model;

namespace FitEdit.Data;

public interface IFileService
{
  UiFile? MainFile { get; set; }
  ObservableCollection<UiFile> Files { get; set; }
  IObservable<LocalActivity> Deleted { get; }

  /// <summary>
  /// Create a new file and file list entry for the given FIT file
  /// </summary>
  Task CreateAsync(FitFile fit);
  Task<bool> CreateAsync(LocalActivity? act, CancellationToken ct = default);
  Task<LocalActivity?> ReadAsync(string id, CancellationToken ct = default);
  Task<bool> UpdateAsync(LocalActivity? act, CancellationToken ct = default);
  Task<bool> DeleteAsync(LocalActivity? act);
  Task<List<string>> GetAllActivityIdsAsync(DateTime? after, DateTime? before);
  Task<List<LocalActivity>> GetAllActivitiesAsync(DateTime? after, DateTime? before, int limit);

  /// <summary>
  /// Find existing activities on <see cref="LocalActivity.Id"/> or fall back to start time e.g. <see cref="LocalActivity.StartTime"/>. 
  /// The match on start time is fuzzy, e.g. +- 2 seconds.
  /// </summary>
  Task<LocalActivity?> GetByIdOrStartTimeAsync(string id, DateTime startTime);

  /// <summary>
  /// Find existing activities on <see cref="LocalActivity.SourceId"/> e.g. Garmin Activity ID or fall back to start time e.g. <see cref="LocalActivity.StartTime"/>. 
  /// The match on start time is fuzzy, e.g. +- 2 seconds.
  /// </summary>
  Task<LocalActivity?> GetBySourceIdOrStartTimeAsync(string sourceId, DateTime startTime);

  void Add(UiFile file);
  Task LoadMore();
}
