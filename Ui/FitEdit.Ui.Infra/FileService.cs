using System.Collections.ObjectModel;
using System.Reactive.Subjects;
using Avalonia.Threading;
using FitEdit.Data;
using FitEdit.Data.Fit;
using FitEdit.Model;
using FitEdit.Model.Data;
using FitEdit.Model.Extensions;
using FitEdit.Services;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Ui.Infra;

/// <summary>
/// Encapsulates the shared state of the loaded file and which record is currently visualized / editable.
/// </summary>
public class FileService : ReactiveObject, IFileService
{
  private readonly IDatabaseAdapter db_;
  private readonly ICryptoService crypto_;
  private readonly string storageRoot_;

  [Reactive] public UiFile? MainFile { get; set; }
  [Reactive] public ObservableCollection<UiFile> Files { get; set; } = new();

  public string PathFor(LocalActivity act) => Path.Combine(storageRoot_, "Files", $"{act.File!.Id}", act.File.Name);

  public FileService(IDatabaseAdapter db, ICryptoService crypto, string storageRoot)
  {
    db_ = db;
    crypto_ = crypto;
    storageRoot_ = storageRoot;

    db.PropertyChanged += (o, e) =>
    {
      if (!db.Ready) { return; }
      InitFilesList();
    };
  }

  private void InitFilesList() => _ = Task.Run(LoadMore);
  
  public async Task CreateAsync(FitFile fit, string? suffix = "(Edited)")
  { 
    UiFile? originalFile = MainFile;

    var newFile = new UiFile
    {
      FitFile = fit,
      Activity = new LocalActivity(),
    };

    newFile.Activity.Id = $"{Guid.NewGuid()}";
    newFile.Activity.Name = $"{originalFile?.Activity?.Name} {suffix}";
    newFile.Activity.FileType = "fit";
    newFile.Activity.StartTime = fit.GetStartTime();
    newFile.Activity.File = new FileReference(newFile.Activity.Name, fit.GetBytes());
    Add(newFile);

    bool ok = await CreateAsync(newFile.Activity);

    // Make the new file the active one
    // await Dispatcher.UIThread.InvokeAsync(() =>
    // {
    //   if (MainFile != null) { MainFile.IsLoaded = false; }
    //   MainFile = newFile;
    // });
  }

  public async Task<bool> CreateAsync(LocalActivity? act, CancellationToken ct = default)
  {
    if (act == null) { return false; }

    try
    {
      if (!await db_.InsertAsync(act)) { return false; }

      if (act.File == null) { return true; }
      if (!CreateParentDir(PathFor(act))) { return false; }

      byte[]? encrypted = crypto_.Encrypt(GetSalt(act), act.File.Bytes);
      if (encrypted == null) { return false; }
      await File.WriteAllBytesAsync(PathFor(act), encrypted, ct).AnyContext();

      return true;
    }
    catch (Exception e)
    {
      Log.Error(e);
      return false;
    }
  }

  public async Task<LocalActivity?> ReadAsync(string id, CancellationToken ct = default)
  {
    try
    {
      LocalActivity? act = await db_.GetActivityAsync(id);
      if (act == null) { return null; }

      act.File ??= new FileReference(id, null) { Id = id };
      byte[]? encrypted = await File.ReadAllBytesAsync(PathFor(act), ct).AnyContext();
      act.File.Bytes = crypto_.Decrypt(GetSalt(act), encrypted) ?? Array.Empty<byte>();

      if (act.File.Bytes.Length == 0)
      {
        act.File = null;
        Log.Error($"Fit file {PathFor(act)} was empty");
      }

      return act;
    }
    catch (Exception e)
    {
      Log.Error(e);
      return null;
    }
  }

  public async Task<bool> UpdateAsync(LocalActivity? act, CancellationToken ct = default)
  {
    if (act == null) { return false; }

    try
    {
      UiFile? match = Files.FirstOrDefault(f => f.Activity?.Id == act.Id);

      if (match != null)
      {
        match.Activity = act;
      }

      if (!await db_.UpdateAsync(act).AnyContext())
      {
        return false;
      }

      if (act.File == null) { return true; }
      if (act.File.Bytes.Length == 0) { return true; }
      if (!CreateParentDir(PathFor(act))) { return false; }

      byte[]? encrypted = crypto_.Encrypt(GetSalt(act), act.File.Bytes);
      if (encrypted == null) { return false; }
      await File.WriteAllBytesAsync(PathFor(act), encrypted, ct).AnyContext();
      return true;
    }
    catch (Exception e)
    {
      Log.Error(e);
      return false;
    }
  }

  public async Task<bool> DeleteAsync(LocalActivity? act)
  {
    if (act == null) { return false; }

    try
    {
      bool ok = await db_.DeleteAsync(act);

      if (act.File == null) { return ok; }

      DeleteParentDir(PathFor(act));
      return true;
    }
    catch (Exception e)
    {
      Log.Error(e);
      return false;
    }
  }

  public async Task<List<string>> GetAllActivityIdsAsync(DateTime? after, DateTime? before) => await db_.GetAllActivityIdsAsync(after, before);
  public async Task<List<LocalActivity>> GetAllActivitiesAsync(DateTime? after, DateTime? before, int limit) => await db_.GetAllActivitiesAsync(after, before, limit);
  public async Task<LocalActivity?> GetByIdOrStartTimeAsync(string id, DateTime startTime) => await db_.GetByIdOrStartTimeAsync(id, startTime).AnyContext();
  public async Task<LocalActivity?> GetBySourceIdOrStartTimeAsync(string sourceId, DateTime startTime) => await db_.GetBySourceIdOrStartTimeAsync(sourceId, startTime).AnyContext();

  public void Add(UiFile file)
  {
    if (file is null) { return; }

    lock (Files)
    {
      // Find the first activity that is newer. They are sorted newest to oldest; preserve that
      UiFile? firstNewer = Files.Reverse().FirstOrDefault(f => f?.Activity?.StartTime > file.Activity?.StartTime);
      int idx = firstNewer == null ? 0 : Files.IndexOf(firstNewer) + 1;

      Files.Insert(idx, file);
    }
  }

  public async Task LoadMore()
  {
    DateTime oldest = Files
      .Select(f => f?.Activity?.StartTime ?? DateTime.UtcNow)
      .OrderBy(d => d)
      .FirstOrDefault();

    if (oldest == default)
    {
      oldest = DateTime.UtcNow;
    }

    var backfill = TimeSpan.FromDays(7);
    var backfillLimit = TimeSpan.FromDays(365 * 10); // 10 years
    int limit = 25;

    List<LocalActivity> more = new();

    // While no results found, keep looking further into the past until we get a result or hit the limit
    while (more.Count < limit && backfill < backfillLimit)
    {
      more = await GetAllActivitiesAsync(oldest - backfill, oldest, limit);
      backfill *= 2;
    }

    // If we still didn't get any, try activities with the default Datetime e.g. 0001-01-01T00:00
    if (more.Count < limit)
    {
      more = await GetAllActivitiesAsync(new DateTime(), DateTime.UtcNow, limit);
    }
    
    more.Sort((a1, a2) => a2.StartTime.CompareTo(a1.StartTime));

    foreach (LocalActivity activity in more)
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

  private static string GetSalt(LocalActivity? act) => $"{act?.Id}+$F#6$ugnlsn91=@nq2kHznq";
}

