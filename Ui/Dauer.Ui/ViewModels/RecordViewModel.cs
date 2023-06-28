using System.Collections.ObjectModel;
using Dauer.Data.Fit;
using Dauer.Ui.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public interface IRecordViewModel
{
  int SelectedIndex { get; set; }
  int SelectionCount { get; set; }
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
  [Reactive] public int SelectionCount { get; set; }

  private readonly IFileService fileService_;
  private IDisposable? selectedIndexSub_;
  private IDisposable? selectedCountSub_;

  public RecordViewModel(
    IFileService fileService
  )
  {
    fileService_ = fileService;

    fileService.ObservableForProperty(x => x.MainFile).Subscribe(property => HandleMainFileChanged(fileService.MainFile));

    this.ObservableForProperty(x => x.SelectedIndex).Subscribe(property =>
    {
      if (fileService_.MainFile == null) { return; }
      fileService_.MainFile.SelectedIndex = property.Value;
    });
  }

  private void HandleMainFileChanged(SelectedFile? file)
  {
    if (file == null) { return; }
    if (file.FitFile == null) { return; }
    Show(file.FitFile);

    selectedIndexSub_?.Dispose();
    selectedCountSub_?.Dispose();

    selectedIndexSub_ = file.ObservableForProperty(x => x.SelectedIndex).Subscribe(e => HandleSelectedIndexChanged(e.Value));
    selectedCountSub_ = file.ObservableForProperty(x => x.SelectionCount).Subscribe(e => HandleSelectionCountChanged(e.Value));
  }

  private void HandleSelectedIndexChanged(int index)
  {
    if (SelectedIndex == index) { return; }
    SelectedIndex = index;

    // Lazy-load more records.
    if (SelectedIndex > Records.Count && fileService_.MainFile?.FitFile != null)
    {
      FillUpTo(fileService_.MainFile.FitFile, SelectedIndex + 100);
    }
  }

  private void HandleSelectionCountChanged(int count)
  {
    SelectionCount = count;
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
