using System.Collections.ObjectModel;
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
  private BlobFile? lastFile_ = null;

  [Reactive] public double Progress { get; set; }
  [Reactive] public ObservableCollection<BlobFile> Files { get; set; } = new();
  [Reactive] public int SelectedIndex { get; set; }

  private readonly IFileService fileService_;
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
    fileService_ = fileService;
    db_ = db;
    storage_ = storage;
    auth_ = auth;
    log_ = log;

    SyncFilesList();
    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      _ = Task.Run(async () =>
      {
        int index = property.Value;
        if (index < 0 || index >= Files.Count) { return; }

        await LoadFile(Files[index]).AnyContext();
      });
    });

    SyncFilesList();
  }

  private void SyncFilesList()
  {
    _ = Task.Run(async () =>
    {
      List<BlobFile> files = await db_.GetAllAsync();
      Files.Clear();
      Files.AddRange(files);
    });
  }

  public async void HandleSelectFileClicked()
  {
    Log.Info("Select file clicked");

    // On macOS and iOS, the file picker must run on the main thread
    BlobFile? file = await storage_.OpenFileAsync();

    if (file == null)
    {
      Log.Info("No file selected in the file dialog");
      return;
    }

    _ = Task.Run(async () =>
    {
      bool ok = await db_.InsertAsync(file).AnyContext();

      if (ok) { Log.Info($"Persisted file {file}"); }
      else { Log.Error($"Could not persist file {file}"); }

      SyncFilesList();
      await LoadFile(file).AnyContext();
    });
  }

  public async void HandleForgetFileClicked()
  {
    int index = SelectedIndex;
    if (index < 0 || Files.Count == 0)
    {
      Log.Info("No file selected; cannot forget file");
      return;
    }

    var file = Files[index];
    if (file == null) { return; }

    await db_.DeleteAsync(file).AnyContext();
    SyncFilesList();

    SelectedIndex = Math.Min(index, Files.Count);
  }

  public async Task LoadFile(BlobFile? file)
  {
    if (file == null)
    {
      Log.Info("Could not load null file");
      return;
    }

    if (ReferenceEquals(file, lastFile_))
    {
      Log.Info($"File {file.Name} already loaded");
      return;
    }

    try
    {
      Log.Info($"Got file {file.Name} ({file.Bytes.Length} bytes)");
      lastFile_ = file;

      // Handle FIT files
      string extension = Path.GetExtension(file.Name);

      if (extension.ToLower() != ".fit")
      {
        Log.Info($"Unsupported extension {extension}");
        return;
      }

      using var ms = new MemoryStream(lastFile_.Bytes);
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
      Progress = 0;
      while (await reader.ReadOneAsync(ms, decoder, 100))
      {
        if (ms.Position - resolution > lastPosition)
        {
          continue;
        }

        double progress = (double)ms.Position / ms.Length * 100;
        Progress = progress;
        await TaskUtil.MaybeYield();
        lastPosition = ms.Position;
      }

      fit.ForwardfillEvents();
      Progress = 100;
      await log_.Log($"Done reading FIT file");

      Log.Info(fit.Print(showRecords: false));
      fileService_.FitFile = fit;
    }
    catch (Exception e)
    {
      Log.Error($"{e}");
    }
  }

  public async void HandleDownloadFileClicked()
  {
    Log.Info("Download file clicked...");

    try
    {
      if (lastFile_ == null)
      {
        await log_.Log("Cannot download file; none has been uploaded");
        return;
      }

      var ms = new MemoryStream();
      new Writer().Write(fileService_.FitFile, ms);
      byte[] bytes = ms.ToArray();

      string name = Path.GetFileNameWithoutExtension(lastFile_.Name);
      string extension = Path.GetExtension(lastFile_.Name);
      // On macOS and iOS, the file save dialog must run on the main thread
      await storage_.SaveAsync(new BlobFile($"{name}_edit.{extension}", bytes));
    }
    catch (Exception e)
    {
      Log.Info($"{e}");
    }
  }

  public void HandleAuthorizeClicked()
  {
    Log.Info($"{nameof(HandleAuthorizeClicked)}");
    Log.Info($"Starting {auth_.GetType()}.{nameof(IWebAuthenticator.AuthenticateAsync)}");

    auth_.AuthenticateAsync();
  }
}
