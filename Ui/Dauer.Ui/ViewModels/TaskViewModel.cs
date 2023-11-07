using System.Collections.Specialized;
using Dauer.Model;
using Dauer.Ui.Infra;
using DynamicData.Binding;
using ReactiveUI;

namespace Dauer.Ui.ViewModels;

public class DesignTaskViewModel : TaskViewModel
{
  public DesignTaskViewModel() : base(
    new NullTaskService()
  )
  {
  }
}

public class TaskViewModel : ViewModelBase
{
  public ITaskService TaskService { get; }

  public TaskViewModel(
    ITaskService taskService
  )
  {
    TaskService = taskService;
    taskService.Tasks.ObserveCollectionChanges().Subscribe(x =>
    {
      IsVisible = taskService.Tasks.Count > 0;

      if (x.EventArgs.Action != NotifyCollectionChangedAction.Add) { return; }
      if (x?.EventArgs?.NewItems == null) { return; }

      foreach (UserTask task in x.EventArgs.NewItems.OfType<UserTask>())
      {
        task.ObservableForProperty(x => x.IsCanceled).Subscribe(async _ => 
        {
          await Task.Delay(5_000);
          Remove(task);
        });
      }
    });
  }

  public void HandleDismissClicked(UserTask task) => Remove(task);

  private void Remove(UserTask task) => TaskService.Remove(task);
}
