using System.Collections.ObjectModel;
using Avalonia.Threading;
using FitEdit.Model;
using FitEdit.Model.Services;
using FitEdit.Model.Storage;
using ReactiveUI;
using ReactiveUI.Fody.Helpers;

namespace FitEdit.Ui.ViewModels;

public class DesignDeviceFileImportViewModel : DeviceFileImportViewModel
{
  public DesignDeviceFileImportViewModel(
    IMtpAdapter mtp,
    IEventService events,
    PortableDevice dev,
    UserTask ut) 
    : base (mtp, events, dev, ut)
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
  private readonly IMtpAdapter mtp_;
  private readonly IEventService events_;
  private readonly PortableDevice dev_;
  private readonly UserTask ut_;

  private TimeSpan howFarBack_ = new(7, 0, 0, 0);
  public TimeSpan HowFarBack
  {
    get
    {
      TimeSpan time = howFarBack_;
      howFarBack_ = howFarBack_ switch
      {
        _ when howFarBack_.TotalDays < 30 => new TimeSpan(30, 0, 0, 0),
        _ when howFarBack_.TotalDays < 60 => new TimeSpan(60, 0, 0, 0),
        _ when howFarBack_.TotalDays < 120 => new TimeSpan(120, 0, 0, 0),
        _ when howFarBack_.TotalDays < 365 => new TimeSpan(365, 0, 0, 0),
        _ when howFarBack_.TotalDays < 365 * 5 => new TimeSpan(365 * 5, 0, 0, 0),
        _ when howFarBack_.TotalDays < 365 * 10 => new TimeSpan(365 * 10, 0, 0, 0),
        _ => new TimeSpan(365 * 100, 0, 0, 0),
      };
      return time;
    }
  }

  [Reactive] public ObservableCollection<LocalActivity> Activities { get; set; } = new();
  [Reactive] public ObservableCollection<LocalActivity> SelectedActivities { get; set; } = new();
  public bool HasSelection => SelectedActivities.Count > 0;
  [Reactive] public string Message { get; set; } = string.Empty;
  [Reactive] public bool ImportComplete { get; set; }

  public DeviceFileImportViewModel(IMtpAdapter mtp, IEventService events, PortableDevice dev, UserTask ut)
  {
    mtp_ = mtp;
    events_ = events;
    dev_ = dev;
    ut_ = ut;

    SelectedActivities.CollectionChanged += (s, e) => this.RaisePropertyChanged(nameof(HasSelection));
  }

  public void HandleActivityFound(LocalActivity activity)
  {
    Activities.Add(activity);

    ut_.Status = Activities.Count switch
    {
      0 => "No activities found",
      1 => "Found 1 activity",
      _ => $"Found {Activities.Count} activities",
    };
  }

  public void SearchForFiles()
  {
    TimeSpan howFarBack = HowFarBack;
    Message = howFarBack switch
    {
      _ when howFarBack.TotalDays > 365 => $"Showing last {howFarBack.TotalDays/365:#.#} years",
      _ when howFarBack.TotalDays > 364 => $"Showing the last year",
      _ => $"Showing last {howFarBack.TotalDays:#} days",
    };

    Dispatcher.UIThread.Invoke(Activities.Clear);

    _ = Task.Run(() =>
    {
      using IDisposable sub = events_.Subscribe<LocalActivity>(EventKey.MtpActivityFound, HandleActivityFound);
      mtp_.GetFiles(dev_, howFarBack);
    });
  }

  public void ImportFiles()
  {
    ut_.Status = "Importing...";
    ut_.NextAction?.Invoke();
    ImportComplete = true;
  }

  public void Dismiss() => ut_.Dismiss();
}
