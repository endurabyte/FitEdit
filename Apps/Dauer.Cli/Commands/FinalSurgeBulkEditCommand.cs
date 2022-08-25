using Dauer.Adapters.Selenium;
using Dauer.Model;
using Dauer.Model.Extensions;
using OpenQA.Selenium;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("bulk-edit-finalsurge", Manual = "Edit many Final Surge Workouts")]
public class FinalSurgeBulkEditCommand : ICommand
{
  private readonly FinalSurgeBulkEditStep edit_;

  [CommandOption("dates", 't', Description = "Workout Dates", IsRequired = true)]
  public List<DateTime> Dates { get; set; }

  [CommandOption("names", 'n', Description = "Workout Names", IsRequired = true)]
  public List<string> Names { get; set; }

  [CommandOption("descriptions", 'd', Description = "Workout Descriptions", IsRequired = true)]
  public List<string> Descriptions { get; set; }

  public FinalSurgeBulkEditCommand(FinalSurgeBulkEditStep edit)
  {
    edit_ = edit;
  }

  public async ValueTask ExecuteAsync(IConsole console)
  {
    edit_.Dates = Dates;
    edit_.WorkoutNames = Names;
    edit_.Descriptions = Descriptions;

    try
    {
      if (!await edit_.Run().AnyContext())
      {
        Log.Error($"{nameof(FinalSurgeBulkEditCommand)} Failed");
      }
    }
    finally
    {
      edit_.Close();
    }
  }
}