using Avalonia.Threading;
using Dauer.Data.Fit;
using Dauer.Model;
using Dauer.Model.Data;
using Dauer.Model.Extensions;
using Dauer.Ui.Extensions;
using Dauer.Ui.Infra;
using Dauer.Ui.Infra.Adapters.Storage;
using DynamicData;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IFileViewModel
{
}

public class DesignFileViewModel : FileViewModel
{
  public DesignFileViewModel() : base(
    new FileService(),
    new NullDatabaseAdapter(),
    new NullStorageAdapter(),
    new NullWebAuthenticator(),
    new DesignLogViewModel()) { }
}

public class FileViewModel : ViewModelBase, IFileViewModel
{
  [Reactive] public int SelectedIndex { get; set; }

  public IFileService FileService { get; }
  private readonly IDatabaseAdapter db_;
  private readonly IStorageAdapter storage_;
  private readonly IWebAuthenticator auth_;
  private readonly ILogViewModel log_;

  public FileViewModel(
    IFileService fileService,
    IDatabaseAdapter db,
    IStorageAdapter storage,
    IWebAuthenticator auth,
    ILogViewModel log
  )
  {
    FileService = fileService;
    db_ = db;
    storage_ = storage;
    auth_ = auth;
    log_ = log;

    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      int i = property.Value;
      if (i < 0 || i >= fileService.Files.Count) { return; }
      fileService.MainFile = fileService.Files[i];
    });

    db.ObservableForProperty(x => x.Ready).Subscribe(property =>
    {
      if (!property.Value) { return; }
      InitFilesList();
    });
  }

  private void InitFilesList()
  {
    _ = Task.Run(async () =>
    {
      List<BlobFile> files = await db_.GetAllAsync().AnyContext();

      var sfs = files.Select(file => new SelectedFile
      {
        FitFile = null, // Don't parse the blobs, that would be too slow
        Blob = file,
      }).ToList();

      await Dispatcher.UIThread.InvokeAsync(() => FileService.Files.AddRange(sfs));

      foreach (var sf in sfs)
      {
        sf.SubscribeToIsLoaded(LoadOrUnload);
      }
    });
  }

  public async void HandleImportClicked()
  {
    Log.Info("Select file clicked");

    // On macOS and iOS, the file picker must run on the main thread
    BlobFile? file = await storage_.OpenFileAsync();

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

    List<BlobFile> files = Zip.Unzip(file);
    foreach (BlobFile f in files)
    {
      await Persist(f);
    }
  }

  private async Task<SelectedFile> Persist(BlobFile file)
  { 
    SelectedFile sf = await Task.Run(async () =>
    {
      bool ok = await db_.InsertAsync(file).AnyContext();

      if (ok) { Log.Info($"Persisted file {file}"); }
      else { Log.Error($"Could not persist file {file}"); }

      var sf = new SelectedFile { Blob = file };
      sf.SubscribeToIsLoaded(LoadOrUnload);
      return sf;
    });

    FileService.Files.Add(sf);
    FileService.MainFile = sf;

    return sf;
  }

  public async void HandleRemoveClicked()
  {
    int index = SelectedIndex;
    if (index < 0 || FileService.Files.Count == 0)
    {
      Log.Info("No file selected; cannot remove file");
      return;
    }

    await Remove(index);
    SelectedIndex = Math.Min(index, FileService.Files.Count);
  }

  private async Task Remove(int index)
  {
    SelectedFile file = FileService.Files[index];

    await db_.DeleteAsync(file.Blob);
    FileService.Files.Remove(file);
  }

  private void LoadOrUnload(SelectedFile sf)
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

  private void UnloadFile(SelectedFile? sf)
  {
    if (sf == null) { return; }
    FileService.MainFile = FileService.Files.FirstOrDefault(f => f.IsVisible);
    sf.Progress = 0;
  }

  private async Task LoadFile(SelectedFile? sf)
  {
    if (sf == null || sf.Blob == null)
    {
      Log.Info("Could not load null file");
      return;
    }

    if (sf.FitFile != null) 
    {
      Log.Info($"File {sf.Blob.Name} is already loaded");
      sf.Progress = 100;
      return;
    }

    BlobFile file = sf.Blob;

    try
    {
      Log.Info($"Got file {file.Name} ({file.Bytes.Length} bytes)");

      // Handle FIT files
      string extension = Path.GetExtension(file.Name);

      if (extension.ToLower() != ".fit")
      {
        Log.Info($"Unsupported extension {extension}");
        return;
      }

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
        if (ms.Position - resolution > lastPosition)
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

  private async Task Export(SelectedFile? file)
  { 
    if (file == null) { return; }
    if (file.Blob == null) { return; }
    if (file.FitFile == null) { return; }

    Log.Info($"Exporting {file.Blob.Name}...");

    try
    {
      byte[] bytes = file.FitFile.GetBytes();
      file.Blob.Bytes = bytes;

      string name = Path.GetFileNameWithoutExtension(file.Blob.Name);
      string extension = Path.GetExtension(file.Blob.Name);
      // On macOS and iOS, the file save dialog must run on the main thread
      await storage_.SaveAsync(new BlobFile($"{name}_edit.{extension}", bytes));
    }
    catch (Exception e)
    {
      Log.Info($"{e}");
    }
  }

  public async void HandleMergeClicked()
  {
    List<SelectedFile> files = FileService.Files.Where(f => f.IsVisible).ToList();
    if (files.Count < 2) { return; }
    if (files.Any(f => f.FitFile == null)) { return; }

    var merged = new FitFile();

    foreach (var file in files)
    {
      merged.Append(file.FitFile);
    }

    var blob = new BlobFile
    {
      Bytes = merged.GetBytes(),
      Name = $"Merged {string.Join("-", files.Select(f => f.Blob?.Name).Where(s => !string.IsNullOrEmpty(s)))}"
    };

    SelectedFile sf = await Persist(blob);
    sf.FitFile = merged;
    sf.IsVisible = true;
    sf.Progress = 100;
  }
}
