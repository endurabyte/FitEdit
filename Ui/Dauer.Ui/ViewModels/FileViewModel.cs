using Avalonia.Threading;
using Dauer.Data;
using Dauer.Data.Fit;
using Dauer.Model;
using Dauer.Model.Extensions;
using Dauer.Model.Storage;
using Dauer.Model.Web;
using Dauer.Services;
using Dauer.Ui.Extensions;
using Dauer.Ui.Model.Supabase;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IFileViewModel
{
}

public class DesignFileViewModel : FileViewModel
{
  public DesignFileViewModel() : base(
    new NullFileService(),
    new NullStorageAdapter(),
    new NullFitEditService(),
    new NullSupabaseAdapter(),
    new NullBrowser(),
    new DesignLogViewModel()) { }
}

public class FileViewModel : ViewModelBase, IFileViewModel
{
  [Reactive] public int SelectedIndex { get; set; }

  public IFileService FileService { get; }
  public IFitEditService FitEdit { get; }
  private readonly IStorageAdapter storage_;
  private readonly ISupabaseAdapter supa_;
  private readonly ILogViewModel log_;
  private readonly IBrowser browser_;

  public FileViewModel(
    IFileService fileService,
    IStorageAdapter storage,
    IFitEditService fitEdit,
    ISupabaseAdapter supa,
    IBrowser browser,
    ILogViewModel log
  )
  {
    FileService = fileService;
    FitEdit = fitEdit;
    supa_ = supa;
    storage_ = storage;
    log_ = log;
    browser_ = browser;
    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      int i = property.Value;
      if (i < 0 || i >= fileService.Files.Count) { return; }
      fileService.MainFile = fileService.Files[i];
    });

    FileService.SubscribeAdds(file => file.SubscribeToIsLoaded(LoadOrUnload));

    foreach (var file in fileService.Files)
    {
      file.SubscribeToIsLoaded(LoadOrUnload);
    }
  }

  public async void HandleImportClicked()
  {
    Log.Info($"{nameof(HandleImportClicked)} clicked");

    // On macOS and iOS, the file picker must run on the main thread
    FileReference? file = await storage_.OpenFileAsync();

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

  public async void HandleActivityImportClicked(DauerActivity? act)
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

    if (!await supa_.UpdateAsync(act)) // Sets DauerActivity.BucketUrl
    {
      // TODO Show error
    }
  }

  private async Task<UiFile?> Persist(FileReference? file)
  {
    if (file == null) { return null; }

    return await Persist(new DauerActivity
    {
      Name = file.Name,
      Id = file.Id,
      File = file,
    });
  }

  private async Task<UiFile> Persist(DauerActivity act)
  { 
    UiFile sf = await Task.Run(async () =>
    {
      bool ok = await FileService.CreateAsync(act);

      if (ok) { Log.Info($"Persisted activity {act}"); }
      else { Log.Error($"Could not persist activity {act}"); }

      return new UiFile { Activity = act };
    });

    FileService.Add(sf);
    FileService.MainFile = sf;

    return sf;
  }

  public async void HandleRemoveClicked()
  {
    int index = SelectedIndex;
    if (index < 0 || index >= FileService.Files.Count)
    {
      SelectedIndex = 0;
      Log.Info("No file selected; cannot remove file");
      return;
    }

    await Remove(index);
    SelectedIndex = Math.Min(index, FileService.Files.Count);
  }

  private async Task Remove(int index)
  {
    UiFile file = FileService.Files[index];

    await FileService.DeleteAsync(file.Activity);
    FileService.Files.Remove(file);
  }

  private void LoadOrUnload(UiFile sf)
  {
    if (sf.IsVisible)
    {
      _ = Task.Run(async () => await LoadFile(sf).AnyContext());
    }
    else
    {
      UnloadFile(sf);
    }
  }

  private void UnloadFile(UiFile? sf)
  {
    if (sf == null) { return; }
    FileService.MainFile = FileService.Files.FirstOrDefault(f => f.IsVisible);
    sf.Progress = 0;
  }

  private async Task LoadFile(UiFile? sf)
  {
    if (sf == null || sf.Activity == null || sf.Activity.File == null)
    {
      Log.Info("Could not load null file");
      return;
    }

    if (sf.FitFile != null) 
    {
      Log.Info($"File {sf.Activity.Name} is already loaded");
      sf.Progress = 100;
      return;
    }

    DauerActivity? act = await FileService.ReadAsync(sf.Activity.Id);
    FileReference? file = act?.File;
    sf.Activity.File = act?.File;

    if (file == null) 
    {
      Log.Error($"Could not load file {sf.Activity.Name}");
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
      sf.Progress = 0;
      while (await reader.ReadOneAsync(ms, decoder, 100))
      {
        if (ms.Position - lastPosition < resolution)
        {
          continue;
        }

        double progress = (double)ms.Position / ms.Length * 100;
        sf.Progress = progress;
        await TaskUtil.MaybeYield();
        lastPosition = ms.Position;
      }

      fit.ForwardfillEvents();

      // Do on the main thread because there are subscribers which update the UI
      await Dispatcher.UIThread.InvokeAsync(() =>
      {
        sf.FitFile = fit;
        FileService.MainFile = null; // Trigger notification
        FileService.MainFile = sf;
      });

      sf.Progress = 100;
      await log_.Log($"Done reading FIT file");
      Log.Info(fit.Print(showRecords: false));
    }
    catch (Exception e)
    {
      Log.Error($"{e}");
      return;
    }
  }

  public async void HandleExportClicked()
  {
    int index = SelectedIndex;
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
    if (file.FitFile == null) { return; }

    Log.Info($"Exporting {file.Activity.Name}...");

    try
    {
      byte[] bytes = file.FitFile.GetBytes();
      file.Activity.File.Bytes = bytes;

      string name = Path.GetFileNameWithoutExtension(file.Activity.File.Name);
      string extension = Path.GetExtension(file.Activity.File.Name);
      // On macOS and iOS, the file save dialog must run on the main thread
      await storage_.SaveAsync(new FileReference($"{name}_edit.{extension}", bytes));
    }
    catch (Exception e)
    {
      Log.Info($"{e}");
    }
  }

  public async void HandleMergeClicked()
  {
    List<UiFile> files = FileService.Files.Where(f => f.IsVisible).ToList();
    if (files.Count < 2) { return; }
    if (files.Any(f => f.FitFile == null)) { return; }

    var merged = new FitFile();

    foreach (var file in files)
    {
      merged.Append(file.FitFile);
    }

    var activity = new DauerActivity
    {
      Id = $"{Guid.NewGuid()}",
      Name = $"Merged {string.Join("-", files.Select(f => f.Activity?.Name).Where(s => !string.IsNullOrEmpty(s)))}"
    };

    var fileRef = new FileReference(activity.Name, merged.GetBytes());
    activity.File = fileRef;

    UiFile? sf = await Persist(fileRef);

    if (sf == null) { return; }
    
    sf.FitFile = merged;
    sf.IsVisible = true;
    sf.Progress = 100;
  }

  public void HandleRepairSubtractivelyClicked()
  {
    int index = SelectedIndex;
    if (index < 0 || index >= FileService.Files.Count)
    {
      Log.Info("No file selected; cannot repair file");
      return;
    }

    _ = Task.Run(async () => await RepairAsync(FileService.Files[index], RepairStrategy.Subtractive));
  }

  public void HandleRepairAdditivelyClicked()
  {
    int index = SelectedIndex;
    if (index < 0 || index >= FileService.Files.Count)
    {
      Log.Info("No file selected; cannot repair file");
      return;
    }

    _ = Task.Run(async () => await RepairAsync(FileService.Files[index], RepairStrategy.Additive));
  }

  public void HandleRepairAddMissingFieldsClicked()
  {
    int index = SelectedIndex;
    if (index < 0 || index >= FileService.Files.Count)
    {
      Log.Info("No file selected; cannot repair file");
      return;
    }

    _ = Task.Run(async () => await RepairAsync(FileService.Files[index], RepairStrategy.AddMissingFields));
  }

  public async Task<UiFile?> RepairAsync(UiFile? file, RepairStrategy strategy)
  {
    if (file == null) { return null; }
    if (file.FitFile == null) { return null; }

    FitFile? fit = strategy switch
    {
      RepairStrategy.Additive => file.FitFile.RepairAdditively(),
      RepairStrategy.AddMissingFields => file.FitFile.RepairAddMissingFields(),
      _ => file.FitFile.RepairSubtractively(),
    };

    return await Persist(new FileReference
    (
      $"Repaired {file.Activity?.Name}",
      fit.GetBytes()
    ));
  }

  public async Task HandleGarminUploadClicked() => await browser_.OpenAsync("https://connect.garmin.com/modern/import-data");
  public async Task HandleStravaUploadClicked() => await browser_.OpenAsync("https://www.strava.com/upload/select");

  public async Task HandleViewOnlineClicked(DauerActivity? act)
  {
    if (act == null) { return; }
    await browser_.OpenAsync(act.OnlineUrl);
  }
}