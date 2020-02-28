using System;
using Npgsql.Logging;

namespace Dauer.BlazorApp.Server.Logging
{
    public class SerilogNpgsqlLogger : NpgsqlLogger
    {
        private readonly string _name;
        
        public SerilogNpgsqlLogger(string name)
        {
            _name = name;
        }
        public override bool IsEnabled(NpgsqlLogLevel level)
        {
            return Serilog.Log.Logger.IsEnabled(level.ToSerilogLogEventLevel());
        }

        public override void Log(NpgsqlLogLevel level, int connectorId, string msg, Exception exception = null)
        {
            var log = level.ToSerilogLogCall();

            var prefix = $"{_name}: {connectorId}: ";
            
            log($"{prefix}{msg}");
            
            if (exception != null)
                log($"{prefix}{exception}");
        }
    }
}