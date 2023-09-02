#nullable enable
using System.Collections.ObjectModel;
using Dauer.Model;

namespace Dauer.Data;

public interface IFileService
{
  UiFile? MainFile { get; set; }
  ObservableCollection<UiFile> Files { get; set; }
  IObservable<DauerActivity> Deleted { get; }

  Task<bool> CreateAsync(DauerActivity? act, CancellationToken ct = default);
  Task<DauerActivity?> ReadAsync(string id, CancellationToken ct = default);
  Task<bool> UpdateAsync(DauerActivity? act, CancellationToken ct = default);
  Task<bool> DeleteAsync(DauerActivity? act);
  Task<List<string>> GetAllActivityIdsAsync(DateTime? after, DateTime? before);
  Task<List<DauerActivity>> GetAllActivitiesAsync(DateTime? after, DateTime? before);

  void Add(UiFile file);
}
