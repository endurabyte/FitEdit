using Avalonia.Controls;
using Dauer.Ui.ViewModels;
using ReactiveUI;

namespace Dauer.Ui.Views;

public partial class RecordView : UserControl
{
  private IRecordViewModel? vm_;

  public RecordView()
  {
    InitializeComponent();
    this.ObservableForProperty(x => x.DataContext).Subscribe(_ => HandleDataContextChanged());
  }

  private void HandleDataContextChanged()
  {
    if (DataContext is not IRecordViewModel vm) { return; }
    vm_ = vm;

    vm.ObservableForProperty(x => x.ShowHexData).Subscribe(_ => HandleHexSelectionChanged());
    vm.ObservableForProperty(x => x.HexDataSelectionStart).Subscribe(_ => HandleHexSelectionChanged());
  }

  private void HandleHexSelectionChanged()
  {
    if (vm_ == null) { return; }

    double perc = vm_.HexDataSelectionStart / (double)vm_.HexData.Length;
    double height = HexScrollViewer.Extent.Height;
    double newHeight = perc * height - HexScrollViewer.Viewport.Height / 2;
    HexScrollViewer.Offset = new(HexScrollViewer.Offset.X, newHeight);

  }
}