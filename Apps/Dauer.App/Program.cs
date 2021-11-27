using Dauer.Data.Fit;
using Dauer.Model;
using Dauer.Model.Units;
using Dauer.Model.Workouts;

namespace Dauer.App
{
  public class Program
  {
    public static void Main(string[] args)
    {
      if (args.Length == 1)
      {
        Print(args[0], true);
        //Dump(args[0]);
        return;
      }

      if (args.Length != 2)
      {
        Log.Info("Usage: dauer <input.fit> [<output.fit>]");
        return;
      }

      //Copy(args[0], args[1]);

      var workout = new Model.Workouts.Workout
      (
        new Lap().WithSpeed(new Speed { Value = 6.7, Unit = SpeedUnit.MiPerHour }),
        new Lap().WithSpeed(new Speed { Value = 9.2, Unit = SpeedUnit.MiPerHour }),
        new Lap().WithSpeed(new Speed { Value = 6, Unit = SpeedUnit.MiPerHour })
      );

      ApplySpeeds(args[0], args[1], workout.Speeds);
    }

    /// <summary>
    /// Pretty-print useful information from a fit file: Session, Laps, and Records.
    /// Optionally show details about each record.
    /// </summary>
    public static void Print(string source, bool showRecords)
    {
      FitFile fitFile = new Reader().Read(source);
      fitFile?.Print(showRecords);
    }

    /// <summary>
    /// Pretty-print everything in the given FIT file.
    /// </summary>
    public static void Dump(string source)
    {
      FitFile fitFile = new Reader().Read(source);
      Log.Info(fitFile.PrintAll());
    }

    /// <summary>
    /// Duplicate the given FIT file by reading and writing each message.
    /// </summary>
    public static void Copy(string sourceFile, string destFile)
    {
      FitFile fitFile = new Reader()
        .Read(sourceFile);

       new Writer().Write(fitFile, destFile);
    }

    /// <summary>
    /// Recalculate the workout as if each lap was run at the corresponding constant speed.
    /// </summary>
    public static void ApplySpeeds(string sourceFile, string destFile, List<Speed> speeds)
    {
      FitFile fitFile = new Reader()
        .Read(sourceFile)
       ?.ApplySpeeds(speeds);
      
       new Writer().Write(fitFile, destFile);
    }
  }
}