using Dauer.Adapters.Selenium;
using Dauer.Model;
using Dauer.Model.Extensions;
using OpenQA.Selenium;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("edit-garmin", Manual = "Edit a Garmin Activity")]
public class GarminEditCommand : ICommand
{
  private readonly GarminEditStep edit_;

  [CommandParameter(0, Description = "Activity ID e.g. 9134211575")]
  public string ActivityId { get; set; }

  [CommandOption("title", 't', Description = "Activity Title", IsRequired = false)]
  public string Title { get; set; }

  [CommandOption("note", 'n', Description = "Activity Note", IsRequired = false)]
  public string Note { get; set; }

  public GarminEditCommand(GarminEditStep edit)
  {
    edit_ = edit;
  }

  public async ValueTask ExecuteAsync(IConsole console)
  {
    edit_.ActivityId = ActivityId;
    edit_.Title = Title;
    edit_.Note = Note;

    try
    {
      if (!await edit_.Run().AnyContext())
      {
        Log.Error($"{nameof(GarminEditCommand)} Failed");
      }
    }
    finally
    {
      edit_.Close();
    }
  }
}
