﻿using System.Collections.ObjectModel;
using Dauer.Data.Fit;
using Dauer.Ui.Models;
using ReactiveUI.Fody.Helpers;

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

  [Reactive] public int SelectedIndex { get; set; }

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