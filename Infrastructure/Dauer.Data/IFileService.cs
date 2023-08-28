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
  Task<List<string>> GetAllActivityIdsAsync();

  void Add(UiFile file);
}
