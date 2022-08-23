using Dauer.Adapters.Selenium;
using Dauer.Model;
using FundLog.Model.Extensions;
using OpenQA.Selenium;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("delete-garmin", Manual = "Delete a Garmin Activity")]
public class GarminDeleteCommand : ICommand
{
  private readonly GarminDeleteStep delete_;

  [CommandParameter(0, Description = "Activity ID e.g. 9134211575")]
  public string ActivityId { get; set; }

  public GarminDeleteCommand(GarminDeleteStep delete)
  {
    delete_ = delete;
  }

  public async ValueTask ExecuteAsync(IConsole console)
  {
    delete_.ActivityId = ActivityId;

    try
    {
      if (!await delete_.Run().AnyContext())
      {
        Log.Error("Failed");
      }
    }
    finally
    {
      delete_.Close();
    }
  }
}
