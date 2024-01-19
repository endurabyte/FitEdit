using Avalonia.Controls;
using ReactiveUI;

namespace FitEdit.Ui.ViewModels;

public class ColumnWrapper : ReactiveObject
{
  public string? Name { get; set; }
  public bool IsNamed => !Name?.StartsWith("Field ") ?? false;
  public bool IsUsed { get; set; }
  /// <summary>
  /// ComboBox entries. If null or empty, a DataGridTextColumn is used.
  /// </summary>
  public HashSet<object?>? NamedValues { get; set; }
  public DataGridColumn? Column { get; set; }
}
