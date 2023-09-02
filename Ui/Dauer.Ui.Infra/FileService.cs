using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using Dauer.Data;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.Infra;

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
      DateTime before = DateTime.UtcNow;
      DateTime after = before - TimeSpan.FromDays(7);

      List<DauerActivity> acts = await db_
        .GetAllActivitiesAsync(after, before)
        .AnyContext();

      var files = acts.Select(act => new UiFile
      {
        FitFile = null, // Don't parse the blobs, that would be too slow
        Activity = act,
      }).OrderByDescending(uif => uif.Activity?.StartTime).ToList();

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

  public async Task<DauerActivity?> ReadAsync(string id, CancellationToken ct = default)
  {
    try
    {
      DauerActivity? act = await db_.GetActivityAsync(id);
      if (act == null) { return null; }

      act.File ??= new FileReference(act.Name ?? id, null) { Id = id };
      act.File.Bytes = await File.ReadAllBytesAsync(act.File.Path, ct).AnyContext();

      if (act.File.Bytes.Length == 0)
      {
        act.File = null;
        Log.Error($"Fit file {act.File?.Path} was empty");
      }

      return act;
    }
    catch (Exception e)
    {
      Log.Error(e);
      return null;
    }
  }

  public async Task<bool> UpdateAsync(DauerActivity? act, CancellationToken ct = default)
  {
    if (act == null) { return false; }

    try
    {
      if (!await db_.UpdateAsync(act).AnyContext())
      {
        return false;
      }

      if (act.File == null) { return true; }
      if (act.File.Bytes.Length == 0) { return true; }
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

  public async Task<List<string>> GetAllActivityIdsAsync(DateTime? after, DateTime? before) => await db_.GetAllActivityIdsAsync(after, before);
  public async Task<List<DauerActivity>> GetAllActivitiesAsync(DateTime? after, DateTime? before) => await db_.GetAllActivitiesAsync(after, before);

  public void Add(UiFile file)
  {
    // Find the first activity that is newer. They are sorted newest to oldest; preserve that
    UiFile? firstNewer = Files.Reverse().FirstOrDefault(f => f.Activity?.StartTime > file.Activity?.StartTime);
    int idx = firstNewer == null ? 0 : Files.IndexOf(firstNewer) + 1;
    Files.Insert(idx, file);
  }

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

