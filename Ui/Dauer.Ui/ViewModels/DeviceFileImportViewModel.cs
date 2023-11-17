using System.Collections.ObjectModel;
using Dauer.Model;
using ReactiveUI.Fody.Helpers;

namespace Dauer.Ui.ViewModels;

public class DesignDeviceFileImportViewModel : DeviceFileImportViewModel
{
  public DesignDeviceFileImportViewModel()
  {
    Activities.Add(new LocalActivity
    {
      Name = "Fake activity",
    });

    Activities.Add(new LocalActivity
    {
      Name = "Another Fake activity",
    });
  }
}

public class DeviceFileImportViewModel : ViewModelBase
{
  [Reactive] public ObservableCollection<LocalActivity> Activities { get; set; } = new();
  [Reactive] public ObservableCollection<LocalActivity> SelectedActivities { get; set; } = new();

  public void HandleActivityFound(LocalActivity activity, UserTask ut)
  {
    Activities.Add(activity);

    ut.Name = Activities.Count switch
    {
      1 => "Found 1 activity",
      _ => $"Found {Activities.Count} activities",
    };
  }
}
