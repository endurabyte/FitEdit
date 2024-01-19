using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Model;

public class UserTask : ReactiveObject
{
  [Reactive] public string Header { get; set; }
  [Reactive] public string Status { get; set; }
  [Reactive] public int Progress { get; set; }
  [Reactive] public bool IsComplete { get; set; }
  [Reactive] public bool IsCanceled { get; set; }
  [Reactive] public bool CanCancel { get; set; } = true;
  [Reactive] public bool IsDismissed { get; set; }
  [Reactive] public bool IsConfirmed { get; set; } = true;
  [Reactive] public object Content { get; set; }

  public Action NextAction { get; set; }

  public CancellationToken CancellationToken => cts_.Token;

  private readonly CancellationTokenSource cts_ = new();

  public UserTask()
  {
    this.ObservableForProperty(x => x.Status).Subscribe(_ =>
    {
      Log.Info($"Task '{Header}' status: '{Status}'");
    });
  }

  public void Cancel(bool labelAsCanceled = true)
  {
    cts_.Cancel();
    if (labelAsCanceled)
    {
      Header = $"(Canceled) {Header}";
      Status = $"(Canceled) {Status}";
    }
    IsCanceled = true;
    IsComplete = true;
  }

  public void Dismiss()
  {
    IsDismissed = true;
    Status = "Dismissed";
  }

  public void Confirm()
  {
    IsConfirmed = true;
    _ = Task.Run(() => NextAction?.Invoke());
  }
}


