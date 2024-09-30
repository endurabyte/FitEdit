using FitEdit.Ui.Infra;
using DynamicData.Binding;

namespace FitEdit.Ui.ViewModels;

public class DesignNotifyViewModel : NotifyViewModel
{
  public DesignNotifyViewModel() : base(
    new DesignNotifyService()
  )
  {
  }
}

public class NotifyViewModel : ViewModelBase
{
  public INotifyService Notifier { get; }

  public NotifyViewModel(
    INotifyService notifier
  )
  {
    Notifier = notifier;
    notifier.Bubbles.ObserveCollectionChanges().Subscribe(x =>
    {
      IsVisible = notifier.Bubbles.Count > 0;
    });
  }
}
