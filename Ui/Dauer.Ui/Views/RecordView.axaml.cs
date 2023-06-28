using Avalonia.Collections;
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
    DataContextChanged += HandleDataContextChanged_;
    MainDataGrid.SelectionChanged += HandleSelectionChanged;
  }

  private void HandleDataContextChanged_(object? sender, EventArgs e)
  {
    if (DataContext is not IRecordViewModel vm) { return; };

    vm_ = vm;
    vm_.ObservableForProperty(x => x.SelectedIndex).Subscribe(property => MainDataGrid.SelectedIndex = property.Value);
    vm_.ObservableForProperty(x => x.SelectionCount).Subscribe(property =>
    {
      if (MainDataGrid.ItemsSource == null) { return; }
      MainDataGrid.SelectedItems.Clear();
      var items = new DataGridCollectionView(MainDataGrid.ItemsSource);
      foreach (int i in Enumerable.Range(vm_.SelectedIndex, vm_.SelectionCount))
      {
        MainDataGrid.SelectedItems.Add(items[i]);
      }
    });
  }

  private void HandleSelectionChanged(object? sender, SelectionChangedEventArgs e) => 
    MainDataGrid.ScrollIntoView(MainDataGrid.SelectedItem, MainDataGrid.CurrentColumn);
}