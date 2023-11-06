using System.Collections.ObjectModel;
using Dauer.Model;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.Infra;

public interface ITaskService
{
  ObservableCollection<UserTask> Tasks { get; } 
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
}

public class TaskService : ITaskService
{
  [Reactive] public ObservableCollection<UserTask> Tasks { get; set; } = new();
}

