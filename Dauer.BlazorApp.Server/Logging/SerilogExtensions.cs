using System;
using Npgsql.Logging;
using Serilog.Events;

namespace Dauer.BlazorApp.Server.Logging
{
    public static class SerilogExtensions
    {
        public static Action<string> ToSerilogLogCall(this NpgsqlLogLevel level)
        {
            switch (level)
            {
                case NpgsqlLogLevel.Trace:
                    return Serilog.Log.Logger.Verbose;
                case NpgsqlLogLevel.Debug:
                    return Serilog.Log.Logger.Debug;
                case NpgsqlLogLevel.Info:
                    return Serilog.Log.Logger.Information;
                case NpgsqlLogLevel.Warn:
                    return Serilog.Log.Logger.Warning;
                case NpgsqlLogLevel.Error:
                    return Serilog.Log.Logger.Error;
                case NpgsqlLogLevel.Fatal:
                    return Serilog.Log.Logger.Fatal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
        
        public static LogEventLevel ToSerilogLogEventLevel(this NpgsqlLogLevel level)
        {
            switch (level)
            {
                case NpgsqlLogLevel.Trace:
                    return LogEventLevel.Verbose;
                case NpgsqlLogLevel.Debug:
                    return LogEventLevel.Debug;
                case NpgsqlLogLevel.Info:
                    return LogEventLevel.Information;
                case NpgsqlLogLevel.Warn:
                    return LogEventLevel.Warning;
                case NpgsqlLogLevel.Error:
                    return LogEventLevel.Error;
                case NpgsqlLogLevel.Fatal:
                    return LogEventLevel.Fatal;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }
    }
}