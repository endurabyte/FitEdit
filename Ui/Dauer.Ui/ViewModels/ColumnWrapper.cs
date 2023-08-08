using Avalonia.Controls;
using ReactiveUI;

namespace Dauer.Ui.ViewModels;

public class ColumnWrapper : ReactiveObject
{
  public string? Name { get; set; }
  public bool IsNamed => !Name?.StartsWith("Field ") ?? false;
  public bool IsUsed { get; set; }
  /// <summary>
  /// ComboBox entries
  /// </summary>
  public HashSet<object?>? NamedValues { get; set; }
  public DataGridColumn? Column { get; set; }
}
