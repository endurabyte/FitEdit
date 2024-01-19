using FitEdit.Ui.Infra;
using DynamicData.Binding;

namespace FitEdit.Ui.ViewModels;

public class DesignTaskViewModel : TaskViewModel
{
  public DesignTaskViewModel() : base(
    new DesignTaskService()
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
    });
  }
}
