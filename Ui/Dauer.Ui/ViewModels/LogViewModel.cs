using System.Collections.ObjectModel;
using Dauer.Ui.Extensions;

namespace Dauer.Ui.ViewModels;

public interface ILogViewModel
{
  Task Log(string s);
}

public class DesignLogViewModel : LogViewModel
{

}

public class LogViewModel : ViewModelBase, ILogViewModel
{
  public ObservableCollection<string> LogEntries { get; } = new();

  public async Task Log(string s)
  {
    Model.Log.Info(s);
    LogEntries.Add(s);
    while (LogEntries.Count > 25) RemoveHead();

    await TaskUtil.MaybeYield();
  }

  private void RemoveHead() => LogEntries.RemoveAt(0);
}