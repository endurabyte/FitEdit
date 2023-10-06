#nullable enable
using System.Collections.ObjectModel;
using Dauer.Data.Fit;
using Dauer.Model;

namespace Dauer.Data;

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
  Task<bool> ActivityExistsAsync(string id);

  void Add(UiFile file);
  Task LoadMore();
}
