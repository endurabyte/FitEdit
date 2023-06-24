using Avalonia.Controls;
using Dauer.Ui.ViewModels;

namespace Dauer.Ui.Views;

public partial class PlotView : UserControl
{
  private IPlotViewModel? vm_;

  public PlotView()
  {
    InitializeComponent();

    DataContextChanged += HandleDataContextChanged;
    OxyPlotView.PointerWheelChanged += HandleWheel;
  }

  private void HandleDataContextChanged(object? sender, EventArgs e)
  {
    if (DataContext is not IPlotViewModel vm)
    {
      return;
    }

    vm_ = vm;
  }

  private void HandleWheel(object? sender, Avalonia.Input.PointerWheelEventArgs e) => vm_?.HandleWheel(e.Delta.Y);
}