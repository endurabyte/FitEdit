using Dauer.Services;
using Typin;
using Typin.Attributes;
using Typin.Console;
namespace Dauer.Cli.Commands;

[Command("show", Manual = "Show file contents")]
public class ShowCommand : ICommand
{
  private readonly IFitService service_;

  [CommandParameter(0, Name = "source", Description = "Source .fit file")]
  public string Source { get; set; }

  [CommandOption("verbose", 'v', IsRequired = false, Description = "Show verbose output")]
  public bool Verbose { get; set; }

  public ShowCommand(IFitService service)
  {
    service_ = service;
  }

  public async ValueTask ExecuteAsync(IConsole console)
  {
    await service_.PrintAsync(Source, Verbose);
  }
}
