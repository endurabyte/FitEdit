using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using Avalonia.Threading;
using Dauer.Data;
using Dauer.Data.Fit;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dauer.Services;
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
  private readonly ICryptoService crypto_;

  [Reactive] public UiFile? MainFile { get; set; }
  [Reactive] public ObservableCollection<UiFile> Files { get; set; } = new();

  public IObservable<DauerActivity> Deleted => deletedSubject_;
  private readonly ISubject<DauerActivity> deletedSubject_ = new Subject<DauerActivity>();

  public FileService(IDatabaseAdapter db, ICryptoService crypto)
  {
    db_ = db;
    crypto_ = crypto;
    db.PropertyChanged += (o, e) =>
    {
      if (!db.Ready) { return; }
      InitFilesList();
    };
  }

  private void InitFilesList() => _ = Task.Run(LoadMore);
  
  public async Task CreateAsync(FitFile fit)
  { 
    UiFile? originalFile = MainFile;

    var newFile = new UiFile
    {
      FitFile = fit,
      Activity = new DauerActivity(),
    };

    newFile.Activity.Id = $"{Guid.NewGuid()}";
    newFile.Activity.Name = originalFile?.Activity?.Name + " (Edited)";
    newFile.Activity.FileType = "fit";
    newFile.Activity.StartTime = fit.GetStartTime();
    newFile.Activity.File = new FileReference(newFile.Activity.Name, fit.GetBytes());
    Add(newFile);

    bool ok = await CreateAsync(newFile.Activity);

    // Make the new file the active one
    await Dispatcher.UIThread.InvokeAsync(() =>
    {
      if (MainFile != null) { MainFile.IsVisible = false; }
      MainFile = newFile;
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

      byte[]? encrypted = crypto_.Encrypt(GetSalt(act), act.File.Bytes);
      if (encrypted == null) { return false; }
      await File.WriteAllBytesAsync(act.File.Path, encrypted, ct).AnyContext();

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
      byte[]? encrypted = await File.ReadAllBytesAsync(act.File.Path, ct).AnyContext();
      act.File.Bytes = crypto_.Decrypt(GetSalt(act), encrypted) ?? Array.Empty<byte>();

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

      byte[]? encrypted = crypto_.Encrypt(GetSalt(act), act.File.Bytes);
      if (encrypted == null) { return false; }
      await File.WriteAllBytesAsync(act.File.Path, encrypted, ct).AnyContext();
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
  public async Task<List<DauerActivity>> GetAllActivitiesAsync(DateTime? after, DateTime? before, int limit) => await db_.GetAllActivitiesAsync(after, before, limit);
  public async Task<bool> ActivityExistsAsync(string id) => await db_.ActivityExistsAsync(id).AnyContext();

  public void Add(UiFile file)
  {
    if (file is null) { return; }

    // Find the first activity that is newer. They are sorted newest to oldest; preserve that
    UiFile? firstNewer = Files.Reverse().FirstOrDefault(f => f.Activity?.StartTime > file.Activity?.StartTime);
    int idx = firstNewer == null ? 0 : Files.IndexOf(firstNewer) + 1;
    Files.Insert(idx, file);
  }

  public async Task LoadMore()
  {
    DateTime oldest = Files
      .Select(f => f.Activity?.StartTime ?? DateTime.UtcNow)
      .OrderBy(d => d)
      .FirstOrDefault();

    if (oldest == default)
    {
      oldest = DateTime.UtcNow;
    }

    var backfill = TimeSpan.FromDays(7);
    var backfillLimit = TimeSpan.FromDays(365 * 10); // 10 years
    int limit = 25;

    List<DauerActivity> more = new();

    // While no results found, keep looking further into the past until we get a result or hit the limit
    while (more.Count == 0 && backfill < backfillLimit)
    {
      more = await GetAllActivitiesAsync(oldest - backfill, oldest, limit);
      backfill *= 2;
    }

    more.Sort((a1, a2) => a2.StartTime.CompareTo(a1.StartTime));

    foreach (DauerActivity activity in more)
    {
      Add(new UiFile
      {
        FitFile = null, // Don't parse the blobs, that would be too slow
        Activity = activity
      });
    } 
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

  private static string GetSalt(DauerActivity? act) => $"{act?.Id}+$F#6$ugnlsn91=@nq2kHznq";
}

