using Npgsql.Logging;

namespace Dauer.BlazorApp.Server.Logging
{
    public class SerilogNpgsqlLoggingProvider : INpgsqlLoggingProvider
    {
        public NpgsqlLogger CreateLogger(string name)
        {
            return new SerilogNpgsqlLogger(name);
        }
    }
}