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
      if (SelectedIndex == property.Value) { return; }
      SelectedIndex = property.Value;

      // Lazy-load more records.
      if (SelectedIndex > Records.Count && fileService_.FitFile != null)
      {
        FillUpTo(fileService_.FitFile, SelectedIndex + 100);
      }
    });

    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      fileService_.SelectedIndex = property.Value;
    });
  }

  private void Show(FitFile fit)
  {
    Records.Clear();

    if (!fit.Records.Any()) { return; }

    // Showing all records at once hangs the UI for a few seconds.
    // Show only the first few. We'll lazy-load more as needed.
    FillUpTo(fit, 100);
  }

  private void FillUpTo(FitFile fit, int endIdx)
  { 
    DateTime start = fit.Records[0].Start();

    int startIdx = Records.Count;
    endIdx = Math.Min(endIdx, fit.Records.Count);

    foreach (int i in Enumerable.Range(startIdx, endIdx - startIdx))
    {
      var record = fit.Records[i];

      double elapsedSeconds = (record.Start() - start).TotalSeconds;
      double speed = record.GetEnhancedSpeed() ?? 0;
      double dist = record.GetDistance() ?? 0;
      double hr = record.GetHeartRate() ?? 0;

      Records.Add(new Record
      {
        Index = i,
        MessageNum = record.Num,
        Name = record.Name,
        HR = $"{hr}",
        Speed = $"{speed:0.##}m/s",
        Distance = $"{dist:0.##}m",
      }); ;
    }
  }
}
