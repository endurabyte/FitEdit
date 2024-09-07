using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FitEdit.Model;

public class DebugLogger : ILogger
{
  public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
  public bool IsEnabled(LogLevel logLevel) => true;

  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
    Func<TState, Exception?, string> formatter)
      => Debug.WriteLine(formatter(state, exception));
}