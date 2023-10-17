using System.Reactive.Linq;
using DynamicData.Binding;
using System.Collections.Specialized;
using Dauer.Data;
using ReactiveUI;

namespace Dauer.Ui.Extensions;

public static class FileServiceExtensions
{
  public static IDisposable SubscribeAdds(this IFileService fs, Action<UiFile> handle) =>
    fs.Files.ObserveCollectionChanges().Subscribe(x =>
     {
       if (x.EventArgs.Action != NotifyCollectionChangedAction.Add) { return; }
       if (x?.EventArgs?.NewItems == null) { return; }

       foreach (var file in x.EventArgs.NewItems.OfType<UiFile>())
       {
         handle(file);
         file.ObservableForProperty(x => x.FitFile).Subscribe(property => handle(property.Sender));
       }
     });

  public static IDisposable SubscribeRemoves(this IFileService fs, Action<UiFile> handle) =>
    fs.Files.ObserveCollectionChanges().Subscribe(x =>
     {
       if (x.EventArgs.Action != NotifyCollectionChangedAction.Remove) { return; }
       if (x?.EventArgs?.OldItems == null) { return; }

       foreach (var file in x.EventArgs.OldItems.OfType<UiFile>())
       {
         handle(file);
       }
     });
}