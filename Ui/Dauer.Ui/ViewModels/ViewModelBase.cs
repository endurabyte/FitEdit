using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public class ViewModelBase : ReactiveObject
{
  [Reactive] public bool IsVisible { get; set; }
}
