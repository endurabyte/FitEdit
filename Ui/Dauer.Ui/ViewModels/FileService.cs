using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;
namespace Dauer.Ui.ViewModels;

public interface IFileService
{
  UiFile? MainFile { get; set; }
  ObservableCollection<UiFile> Files { get; set; }
  IObservable<DauerActivity> Deleted { get; }

  Task<bool> CreateAsync(DauerActivity? act, CancellationToken ct = default);
  Task<DauerActivity?> ReadAsync(string id);
  Task<bool> UpdateAsync(DauerActivity? act);
  Task<bool> DeleteAsync(DauerActivity? act);
  Task<List<string>> GetAllActivityIdsAsync();
}

}

/// <summary>
/// Encapsulates the shared state of the loaded file and which record is currently visualized / editable.
/// </summary>
public class FileService : ReactiveObject, IFileService
{
  private readonly IDatabaseAdapter db_;

  [Reactive] public UiFile? MainFile { get; set; }
  [Reactive] public ObservableCollection<UiFile> Files { get; set; } = new();

  public IObservable<DauerActivity> Deleted => deletedSubject_;
  private readonly ISubject<DauerActivity> deletedSubject_ = new Subject<DauerActivity>();

  public FileService(IDatabaseAdapter db)
  {
    db_ = db;

    db.PropertyChanged += (o, e) => 
    {
      if (!db.Ready) { return; }
      InitFilesList();
    };
  }

  private void InitFilesList()
  {
    _ = Task.Run(async () =>
    {
      List<DauerActivity> acts = await db_.GetAllActivitiesAsync().AnyContext();

      var files = acts.Select(act => new UiFile
      {
        FitFile = null, // Don't parse the blobs, that would be too slow
        Activity = act,
      }).ToList();

      Files.AddRange(files);
    });
  }

  public async Task<bool> CreateAsync(DauerActivity? act, CancellationToken ct = default)
  {
    if (act == null) { return false; }

    try
    {
      if (!await db_.InsertAsync(act)) { return false; }

      if (act.File == null) { return true; }
      if (!CreateParentDir(act.File.Path)) { return false; }
      await File.WriteAllBytesAsync(act.File.Path, act.File.Bytes, ct).AnyContext();
      return true;
    }
    catch (Exception e)
    {
      Log.Error(e);
      return false;
    }
  }

  public async Task<DauerActivity?> ReadAsync(string id)
  {
    try
    {
      DauerActivity? act = await db_.GetActivityAsync(id);
      if (act == null) { return null; }

      act.File ??= new FileReference(act.Name ?? id, null) { Id = id };
      act.File.Bytes = await File.ReadAllBytesAsync(act.File.Path).AnyContext();

      return act;
    }
    catch (Exception e)
    {
      Log.Error(e);
      return null;
    }
  }

  public async Task<bool> UpdateAsync(DauerActivity? act)
  {
    if (act == null) { return false; }

    try
    {
      return await db_.UpdateAsync(act).AnyContext();
    }
    catch (Exception e)
    {
      Log.Error(e);
      return false;
    }
  }

  public async Task<bool> DeleteAsync(DauerActivity? act)
  {
    if (act == null) { return false; }

    try
    {
      bool ok = await db_.DeleteAsync(act);
      deletedSubject_?.OnNext(act);

      if (act.File == null) { return ok; }

      DeleteParentDir(act.File.Path);
      return true;
    }
    catch (Exception e)
    {
      Log.Error(e);
      return false;
    }
  }

  public async Task<List<string>> GetAllActivityIdsAsync() => await db_.GetAllActivityIdsAsync();

  private static bool CreateParentDir(string? path)
  {
    if (path == null) { return false; }
    string? parent = Directory.GetParent(path)?.FullName;

    if (parent == null) { return false; }
    Directory.CreateDirectory(parent);
    return true;
  }

  private static bool DeleteParentDir(string? path)
  {
    if (path == null) { return false; }
    string? parent = Directory.GetParent(path)?.FullName;

    if (parent == null) { return false; }
    Directory.Delete(parent, recursive: true);
    return true;
  }
}

