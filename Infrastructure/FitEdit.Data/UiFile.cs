#nullable enable
using FitEdit.Data.Fit;
using FitEdit.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Data;

public class UiFile : ReactiveObject
{
  [Reactive] public FitFile? FitFile { get; set; }
  [Reactive] public LocalActivity? Activity { get; set; }
  [Reactive] public bool IsLoaded { get; set; }
  public bool IsLoading => Progress > 0;
  [Reactive] public double Progress { get; set; }

  /// <summary>
  /// The index of the currently shown GPS coordinate shown in the chart, map, and records tab.
  /// </summary>
  [Reactive] public int SelectedIndex { get; set; }
  [Reactive] public int SelectionCount { get; set; }

  public UiFile()
  {
    this.ObservableForProperty(x => x.Progress)
      .Subscribe(_ => this.RaisePropertyChanged(nameof(IsLoading)));
  }
}

