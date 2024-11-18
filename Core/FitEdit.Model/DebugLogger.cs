using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace FitEdit.Model;

public class DebugLogger : ILogger
{
  public static DebugLogger Instance { get; } = new();

  public LogLevel LogLevel { get; set; } = LogLevel.Information;
  
  public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
  public bool IsEnabled(LogLevel logLevel) => logLevel >= LogLevel;

  public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
    Func<TState, Exception?, string> formatter)
  {
    if (!IsEnabled(logLevel)) { return; }
    Debug.WriteLine(formatter(state, exception));
  }
}