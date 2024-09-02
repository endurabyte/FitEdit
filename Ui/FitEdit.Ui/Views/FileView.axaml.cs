using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using FitEdit.Data;
using FitEdit.Model;
using FitEdit.Ui.ViewModels;

namespace FitEdit.Ui.Views;

public partial class FileView : UserControl
{
  public FileView()
  {
    InitializeComponent();

    AddHandler(DragDrop.DragEnterEvent, HandleDragEnter);
    AddHandler(DragDrop.DragLeaveEvent, HandleDragLeave);
    AddHandler(DragDrop.DropEvent, HandleDrop);
  }

  private void HandleScrollChanged(object? sender, ScrollChangedEventArgs _)
  {
    if (DataContext is not IFileViewModel vm) { return; }
    if (sender is not ScrollViewer sv) { return; }

    double maximumScroll = sv.Extent.Height - sv.Viewport.Height;
    if (maximumScroll <= 0) { return; }

    vm.ScrollPercent = sv.Offset.Y / maximumScroll * 100;
  }

  private void HandleDragEnter(object? sender, DragEventArgs e)
  {
    if (DataContext is not IFileViewModel vm) { return; }
    vm.IsDragActive = true;
  }

  private void HandleDragLeave(object? sender, DragEventArgs e)
  {
    if (DataContext is not IFileViewModel vm) { return; }
    vm.IsDragActive = false;
  }

  private void HandleDrop(object? sender, DragEventArgs e)
  {
    if (DataContext is not IFileViewModel vm) { return; }
    vm.IsDragActive = false;

    if (e.Data.GetFiles() is { } files)
    {
      foreach (IStorageItem item in files)
      {
        if (item is not IStorageFile file) { continue; }
        vm.HandleFileDropped(file);
      }
    }
  }

  private void HandleFileDoubleTapped(object? sender, TappedEventArgs e)
  {
    if (sender is not StyledElement elem) { return; }
    if (elem.DataContext is not UiFile uif) { return; }
    if (DataContext is not IFileViewModel vm) { return; }

    vm.LoadOrUnload(uif);
  }
}