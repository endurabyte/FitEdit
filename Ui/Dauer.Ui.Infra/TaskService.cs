using System.Collections.ObjectModel;
using Dauer.Model;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.Infra;

public interface ITaskService
{
  ObservableCollection<UserTask> Tasks { get; }

  void Add(UserTask task);
  void Remove(UserTask task);
}

public class NullTaskService : ITaskService
{
  [Reactive] public ObservableCollection<UserTask> Tasks { get; set; } = new();

  public NullTaskService()
  {
    Tasks.Add(new UserTask
    {
      Name = "G-diffuser startup",
      Status = "Calibrating the G-diffuser...",
      Progress = 67,
    });
  }

  public void Add(UserTask task) => Tasks.Add(task);
  public void Remove(UserTask task) => Tasks.Remove(task);
}

public class TaskService : ITaskService
{
  [Reactive] public ObservableCollection<UserTask> Tasks { get; set; } = new();

  public void Add(UserTask task) => Tasks.Add(task);
  public void Remove(UserTask task) => Tasks.Remove(task);
}

