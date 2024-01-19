using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using FitEdit.Ui.ViewModels;

namespace FitEdit.Ui.Views;

public partial class PlotView : UserControl
{
  private IPlotViewModel? vm_;
  private bool isSelecting_;
  private double selectionMinX_;
  private double selectionMaxX_;

  public PlotView()
  {
    InitializeComponent();

    DataContextChanged += HandleDataContextChanged;
    //OxyPlotView.PointerWheelChanged += HandleWheel;
    OxyPlotView.PointerPressed += HandlePointerPressed;
    OxyPlotView.PointerMoved += HandlePointerMoved;
    OxyPlotView.PointerReleased += HandlePointerReleased;
  }

  private void HandleDataContextChanged(object? sender, EventArgs e)
  {
    if (DataContext is not IPlotViewModel vm)
    {
      return;
    }

    vm_ = vm;
  }

  private void HandlePointerPressed(object? sender, PointerPressedEventArgs e)
  {
    if (vm_ == null) { return; }
    isSelecting_ = true;

    OxyPlot.DataPoint p = GetDataPoint(e);
    selectionMinX_ = p.X;
    selectionMaxX_ = p.X;

    vm_.SelectCoordinates(selectionMinX_, selectionMaxX_);
  }

  private void HandlePointerMoved(object? sender, PointerEventArgs e)
  {
    if (vm_ == null) { return; }
    if (!isSelecting_) { return; }

    OxyPlot.DataPoint p = GetDataPoint(e);
    selectionMaxX_ = p.X;
    vm_.SelectCoordinates(selectionMinX_, selectionMaxX_);
  }

  private void HandlePointerReleased(object? sender, PointerReleasedEventArgs e)
  {
    if (vm_ == null) { return; }
    isSelecting_ = false;
    selectionMinX_ = 0;
    selectionMaxX_ = 0;
    vm_.SelectCoordinates(selectionMinX_, selectionMaxX_);
  }

  private OxyPlot.DataPoint GetDataPoint(PointerEventArgs e)
  {
    Point position = e.GetPosition(null);
    var xAxis = OxyPlotView.Model.Axes[0];
    var yAxis = OxyPlotView.Model.Axes[1];
    return xAxis.InverseTransform(position.X, position.Y, yAxis);
  }

  private void HandleWheel(object? sender, PointerWheelEventArgs e) => vm_?.HandleWheel(e.Delta.Y);
}