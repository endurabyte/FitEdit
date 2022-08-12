using Dauer.Services;
using Typin;
using Typin.Attributes;
using Typin.Console;
namespace Dauer.Cli.Commands;

[Command("copy", Manual = "Copy files")]
public class CopyCommand : ICommand
{
  private readonly IFitService service_;

  [CommandParameter(0, Name = "source", Description = "Source .fit file")]
  public string Source { get; set; }

  [CommandParameter(1, Name = "destination", Description = "Destination .fit file")]
  public string Destination { get; set; }

  public CopyCommand(IFitService service)
  {
    service_ = service;
  }

  public ValueTask ExecuteAsync(IConsole console)
  {
    service_.Copy(Source, Destination);
    return ValueTask.CompletedTask;
  }
}
