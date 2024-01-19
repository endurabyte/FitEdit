using Avalonia.Controls;
using Avalonia.Controls.Templates;
using FitEdit.Ui.ViewModels;

namespace FitEdit.Ui;

public class ViewLocator : IDataTemplate
{
#nullable disable
  public Control Build(object data)
#nullable enable
  {
    if (data is null)
      return null;

    var name = data.GetType().FullName!.Replace("ViewModel", "View");
    var type = Type.GetType(name);

    if (type != null)
    {
      return (Control)Activator.CreateInstance(type)!;
    }

    return new TextBlock { Text = name };
  }

  public bool Match(object? data)
  {
    return data is ViewModelBase;
  }
}