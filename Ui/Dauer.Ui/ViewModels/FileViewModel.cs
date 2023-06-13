using Dauer.Data.Fit;
using Dauer.Model;
using Dauer.Ui.Extensions;
using Dauer.Ui.Infra;
using Dauer.Ui.Infra.Adapters.Storage;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IFileViewModel
{
  public FitFile? FitFile { get; set; }
}

public class DesignFileViewModel : FileViewModel
{

  public DesignFileViewModel() : base(new NullStorageAdapter(), new NullWebAuthenticator(), new DesignLogViewModel()) { }
}

public class FileViewModel : ViewModelBase, IFileViewModel
{
  private Model.File? lastFile_ = null;
  private readonly IStorageAdapter storage_;
  private readonly IWebAuthenticator auth_;
  private readonly ILogViewModel log_;

  [Reactive] public double Progress { get; set; }
  [Reactive] public FitFile? FitFile { get; set; }

  public FileViewModel(IStorageAdapter storage, IWebAuthenticator auth, ILogViewModel log)
  {
    storage_ = storage;
    auth_ = auth;
    log_ = log;
  }

  public async void HandleSelectFileClicked()
  {
    Log.Info("Select file clicked");

    try
    {
      // On macOS and iOS, the file picker must run on the main thread
      Model.File? file = await storage_.OpenFileAsync();
      if (file == null)
      {
        Log.Info("Could not load file");
        return;
      }
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
      FitFile = fit;
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

      string name = Path.GetFileNameWithoutExtension(lastFile_.Name);
      string extension = Path.GetExtension(lastFile_.Name);
      // On macOS and iOS, the file save dialog must run on the main thread
      await storage_.SaveAsync(new Model.File($"{name}_edit.{extension}", lastFile_.Bytes));
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
