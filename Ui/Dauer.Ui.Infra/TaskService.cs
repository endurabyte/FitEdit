using System.Collections.ObjectModel;
using Avalonia.Controls;
using Avalonia.Media;
using Dauer.Model;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.Infra;

public interface ITaskService
{
  ObservableCollection<UserTask> Tasks { get; }

  void Add(UserTask task);
  void Remove(UserTask task);
}

public class DesignTaskService : TaskService
{
  public DesignTaskService()
  {
    Tasks.Add(new UserTask
    {
      Header = "A notification",
      Status = "This notification has a pretty long text section so that we can verify that long messages look OK on all platforms.\nWhat do you think?",
      Progress = 67,
    });

    var ut = new UserTask
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

    Tasks.Add(ut);
  }
}

public class TaskService : ITaskService
{
  [Reactive] public ObservableCollection<UserTask> Tasks { get; set; } = new();
  [Reactive] public TimeSpan AutoDismissAfter { get; set; } = TimeSpan.FromSeconds(10);

  public void Add(UserTask task)
  {
    Tasks.Add(task);
    task.ObservableForProperty(x => x.IsCanceled).Subscribe(async _ =>
    {
      await Task.Delay(AutoDismissAfter);
      Remove(task);
    });

    task.ObservableForProperty(x => x.IsDismissed).Subscribe(_ => Remove(task));
  }

  public void Remove(UserTask task) => Tasks.Remove(task);
}

