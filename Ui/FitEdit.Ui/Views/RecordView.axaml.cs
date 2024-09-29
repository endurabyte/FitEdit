using Avalonia.Controls;
using FitEdit.Ui.ViewModels;
using ReactiveUI;

namespace FitEdit.Ui.Views;

public partial class RecordView : UserControl
{
  private IRecordViewModel? vm_;

  public RecordView()
  {
    InitializeComponent();
    this.ObservableForProperty(x => x.DataContext)
      .Subscribe(_ => HandleDataContextChanged());
  }

  private void HandleDataContextChanged()
  {
    if (DataContext is not IRecordViewModel vm) { return; }
    vm_ = vm;
  }
}