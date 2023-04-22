using Splat;
using Dauer.Ui.Adapters.Storage;
using Dauer.Services;
using ReactiveUI;
using Dauer.Data.Fit;
using System.Text;
using Avalonia.Threading;
using System.Collections.ObjectModel;
using Dauer.Model.Workouts;

namespace Dauer.Ui.ViewModels;

public interface IMainViewModel
{

}

public class MainViewModel : ViewModelBase, IMainViewModel
{
  private Models.File? lastFile_ = null;
  private readonly IStorageAdapter storage_;
  private readonly IFitService fit_;

  public ObservableCollection<string> Log { get; } = new();

  private string text_ = "Welcome to Dauer. Please load a FIT file.";
  public string Text 
  { 
    get => text_;
    set => this.RaiseAndSetIfChanged(ref text_, value);
  }

  public MainViewModel() : this(new NullStorageAdapter(), new NullFitService()) { }

  public MainViewModel(IStorageAdapter storage, IFitService fit)
  {
    storage_ = storage;
    fit_ = fit;
    this.Log().Debug($"{nameof(MainViewModel)}.ctor");
    Services.Log.Info($"{nameof(MainViewModel)} ready");
  }

  private async Task Show(string s)
  {
    Services.Log.Info(s);
    Log.Add(s);
    while (Log.Count > 25) RemoveHead();

    Dispatcher.UIThread.RunJobs();
    await Task.Yield();
  }

  private void RemoveHead() => Log.RemoveAt(0);
  private void RemoveTail() => Log.RemoveAt(Log.Count - 1);

  public void HandleSelectFileClicked()
  {
    Services.Log.Info("Select file clicked");

    //_ = Task.Run(async () =>
    Dispatcher.UIThread.Post(async () =>
    {
      try
      {
        Models.File? file = await storage_.OpenFileAsync();
        if (file == null)
        {
          Services.Log.Info("Could not load file");
          return;
        }
        Services.Log.Info($"Got file {file.Name} ({file.Bytes.Length} bytes)");
        lastFile_ = file;

        // Handle FIT files
        string extension = Path.GetExtension(file.Name);

        if (extension.ToLower() != ".fit")
        {
          return;
        }

        await Show($"Reading FIT file {file.Name}");
        using var ms = new MemoryStream(lastFile_.Bytes);
        using var ps = new ProgressStream(ms, 5 * 1024); // report progress every 5 kB
        await Show($"Read progress: ");

        ps.ReadProgressChanged += async (long position, long length) =>
        {
          RemoveTail();
          await Show($"Read progress: {(double)position / length * 100:##.##}% ({position}/{length})");
        };

        // Blocks the UI thread
        FitFile fit = new Reader().Read(file.Name, ps);

        // Blocks the UI thread
        //FitFile fit = await Task.Run(() => new Reader().Read(file.Name, ps));

        // Blocks the UI thread but sometimes RunJobs runs other posted UI work.
        //FitFile fit = await Dispatcher.UIThread.InvokeAsync(() => new Reader().Read(file.Name, ps));

        // Maybe this could be the ticket
        //Dispatcher.UIThread.Post(async () =>
        //{
        //  Show($"Reading FIT file {file.Name}");
        //  using var ms = new MemoryStream(lastFile_.Bytes);
        //  using var ps = new ProgressStream(ms, 5*1024); // report progress every 5 kB
        //  Show($"Read progress: ");
        //  Dispatcher.UIThread.RunJobs();
        //  await Task.Yield();

        //  ps.ReadProgressChanged += async (long position, long length) =>
        //  {
        //    string progress = $"{(double)position / length * 100:##.##}%";
        //    Console.WriteLine(progress);
        //    Log.Add(progress);
        //    Dispatcher.UIThread.RunJobs();
        //    await Task.Yield();
        //  };

        //  FitFile fit = new Reader().Read(file.Name, ps);
        //});

        RemoveTail();
        await Show($"Read progress: 100%");

        var sb = new StringBuilder();
        fit.Print(s => sb.AppendLine(s), showRecords: false);
        await Show(sb.ToString());

        var speeds = new List<Speed>
        {
          new() { Value = 6.7, Unit = Model.Units.SpeedUnit.MiPerHour },
          new() { Value = 9, Unit = Model.Units.SpeedUnit.MiPerHour },
          new() { Value = 5, Unit = Model.Units.SpeedUnit.MiPerHour },
          new() { Value = 9, Unit = Model.Units.SpeedUnit.MiPerHour },
          new() { Value = 5, Unit = Model.Units.SpeedUnit.MiPerHour },
          new() { Value = 6.7, Unit = Model.Units.SpeedUnit.MiPerHour },
        };

        await Show("Applying new lap speeds");
        fit.ApplySpeeds(speeds);
        await Show("Backfilling events");
        await Show("Backfill: ");
        await Task.Run(() =>
        {
          fit.BackfillEvents(100, async (i, total) =>
          {
            RemoveTail();
            await Show($"Backfill: {(double)i / total * 100:##.##}% ({i}/{total})");
          });
        });
        RemoveTail();
        await Show("Backfill: 100%");

        sb = new StringBuilder();
        fit.Print(s => sb.AppendLine(s), showRecords: false);
        await Show(sb.ToString());
      }
      catch (Exception e)
      {
        Services.Log.Info($"{e}");
      }
    });
  }

  public void HandleDownloadFileClicked()
  {
    Services.Log.Info("Download file clicked...");

    _ = Task.Run(async () =>
    {
      try
      {
        if (lastFile_ == null)
        {
          await Show("Cannot download file; none has been uploaded");
          return;
        }

        string name = Path.GetFileNameWithoutExtension(lastFile_.Name);
        string extension = Path.GetExtension(lastFile_.Name);
        await storage_.SaveAsync(new Models.File($"{name}_edit.{extension}", lastFile_.Bytes));
      }
      catch (Exception e)
      {
        Services.Log.Info($"{e}");
      }
    });
  }
}
