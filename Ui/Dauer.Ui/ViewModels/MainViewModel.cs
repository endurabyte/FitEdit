using Splat;
using System.Collections.ObjectModel;
using Dauer.Ui.Models;
using Dauer.Ui.Services;
using Dauer.Ui.Adapters.Storage;

namespace Dauer.Ui.ViewModels;

public interface IMainViewModel
{

}

public class MainViewModel : ViewModelBase, IMainViewModel
{
  public ObservableCollection<InventoryItem> Items { get; }
  private Models.File? lastFile_ = null;
  private readonly IStorageAdapter storage_;

  public MainViewModel(IStorageAdapter storage, Database db)
  {
    storage_ = storage;

    Items = new ObservableCollection<InventoryItem>(db.GetItems());
    this.Log().Debug($"test log from {nameof(MainViewModel)}");
    Log.Write("MainViewModel ready");

    _ = Task.Run(async () =>
    {
      await Task.Delay(5000);
      Log.Write("hello from task");
    });
  }

  public void HandleSelectFileClicked()
  {
    Log.Write("Select file clicked");

    _ = Task.Run(async () =>
    {
      try
      {
        Models.File? file = await storage_.OpenFileAsync();
        if (file == null)
        {
          Log.Write("Could not load file");
          return;
        }
        Log.Write($"Got file {file.Name} ({file.Bytes.Length} bytes)");
        lastFile_ = file;
      }
      catch (Exception e)
      {
        Log.Write($"{e}");
      }
    });
  }

  public void HandleDownloadFileClicked()
  {
    Log.Write("Download file clicked...");

    _ = Task.Run(async () =>
    {
      try
      {
        if (lastFile_ == null)
        {
          Log.Write("Cannot download file; none has been uploaded");
          return;
        }

        string name = Path.GetFileNameWithoutExtension(lastFile_.Name);
        string extension = Path.GetExtension(lastFile_.Name);
        await storage_.SaveAsync(new Models.File($"{name}_edit.{extension}", lastFile_.Bytes));
      }
      catch (Exception e)
      {
        Log.Write($"{e}");
      }
    });
  }
}
