using Typin.Console;
using Typin.Exceptions;

namespace Dauer.Cli;

public class ExceptionHandler : ICliExceptionHandler
{
  private readonly IConsole _console;

  public ExceptionHandler(IConsole console)
  {
    _console = console;
  }

  public bool HandleException(Exception ex)
  {
    _console.Error.WithForegroundColor(ConsoleColor.Red, (error) => error.WriteLine(ex));
    _console.Error.WriteLine();
    return true;
  }
}
