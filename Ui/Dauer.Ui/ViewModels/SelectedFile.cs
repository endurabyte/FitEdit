using Dauer.Data.Fit;
using Dauer.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public class SelectedFile : ReactiveObject
{
  [Reactive] public FitFile? FitFile { get; set; }
  [Reactive] public bool IsVisible { get; set; }
  [Reactive] public BlobFile? Blob { get; set; }
  [Reactive] public double Progress { get; set; }
  [Reactive] public int SelectedIndex { get; set; }
  [Reactive] public int SelectionCount { get; set; }
}

