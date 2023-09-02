using Avalonia.Controls;
using Dauer.Ui.ViewModels;
using ReactiveUI;

namespace Dauer.Ui.Views;

public partial class FileView : UserControl
{
  public FileView()
  {
    InitializeComponent();
    FileListBox.ObservableForProperty(x => x.Scroll).Subscribe(_ =>
    {
      if (FileListBox.Scroll is not ScrollViewer sv) { return; }
      sv.ScrollChanged += HandleScrollChanged;
    });
  }

  private void HandleScrollChanged(object? sender, ScrollChangedEventArgs _)
  {
    if (DataContext is not IFileViewModel vm) { return; }
    if (sender is not ScrollViewer sv) { return; }

    double maximumScroll = sv.Extent.Height - sv.Viewport.Height;
    if (maximumScroll <= 0) { return; }

    vm.ScrollPercent = sv.Offset.Y / maximumScroll * 100;
  }
}