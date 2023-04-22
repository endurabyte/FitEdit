using Dauer.Data.Fit;
using Dauer.Model;
using Dauer.Model.Workouts;

namespace Dauer.Services
{
  public interface IFitService
  {
    /// <summary>
    /// Return a one-line description of a fit file
    /// </summary>
    Task<string> OneLineAsync(string source);

    /// <summary>
    /// Pretty-print useful information from a fit file: Session, Laps, and Records.
    /// Optionally show details about each record.
    /// </summary>
    Task PrintAsync(string source, bool showRecords);

    /// <summary>
    /// Pretty-print everything in the given FIT file.
    /// </summary>
    Task PrintAllAsync(string source);

    Task PrintAllAsync(string source, Stream s);

    /// <summary>
    /// Duplicate the given FIT file by reading and writing each message.
    /// </summary>
    Task CopyAsync(string sourceFile, string destFile);

    /// <summary>
    /// Recalculate the workout as if each lap was run at the corresponding constant speed.
    /// </summary>
    Task SetLapSpeedsAsync(string sourceFile, string destFile, List<Speed> speeds);
  }

  public class NullFitService : IFitService
  {
    public Task CopyAsync(string sourceFile, string destFile) => Task.CompletedTask;
    public Task<string> OneLineAsync(string source) => Task.FromResult("");
    public Task PrintAsync(string source, bool showRecords) => Task.CompletedTask;
    public Task PrintAllAsync(string source) => Task.CompletedTask;
    public Task PrintAllAsync(string source, Stream s) => Task.CompletedTask;
    public Task SetLapSpeedsAsync(string sourceFile, string destFile, List<Speed> speeds) => Task.CompletedTask;
  }

  public class FitService : IFitService
  {
    public async Task<string> OneLineAsync(string source) => (await new Reader().ReadAsync(source)).OneLine();

    public async Task PrintAsync(string source, bool showRecords)
    {
      FitFile fitFile = await new Reader().ReadAsync(source);
      fitFile?.Print(Log.Info, showRecords);
    }

    public async Task PrintAllAsync(string source)
    {
      FitFile fitFile = await new Reader().ReadAsync(source);
      Log.Info(fitFile.PrintAll());
    }

    public async Task PrintAllAsync(string source, Stream s)
    {
      FitFile fitFile = await new Reader().ReadAsync(source, s);
      Log.Info(fitFile.PrintAll());
    }

    public async Task CopyAsync(string sourceFile, string destFile)
    {
      FitFile fitFile = await new Reader()
        .ReadAsync(sourceFile);

      new Writer().Write(fitFile, destFile);
    }

    public async Task SetLapSpeedsAsync(string sourceFile, string destFile, List<Speed> speeds)
    {
      FitFile fitFile = (await new Reader().ReadAsync(sourceFile))
       ?.ApplySpeeds(speeds)
       ?.BackfillEvents()
       ?.Print(Log.Info, false);

      new Writer().Write(fitFile, destFile);
    }
  }
}
