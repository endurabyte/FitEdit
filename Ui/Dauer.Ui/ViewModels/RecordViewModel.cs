using System.Collections.ObjectModel;
using Dauer.Data.Fit;
using Dauer.Ui.Models;
using ReactiveUI;

namespace Dauer.Ui.ViewModels;

public interface IRecordViewModel
{
  int SelectedIndex { get; set; }

  void Show(FitFile fit);
}

public class DesignRecordViewModel : RecordViewModel
{
  public DesignRecordViewModel()
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

  private int selectedIndex_;
  public int SelectedIndex { get => selectedIndex_; set => this.RaiseAndSetIfChanged(ref selectedIndex_, value); }

  public void Show(FitFile fit)
  {
    Records.Clear();

    int i = 0;
    foreach (var record in fit.Records)
    {
      Records.Add(new Record
      {
        Index = i++,
        MessageNum = record.Num,
        Name = record.Name,
      }); ;
    }
  }
}
