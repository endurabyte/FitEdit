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
  Task<DauerActivity?> ReadAsync(string id, CancellationToken ct = default);
  Task<bool> UpdateAsync(DauerActivity? act, CancellationToken ct = default);
  Task<bool> DeleteAsync(DauerActivity? act);
  Task<List<string>> GetAllActivityIdsAsync();

  void Add(UiFile file);
}

public class NullFileService : IFileService
{
  public UiFile? MainFile { get; set; }
  public ObservableCollection<UiFile> Files { get; set; } = new();

  public IObservable<DauerActivity> Deleted => deletedSubject_;
  private readonly ISubject<DauerActivity> deletedSubject_ = new Subject<DauerActivity>();

  public NullFileService()
  {
    int i = 0;

    AddFake(i++);
    AddFake(i++);
    AddFake(i++);
    var file = AddFake(i++);

    file.Activity = new DauerActivity
    {
      Name = "Activity name",
      Description = "This is the activity description.\nIt can get pretty long, so make sure to provide enough space for it, but it should be responsive in case it is not very long.",
      File = new FileReference("fitfile.fit", Array.Empty<byte>())
    };
  }

  private UiFile AddFake(int i) 
  {
    var file = new UiFile
    {
      Activity = new DauerActivity
      {
        Id = $"{Guid.NewGuid()}",
        Name = $"Workout {i}",
        Description = $"Description {i}"
      },
      Progress = 66.7,
    };

    Files.Add(file);
    return file;
  }

  public Task<bool> CreateAsync(DauerActivity? act, CancellationToken ct = default) => Task.FromResult(true);
  public Task<bool> DeleteAsync(DauerActivity? act)=> Task.FromResult(true);
  public Task<List<string>> GetAllActivityIdsAsync() => Task.FromResult(new List<string>());
  public Task<DauerActivity?> ReadAsync(string id, CancellationToken ct = default) => Task.FromResult((DauerActivity?)new DauerActivity { Id = id });
  public Task<bool> UpdateAsync(DauerActivity? act, CancellationToken ct = default) => Task.FromResult(true);
  public void Add(UiFile file) { }
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
      List<DauerActivity> acts = await db_
        .GetAllActivitiesAsync()
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

  public async Task<List<string>> GetAllActivityIdsAsync() => await db_.GetAllActivityIdsAsync();

  public void Add(UiFile file)
  {
    UiFile? previous = Files.FirstOrDefault(f => f.Activity?.StartTime >  file.Activity?.StartTime);
    int idx = previous == null ? 0 : Files.IndexOf(previous);
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

