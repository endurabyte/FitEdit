using Avalonia.Controls;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public class DataGridWrapper : ReactiveObject
{
  public int Num { get; set; }
  public string? Name { get; set; }
  public bool IsNamed { get; set; }
  public List<ColumnWrapper> Headers { get; set; } = new();

  [Reactive] public bool IsVisible { get; set; }
  [Reactive] public bool IsExpanded { get; set; }
  [Reactive] public DataGrid? DataGrid { get; set; }
}
