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
    string OneLine(string source);

    /// <summary>
    /// Pretty-print useful information from a fit file: Session, Laps, and Records.
    /// Optionally show details about each record.
    /// </summary>
    void Print(string source, bool showRecords);

    /// <summary>
    /// Pretty-print everything in the given FIT file.
    /// </summary>
    void PrintAll(string source);

    /// <summary>
    /// Duplicate the given FIT file by reading and writing each message.
    /// </summary>
    void Copy(string sourceFile, string destFile);

    /// <summary>
    /// Recalculate the workout as if each lap was run at the corresponding constant speed.
    /// </summary>
    void SetLapSpeeds(string sourceFile, string destFile, List<Speed> speeds);
  }

  public class FitService : IFitService
  {
    public string OneLine(string source) => new Reader().Read(source).OneLine();

    public void Print(string source, bool showRecords)
    {
      FitFile fitFile = new Reader().Read(source);
      fitFile?.Print(showRecords);
    }

    public void PrintAll(string source)
    {
      FitFile fitFile = new Reader().Read(source);
      Log.Info(fitFile.PrintAll());
    }

    public void Copy(string sourceFile, string destFile)
    {
      FitFile fitFile = new Reader()
        .Read(sourceFile);

      new Writer().Write(fitFile, destFile);
    }

    public void SetLapSpeeds(string sourceFile, string destFile, List<Speed> speeds)
    {
      FitFile fitFile = new Reader()
        .Read(sourceFile)
       ?.ApplySpeeds(speeds)
       ?.BackfillEvents()
       ?.Print(false);

      new Writer().Write(fitFile, destFile);
    }
  }
}
