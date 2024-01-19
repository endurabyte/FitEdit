using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Ui.ViewModels;

public class ViewModelBase : ReactiveObject
{
  [Reactive] public bool IsVisible { get; set; }
}
