using Avalonia.Controls;

namespace Dauer.Ui.Views;

public partial class RecordView : UserControl
{
  public RecordView()
  {
    InitializeComponent();
    MainDataGrid.SelectionChanged += HandleSelectionChanged;
  }

  private void HandleSelectionChanged(object? sender, SelectionChangedEventArgs e) => 
    MainDataGrid.ScrollIntoView(MainDataGrid.SelectedItem, MainDataGrid.CurrentColumn);
}