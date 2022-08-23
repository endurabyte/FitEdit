using Dauer.Adapters.Selenium;
using Dauer.Model;
using FundLog.Model.Extensions;
using OpenQA.Selenium;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("edit-finalsurge", Manual = "Edit a Final Surge Workout")]
public class FinalSurgeEditCommand : ICommand
{
  private readonly FinalSurgeEditStep edit_;

  [CommandOption("date", 't', Description = "Workout Date", IsRequired = true)]
  public DateTime Date { get; set; }

  [CommandOption("name", 'n', Description = "Workout Name", IsRequired = false)]
  public string Name { get; set; }

  [CommandOption("description", 'd', Description = "Workout Description", IsRequired = false)]
  public string Description { get; set; }

  public FinalSurgeEditCommand(FinalSurgeEditStep edit)
  {
    edit_ = edit;
  }

  public async ValueTask ExecuteAsync(IConsole console)
  {
    edit_.Date = Date;
    edit_.WorkoutName = Name;
    edit_.Description = Description;

    try
    {
      if (!await edit_.Run().AnyContext())
      {
        Log.Error("Failed");
      }
    }
    finally
    {
      edit_.Close();
    }
  }
}