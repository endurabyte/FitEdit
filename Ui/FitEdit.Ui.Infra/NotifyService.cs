using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using FitEdit.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Ui.Infra;

public interface INotifyService
{
  ObservableCollection<NotifyBubble> Bubbles { get; }

  void Add(NotifyBubble bubble);
  void Remove(NotifyBubble bubble);

  NotifyBubble NotifyUser(string header, string? status = null, Action? next = null, bool autoCancel = false);
}

public class DesignNotifyService : NotifyService
{
  public DesignNotifyService()
  {
    Bubbles.Add(new NotifyBubble
    {
      Header = "A notification",
      Status = "This notification has a pretty long text section so that we can verify that long messages look OK on all platforms.\nWhat do you think?",
      Progress = 67,
    });

    var ut = new NotifyBubble
    {
      Header = "Another notification",
      Status = "Please click continue",
      IsConfirmed = false
    };
    ut.NextAction = () =>
    {
      ut.Status = "You clicked continue!";
      ut.IsComplete = true;
    };
    ut.Content = new Border 
    { 
      Width = 200,
      Height = 50,
      Background = new SolidColorBrush(Colors.OrangeRed), 
      CornerRadius = new Avalonia.CornerRadius(20),
      Child = new TextBlock
      {
        Text = "Test control",
        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
        VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
      }
    };

    Bubbles.Add(ut);
  }
}

public class NotifyService : INotifyService
{
  [Reactive] public ObservableCollection<NotifyBubble> Bubbles { get; set; } = new();
  [Reactive] public TimeSpan AutoDismissAfter { get; set; } = TimeSpan.FromSeconds(10);

  public void Add(NotifyBubble bubble)
  {
    Bubbles.Add(bubble);
    bubble.ObservableForProperty(x => x.IsCanceled).Subscribe(async _ =>
    {
      await Task.Delay(AutoDismissAfter);
      Remove(bubble);
    });

    bubble.ObservableForProperty(x => x.IsDismissed).Subscribe(_ => Remove(bubble));
  }

  public void Remove(NotifyBubble bubble) => Bubbles.Remove(bubble);

  public NotifyBubble NotifyUser(string header, string? status = null, Action? next = null, bool autoCancel = false)
  {
    Log.Info(header);

    var bubble = new NotifyBubble
    {
      Header = header,
      Status = status,
    };
    Add(bubble);

    if (autoCancel)
    {
      bubble.Cancel();
    }

    // If the notification has no action
    if (next != null)
    {
      // Prompt the user to confirm the action
      bubble.IsConfirmed = false;
      bubble.NextAction = () =>
      {
        // Dismiss the notification when the action starts
        bubble.Dismiss();
        next();
      };
    }

    return bubble;
  }
}

