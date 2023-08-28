using System.Reactive.Linq;
using ReactiveUI;
using Dauer.Data;

namespace Dauer.Ui.Extensions;

public static class SelectedFileExtensions
{
  public static IDisposable SubscribeToFitFile(this UiFile file, Action<UiFile> handle) =>
    file.ObservableForProperty(x => x.FitFile).Subscribe(property => handle(property.Sender));

  public static IDisposable SubscribeToIsLoaded(this UiFile file, Action<UiFile> handle) =>
    file.ObservableForProperty(x => x.IsVisible).Subscribe(property => handle(property.Sender));
}
