using Dauer.Services;
using Typin;
using Typin.Attributes;
using Typin.Console;
namespace Dauer.Cli.Commands;

[Command("dump", Manual = "Show detailed file contents")]
public class DumpCommand : ICommand
{
  private readonly IFitService service_;

  [CommandParameter(0, Name = "source", Description = "Source .fit file")]
  public string Source { get; set; }

  public DumpCommand(IFitService service)
  {
    service_ = service;
  }

  public ValueTask ExecuteAsync(IConsole console)
  {
    service_.PrintAll(Source);
    return ValueTask.CompletedTask;
  }
}
