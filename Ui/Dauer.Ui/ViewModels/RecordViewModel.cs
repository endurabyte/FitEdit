using System.Collections.ObjectModel;
using Dauer.Data.Fit;
using Dauer.Ui.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IRecordViewModel
{
}

public class DesignRecordViewModel : RecordViewModel
{
  public DesignRecordViewModel() : base(new FileService())
  {
    Records.Add(new Record { MessageNum = 2 });
    Records.Add(new Record { MessageNum = 1 });
    Records.Add(new Record { MessageNum = 4 });
    Records.Add(new Record { MessageNum = 3 });
  }
}

public class RecordViewModel : ViewModelBase, IRecordViewModel
{
  public ObservableCollection<Record> Records { get; set; } = new();

  [Reactive] public int SelectedIndex { get; set; }

  private readonly IFileService fileService_;

  public RecordViewModel(
    IFileService fileService
  )
  {
    fileService_ = fileService;

    fileService.ObservableForProperty(x => x.FitFile).Subscribe(property =>
    {
      if (property.Value == null) { return; }
      Show(property.Value);
    });

    fileService.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      SelectedIndex = property.Value;
    });

    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      fileService_.SelectedIndex = property.Value;
    });
  }

  public void Show(FitFile fit)
  {
    Records.Clear();

    if (!fit.Records.Any()) { return; }

    int i = 0;
    DateTime start = fit.Records[0].Start();
    foreach (var record in fit.Records)
    {
      double elapsedSeconds = (record.Start() - start).TotalSeconds;
      double speed = record.GetEnhancedSpeed() ?? 0;
      double dist = record.GetDistance() ?? 0;
      double hr = record.GetHeartRate() ?? 0;

      Records.Add(new Record
      {
        Index = i++,
        MessageNum = record.Num,
        Name = record.Name,
        Detail = $"{elapsedSeconds} {speed} {dist} {hr}",
      }); ;
    }
  }
}
