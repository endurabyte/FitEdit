using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Dauer.Adapters.GarminConnect;
using Dauer.Adapters.Strava;
using Dauer.Data;
using Dauer.Data.Fit;
using Dauer.Model;
using Dauer.Model.Extensions;
using Dauer.Model.GarminConnect;
using Dauer.Model.Storage;
using Dauer.Model.Strava;
using Dauer.Model.Web;
using Dauer.Services;
using Dauer.Ui.Extensions;
using Dauer.Ui.Infra;
using Dauer.Ui.Model.Supabase;
using Nmtp;
using MediaDevices;
using Microsoft.Extensions.Logging.Abstractions;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IFileViewModel
{
  /// <summary>
  /// Percentage 0-100 representing how far down the file list the user has scrolled.
  /// 0 => top,
  /// 50 => halfway,
  /// 100 => bottom
  /// </summary>
  double ScrollPercent { get; set; }
  bool IsDragActive { set; }

  void HandleFileDropped(IStorageFile? file);
}

public class DesignFileViewModel : FileViewModel
{
  public DesignFileViewModel() : base(
    new NullTaskService(),
    new NullFileService(),
    new NullFitEditService(),
    new NullGarminConnectClient(),
    new NullStravaClient(),
    new NullStorageAdapter(),
    new NullSupabaseAdapter(),
    new NullBrowser(),
    new DesignLogViewModel(),
    new FileDeleteViewModel(new NullFileService(), new NullSupabaseAdapter(), new NullLogger<FileDeleteViewModel>()),
    new FileRemoteDeleteViewModel(new NullGarminConnectClient(), new NullStravaClient(), new NullSupabaseAdapter(), new NullLogger<FileRemoteDeleteViewModel>()),
    new DragViewModel()
  ) 
  {
    IsDragActive = false;
  }
}

public class FileViewModel : ViewModelBase, IFileViewModel
{
  [Reactive] public UiFile? SelectedFile { get; set; }

  public bool IsDragActive { set => DragViewModel.IsVisible = value; }
  [Reactive] public FileDeleteViewModel FileDeleteViewModel { get; set; }
  [Reactive] public FileRemoteDeleteViewModel FileRemoteDeleteViewModel { get; set; }
  [Reactive] public ViewModelBase DragViewModel { get; set; }

  /// <summary>
  /// Load more (older) items into the file list if the user scrolls this percentage to the bottom
  /// </summary>
  private const int loadMorePercent_ = 90;

  private double scrollPercent_;
  public double ScrollPercent
  {
    get => scrollPercent_;
    set
    {
      if (scrollPercent_ > loadMorePercent_) 
      {
        scrollPercent_ = value;
        return; 
      }
      scrollPercent_ = value;

      if (scrollPercent_ < loadMorePercent_) { return; }
      Dispatcher.UIThread.Invoke(async () => await FileService.LoadMore());
    }
  }

  public IFileService FileService { get; }
  public IFitEditService FitEdit { get; }
  public IGarminConnectClient Garmin { get; }
  public IStravaClient Strava { get; }

  private readonly ITaskService tasks_;
  private readonly IStorageAdapter storage_;
  private readonly ISupabaseAdapter supa_;
  private readonly ILogViewModel log_;
  private readonly IBrowser browser_;

  public FileViewModel(
    ITaskService tasks,
    IFileService fileService,
    IFitEditService fitEdit,
    IGarminConnectClient garmin,
    IStravaClient strava,
    IStorageAdapter storage,
    ISupabaseAdapter supa,
    IBrowser browser,
    ILogViewModel log,
    FileDeleteViewModel fileDeleteViewModel,
    FileRemoteDeleteViewModel fileRemoteDeleteViewModel,
    DragViewModel dragViewModel
  )
  {
    tasks_ = tasks;
    FileService = fileService;
    FitEdit = fitEdit;
    Garmin = garmin;
    Strava = strava;
    supa_ = supa;
    storage_ = storage;
    log_ = log;
    browser_ = browser;

    FileDeleteViewModel = fileDeleteViewModel;
    FileRemoteDeleteViewModel = fileRemoteDeleteViewModel;
    DragViewModel = dragViewModel;

    FileService.SubscribeAdds(SubscribeChanges);

    if (fileService.Files == null) { return; }
    foreach (var file in fileService.Files)
    {
      SubscribeChanges(file);
    }

    _ = Task.Run(() =>
    {
      if (OperatingSystem.IsWindows())
      {
        ReadMtpDevicesWindows();
      }
      else
      {
        ReadMtpDevices();
      }
    });
  }

  // On Windows, WDM has a permanent connection to MTP devices so we can't connect.
  // We use a library which uses WDM to interact with MTP devices
  private static void ReadMtpDevicesWindows()
  {
    if (!OperatingSystem.IsWindows() || !OperatingSystem.IsWindowsVersionAtLeast(7)) { return; }

#pragma warning disable CA1416 // Validate platform compatibility. We already validated.
    List<MediaDevice> devices = MediaDevice.GetDevices().Where(d => d.Manufacturer.ToLower() == "garmin").ToList();

    string targetDir = $"C:/Users/doug/AppData/Local/FitEdit-Data/MTP";
    Directory.CreateDirectory(targetDir);

    foreach (MediaDevice device in devices)
    {
      device.Connect();
      if (!device.IsConnected) { continue; }

      MediaDirectoryInfo activityDir = device.GetDirectoryInfo("\\Internal storage/GARMIN/Activity");
      IEnumerable<MediaFileInfo> fitFiles = activityDir.EnumerateFiles("*.fit");

      //string[] files = device.GetFiles(@"\\Internal storage/GARMIN/Activity");
      //foreach (string file in files)
      //{
      //  using var fs = new FileStream($"{targetDir}/{Path.GetFileName(file)}", FileMode.Create);
      //  device.DownloadFile(file, fs);
      //}

      IEnumerable<MediaFileInfo> files = fitFiles.Where(f => f.LastWriteTime > DateTime.UtcNow - TimeSpan.FromDays(7));
      foreach (var file in files)
      {
        using var fs = new FileStream($"{targetDir}/{file.Name}", FileMode.Create);
        device.DownloadFile(file.FullName, fs);
      }
    }

#pragma warning restore CA1416 
  }

  private static void ReadMtpDevices()
  {
    const ushort GARMIN = 0x091e;
    var deviceList = new RawDeviceList();
    var garminDevices = deviceList.Where(d => d.DeviceEntry.VendorId == GARMIN).ToList();
    foreach (RawDevice rawDevice in garminDevices)
    {
      RawDevice rd = rawDevice;
      using var device = new Device(ref rd, cached: true); // TODO catch OpenedDeviceException

      if (device == null) { continue; }

      Log.Info($"Found Garmin device {device.GetModelName() ?? "(unknown)"}");
      Log.Info($"Found device serial # {device.GetSerialNumber() ?? "unknown"}");
      
      IEnumerable<Nmtp.DeviceStorage> storages = device.GetStorages();

      foreach (var storage in storages)
      {
        IEnumerable<Nmtp.Folder> folders = device.GetFolderList(storage.Id);
        var activityFolder = folders.FirstOrDefault(folder => folder.Name == "Activity");

        if (activityFolder.FolderId <= 0) { continue; }

        List<Nmtp.File> files = device
          .GetFiles(progress =>
          {
            Log.Info($"List files progress: {progress * 100:##.#}%");
            return true;
          })
          .Where(file => file.ParentId == activityFolder.FolderId)
          .Where(file => file.FileName.EndsWith(".fit"))
          .Where(file => DateTime.UnixEpoch + TimeSpan.FromSeconds(file.ModificationDate) > DateTime.UtcNow - TimeSpan.FromDays(7))
          .ToList();
      
	      string dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FitEdit-Data", "MTP");
        Directory.CreateDirectory(dir);

        foreach (Nmtp.File file in files)
        {
          Console.WriteLine($"Found file {file.FileName}");

          device.GetFile(file.ItemId, $"{dir}/{file.FileName}", progress =>
          {
            Log.Info($"Download progress {file.FileName} {progress * 100:##.#}%");
            return false;
          });
        }
      }
    }
  }

  private void SubscribeChanges(UiFile file)
  {
    if (file.Activity is null) { return; }

    file.Activity.ObservableForProperty(x => x.Name).Subscribe(async _ =>
    {
      await FileService.UpdateAsync(file.Activity);
      await supa_.UpdateAsync(file.Activity);

      if (!FitEdit.IsActive) { return; }
      if (!Garmin.IsSignedIn) { return; }
      if (!long.TryParse(file.Activity.SourceId, out long id)) { return; }
      await Garmin.SetActivityName(id, file.Activity.Name ?? "");
    });

    file.Activity.ObservableForProperty(x => x.Description).Subscribe(async _ =>
    {
      await FileService.UpdateAsync(file.Activity);
      await supa_.UpdateAsync(file.Activity);

      if (!FitEdit.IsActive) { return; }
      if (!Garmin.IsSignedIn) { return; }
      if (!long.TryParse(file.Activity.SourceId, out long id)) { return; }
      await Garmin.SetActivityDescription(id, file.Activity.Description ?? "");
    });

    file.Activity.ObservableForProperty(x => x.OnlineUrl).Subscribe(async _ =>
    {
      await FileService.UpdateAsync(file.Activity);
      await supa_.UpdateAsync(file.Activity);
    });
    
    file.Activity.ObservableForProperty(x => x.SourceId).Subscribe(async _ =>
    {
      await FileService.UpdateAsync(file.Activity);
      await supa_.UpdateAsync(file.Activity);
    });
  }

  public async Task HandleImportClicked()
  {
    Log.Info($"{nameof(HandleImportClicked)}");

    // On macOS and iOS, the file picker must run on the main thread
    FileReference? file = await storage_.OpenFileAsync();

    _ = Task.Run(async () => await ImportAsync(file));
  }

  public void HandleFileDropped(IStorageFile? file)
  {
    Log.Info($"{nameof(HandleFileDropped)}");

    _ = Task.Run(async () =>
    {
      FileReference? fr = await FileReference.FromStorage(file);
      await ImportAsync(fr);
    });
  }

  private async Task ImportAsync(FileReference? file)
  { 
    if (file == null)
    {
      Log.Info("No file selected in the file dialog");
      return;
    }

    if (!file.Name.EndsWith(".zip"))
    {
      await Persist(file);
      return;
    }

    List<FileReference> files = Zip.Unzip(file);
    foreach (FileReference f in files)
    {
      await Persist(f);
    }
  }

  /// <summary>
  /// Import a file and associate it with an existing activity.
  /// </summary>
  public async Task HandleActivityImportClicked(LocalActivity? act)
  {
    Log.Info($"{nameof(HandleActivityImportClicked)} clicked");

    if (act == null) { return; }
    if (act.File != null) { return; }

    // On macOS and iOS, the file picker must run on the main thread
    FileReference? file = await storage_.OpenFileAsync();

    if (file == null)
    {
      Log.Info("No file selected in the file dialog");
      return;
    }

    if (file.Name.EndsWith(".zip"))
    {
      List<FileReference> files = Zip.Unzip(file);
      act.File = files.FirstOrDefault();
    }
    else
    {
      act.File = file;
    }

    if (act.File is null) { return; }

    await supa_.UpdateAsync(act); // Sets LocalActivity.BucketUrl

    // TODO Show result
  }

  private async Task<UiFile?> Persist(FileReference? file)
  {
    if (file == null) { return null; }

    return await Persist(new LocalActivity
    {
      Name = file.Name,
      Id = file.Id,
      File = file,
    });
  }

  /// <summary>
  /// Load first few messages of the given FIT file to get its start time
  /// </summary>
  private async Task<DateTime> GetStartTimeAsync(FileReference? file)
  {
    if (file is null) { return default; }

    using var ms = new MemoryStream(file.Bytes);
    await log_.Log($"Reading FIT file {file.Name}");

    var reader = new Reader();
    if (reader.TryGetDecoder(file.Name, ms, out FitFile fit, out var decoder))
    {
      await reader.ReadOneAsync(ms, decoder, 100);
    }

    return fit?.GetStartTime() ?? default;
  }

  private async Task<UiFile> Persist(LocalActivity act)
  { 
    if (act.StartTime == default)
    {
      act.StartTime = await GetStartTimeAsync(act.File);
    }

    UiFile sf = await Task.Run(async () =>
    {
      bool ok = await FileService.CreateAsync(act);

      if (ok) { Log.Info($"Persisted activity {act}"); }
      else { Log.Error($"Could not persist activity {act}"); }

      return new UiFile { Activity = act };
    });

    await Dispatcher.UIThread.InvokeAsync(() =>
    {
      FileService.Add(sf);
      FileService.MainFile = sf;
    });

    await supa_.UpdateAsync(act);

    return sf;
  }

  public void HandleDeleteClicked(UiFile uif) => FileDeleteViewModel.BeginDelete(uif);
  public void HandleRemoteDeleteClicked(UiFile uif) => FileRemoteDeleteViewModel.BeginDelete(uif);

  private void Remove(int index)
  {
    UiFile file = FileService.Files[index];
    FileService.Files.Remove(file);

    _ = Task.Run(async () =>
    {
      await FileService.DeleteAsync(file.Activity);
      await supa_.DeleteAsync(file.Activity);
    });
  }

  public void LoadOrUnload(UiFile uif)
  {
    if (!uif.IsLoaded)
    {
      _ = Task.Run(async () => await LoadFile(uif).AnyContext());
      return;
    }

    UnloadFile(uif);
  }

  private void UnloadFile(UiFile? uif)
  {
    if (uif == null) { return; }
    uif.Progress = 0;
    uif.IsLoaded = false;
    FileService.MainFile = FileService.Files.FirstOrDefault(f => f.IsLoaded);
  }

  private async Task LoadFile(UiFile? uif)
  {
    if (uif == null || uif.Activity == null || uif.Activity.File == null)
    {
      Log.Info("Could not load null file");
      return;
    }

    if (uif.FitFile != null) 
    {
      Log.Info($"File {uif.Activity.Name} is already loaded");
      uif.Progress = 100;
      uif.IsLoaded = true;

      await Dispatcher.UIThread.InvokeAsync(() =>
      {
        FileService.MainFile = uif;
      });
      return;
    }

    LocalActivity? act = await FileService.ReadAsync(uif.Activity.Id);
    FileReference? file = act?.File;
    uif.Activity.File = act?.File;

    if (file == null) 
    {
      Log.Error($"Could not load file {uif.Activity.Name}");
      return;
    }

    try
    {
      Log.Info($"Got file {file.Name} ({file.Bytes.Length} bytes)");

      // Handle FIT files
      string extension = Path.GetExtension(file.Name);

      //if (extension.ToLower() != ".fit")
      //{
      //  Log.Info($"Unsupported extension {extension}");
      //  return;
      //}

      using var ms = new MemoryStream(file.Bytes);
      await log_.Log($"Reading FIT file {file.Name}");

      var reader = new Reader();
      if (!reader.TryGetDecoder(file.Name, ms, out FitFile fit, out var decoder))
      {
        return;
      }

      long lastPosition = 0;
      long resolution = 5 * 1024; // report progress every 5 kB

      // Instead of reading all FIT messages at once,
      // Read just a few FIT messages at a time so that other tasks can run on the main thread e.g. in WASM
      uif.Progress = 0;

      try
      {
        while (await reader.ReadOneAsync(ms, decoder, 100))
        {
          if (ms.Position - lastPosition < resolution)
          {
            continue;
          }

          double progress = (double)ms.Position / ms.Length * 100;
          uif.Progress = progress;
          await TaskUtil.MaybeYield();
          lastPosition = ms.Position;
        }
      }
      catch (Exception e)
      {
        Log.Error($"{e}");
        // Try to proceed despite the exception
      }

      fit.ForwardfillEvents();
      uif.FitFile = fit;
      uif.IsLoaded = true;

      // Do on the main thread because there are subscribers which update the UI
      await Dispatcher.UIThread.InvokeAsync(() =>
      {
        FileService.MainFile = uif;
      });

      uif.Progress = 100;
      await log_.Log($"Done reading FIT file");
      Log.Info(fit.Print(showRecords: false));
    }
    catch (Exception e)
    {
      Log.Error($"{e}");
    }
  }

  public async void HandleExportClicked(UiFile uif)
  {
    int index = FileService.Files.IndexOf(uif);
    if (index < 0 || FileService.Files.Count == 0)
    {
      Log.Info("No file selected; cannot export file");
      return;
    }

    await Export(FileService.Files[index]);
  }

  private async Task Export(UiFile? file)
  { 
    if (file == null) { return; }
    if (file.Activity == null) { return; }
    if (file.Activity.File == null) { return; }

    if (file.FitFile != null) 
    { 
      file.Activity.File.Bytes = file.FitFile.GetBytes();
    }
    else
    {
      // Load the file bytes from disk, without parsing them as a FIT file
      LocalActivity? tmp = await FileService.ReadAsync(file.Activity.Id);
      if (tmp is null) { return; }
      if (tmp.File is null) { return; }
      file.Activity.File.Bytes = tmp.File.Bytes;
    }

    Log.Info($"Exporting \"{file.Activity.Name}\"...");

    try
    {
      string? name = Path.GetFileNameWithoutExtension(file.Activity.Name) ?? "FitEdit Export";
      string extension = Path.GetExtension(file.Activity.File.Name);
      // On macOS and iOS, the file save dialog must run on the main thread
      await storage_.SaveAsync(new FileReference($"{name}{extension}", file.Activity.File.Bytes));
    }
    catch (Exception e)
    {
      Log.Info($"{e}");
    }
  }

  public void HandleSplitByLapsClicked(UiFile? file) => _ = Task.Run(() => SplitByLap(file));

  private async Task SplitByLap(UiFile? file)
  {
    if (file == null) { return; }
    if (file.FitFile == null) { return; }

    List<FitFile> fits = file.FitFile.SplitByLap();

    int i = 0;
    foreach (FitFile fit in fits)
    {
      i++;
      await Persist(new FileReference
      (
        $"{file.Activity?.Name} - Lap {i} of {fits.Count}",
        fit.GetBytes()
      ));
    }
  }

  public void HandleMergeClicked() => _ = Task.Run(Merge);

  private async Task Merge()
  { 
    List<UiFile> files = FileService.Files.Where(f => f.IsLoaded).ToList();
    if (files.Count < 2) { return; }
    if (files.Any(f => f.FitFile == null)) { return; }

    var merged = new FitFile();

    foreach (var file in files)
    {
      merged.Append(file.FitFile);
    }

    var activity = new LocalActivity
    {
      Id = $"{Guid.NewGuid()}",
      Name = $"Merged {string.Join("-", files.Select(f => f.Activity?.Name).Where(s => !string.IsNullOrEmpty(s)))}"
    };

    var fileRef = new FileReference(activity.Name, merged.GetBytes());
    activity.File = fileRef;

    UiFile? sf = await Persist(fileRef);

    if (sf == null) { return; }
    
    sf.FitFile = merged;
    sf.IsLoaded = true;
    sf.Progress = 100;
  }

  public void HandleRepairSubtractivelyClicked(UiFile uif)
  {
    int index = FileService.Files.IndexOf(uif);
    if (index < 0 || index >= FileService.Files.Count)
    {
      Log.Info("No file selected; cannot repair file");
      return;
    }

    _ = Task.Run(async () => await RepairAsync(FileService.Files[index], RepairStrategy.Subtractive));
  }

  public void HandleRepairAdditivelyClicked(UiFile uif)
  {
    int index = FileService.Files.IndexOf(uif);
    if (index < 0 || index >= FileService.Files.Count)
    {
      Log.Info("No file selected; cannot repair file");
      return;
    }

    _ = Task.Run(async () => await RepairAsync(FileService.Files[index], RepairStrategy.Additive));
  }

  public void HandleRepairBackfillClicked(UiFile uif)
  {
    int index = FileService.Files.IndexOf(uif);
    if (index < 0 || index >= FileService.Files.Count)
    {
      Log.Info("No file selected; cannot repair file");
      return;
    }

    _ = Task.Run(async () => await RepairAsync(FileService.Files[index], RepairStrategy.BackfillMissingFields));
  }

  public async Task<UiFile?> RepairAsync(UiFile? file, RepairStrategy strategy)
  {
    if (file == null) { return null; }
    if (file.FitFile == null) { return null; }

    FitFile? fit = strategy switch
    {
      RepairStrategy.Additive => file.FitFile.RepairAdditively(),
      RepairStrategy.BackfillMissingFields => file.FitFile.RepairBackfillMissingFields(),
      _ => file.FitFile.RepairSubtractively(),
    };

    return await Persist(new FileReference
    (
      $"Repaired {file.Activity?.Name}",
      fit.GetBytes()
    ));
  }

  public async Task HandleOpenGarminUploadPageClicked() => await browser_.OpenAsync("https://connect.garmin.com/modern/import-data");
  public async Task HandleOpenStravaUploadPageClicked() => await browser_.OpenAsync("https://www.strava.com/upload/select");

  public async Task HandleViewOnlineClicked(LocalActivity? act)
  {
    if (act == null) { return; }
    await browser_.OpenAsync(act.OnlineUrl);
  }

  public void HandleGarminUploadClicked(LocalActivity? act)
  {
    if (!FitEdit.IsActive) { return; }

    _ = Task.Run(async () => await UploadGarminActivity(act));
  }

  /// <summary>
  /// Upload a file to Garmin. We don't associate it with the activity because that will happen in the Garmin webhook handler.
  /// </summary>
  private async Task UploadGarminActivity(LocalActivity? act)
  {
    if (act is null) { return; }

    // Load the file bytes from disk, without parsing them as a FIT file
    LocalActivity? tmp = await FileService.ReadAsync(act.Id);
    if (tmp is null) { return; }
    act.File = tmp.File;

    if (act.File?.Bytes is null) { return; }
    if (act.File.Bytes.Length == 0) { return; }

    using var ms = new MemoryStream(act.File.Bytes);

    (bool ok, long id) = await Garmin
      .UploadActivity(ms, new FileFormat { FormatKey = "fit" })
      .AnyContext();

    // Save the new Garmin activity ID
    if (!ok || id < 0) { return; }
    act.Source = ActivitySource.GarminConnect;
    act.SourceId = $"{id}";

    // TODO Show result
  }

  public void HandleStravaUploadClicked(LocalActivity? act)
  {
    if (!FitEdit.IsActive) { return; }

    _ = Task.Run(async () => await UploadStravaActivity(act));
  }

  private async Task UploadStravaActivity(LocalActivity? act)
  {
    if (act is null) { return; }

    // Load the file bytes from disk, without parsing them as a FIT file
    LocalActivity? tmp = await FileService.ReadAsync(act.Id);
    if (tmp is null) { return; }
    act.File = tmp.File;

    if (act.File?.Bytes is null) { return; }
    if (act.File.Bytes.Length == 0) { return; }

    using var ms = new MemoryStream(act.File.Bytes);

    (bool ok, long id) = await Strava
      .UploadActivityAsync(ms)
      .AnyContext();

    // Save the new Strava activity ID
    if (!ok || id < 0) { return; }
    act.Source = ActivitySource.Strava;
    act.SourceId = $"{id}";
  }

  public void HandleSyncFromGarminClicked() => _ = Task.Run(SyncFromGarminAsync);

  private async Task SyncFromGarminAsync()
  {
    var task = new UserTask { Name = "Syncing from Garmin..." };
    tasks_.Add(task);

    List<GarminActivity> activities = await Garmin.GetAllActivitiesAsync(task);

    if (task.IsCanceled) { return; }

    List<(GarminActivity, string, string, DateTime)> byTimestamp = activities
      .Select(a => (a, $"{a.ActivityId}", a.ActivityName, a.GetStartTime()))
      .ToList();

    IEnumerable<GarminActivity> filtered = await FileService.FilterExistingAsync(task, byTimestamp);
    List<(long, LocalActivity)> mapped = filtered.Select(GarminActivityMapper.MapLocalActivity).ToList();

    if (task.IsCanceled) { return; }

    await Garmin.DownloadInParallelAsync(task, mapped, Persist);

    task.Status = mapped.Count switch
    {
      0 => "No new activities from Garmin",
      1 => $"Downloaded 1 new activity from Garmin",
      _ => $"Downloaded {mapped.Count} new activities from Garmin",
    };

    task.IsComplete = true;
  }
  
  public void HandleSyncFromStravaClicked() => _ = Task.Run(SyncFromStravaAsync);

  private void SyncFromStravaAsync()
  {
    var task = new UserTask { Name = "Syncing from Strava..." };
    tasks_.Add(task);
    task.Status = "Continue?";

    task.NextAction = async () =>
    {
      task.Status = "Getting list of activities from Strava";
      List<StravaActivity> activities = await Strava.ListAllActivitiesAsync(task, task.CancellationToken);

      if (task.IsCanceled) { return; }

      List<(StravaActivity, string, string, DateTime)> byTimestamp = activities
        .Select(a => (a, $"{a.Id}", a.Name ?? "", a.GetStartTime()))
        .ToList();

      IEnumerable<StravaActivity> filtered = await FileService.FilterExistingAsync(task, byTimestamp);
      List<(long id, LocalActivity activity)> mapped = filtered.Select(StravaActivityMapper.MapLocalActivity).ToList();

      if (task.IsCanceled) { return; }

      await Strava.DownloadInParallelAsync(task, mapped, Persist);

      task.Status = mapped.Count switch
      {
        0 => "No new activities from Strava",
        1 => $"Downloaded 1 new activity from Strava",
        _ => $"Downloaded {mapped.Count} new activities from Strava",
      };

      task.IsComplete = true;
    };
    task.IsConfirmed = true;
  }
}