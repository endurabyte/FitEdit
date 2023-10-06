#nullable enable
using Dauer.Data.Fit;
using Dauer.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Data;

public class UiFile : ReactiveObject
{
  [Reactive] public FitFile? FitFile { get; set; }
  [Reactive] public LocalActivity? Activity { get; set; }
  [Reactive] public bool IsVisible { get; set; }
  [Reactive] public double Progress { get; set; }

  /// <summary>
  /// The index of the currently shown GPS coordinate shown in the chart, map, and records tab.
  /// </summary>
  [Reactive] public int SelectedIndex { get; set; }
  [Reactive] public int SelectionCount { get; set; }
}

