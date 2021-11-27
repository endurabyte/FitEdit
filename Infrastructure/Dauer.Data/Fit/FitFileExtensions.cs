using Dauer.Model;
using Dauer.Model.Units;
using Dauer.Model.Workouts;
using Dynastream.Fit;
using Newtonsoft.Json;

namespace Dauer.Data.Fit
{
  public static class FitFileExtensions
  {
    /// <summary>
    /// Pretty-print useful information from a fit file: Session, Laps, and Records
    /// </summary>
    public static void Print(this FitFile f, bool showRecords)
    {

      var sessions = f.Sessions;
      var laps = f.Laps;
      var records = f.Records;

      Log.Info($"Fit File: ");
      Log.Info($"  {records.Count} {(laps.Count == 1 ? "record" : "records")}:");
      Log.Info($"  {sessions.Count} {(sessions.Count == 1 ? "session" : "sessions")}:");

      foreach (var sess in sessions)
      {
        Log.Info($"    From {sess.Start()} to {sess.End()}: {sess.GetTotalDistance()} m in {sess.GetTotalElapsedTime()}s ({sess.GetEnhancedAvgSpeed():0.##} m/s)");
      }

      Log.Info($"  {laps.Count} {(laps.Count == 1 ? "lap" : "laps")}:");
      foreach (var lap in laps)
      {
        Log.Info($"    From {lap.Start()} to {lap.End()}: {lap.GetTotalDistance()} m in {lap.GetTotalElapsedTime()}s ({lap.GetEnhancedAvgSpeed():0.##} m/s)");

        var lapRecords = records.Where(rec => rec.Start() > lap.Start() && rec.Start() < lap.End())
                                .ToList();

        Log.Info($"      {lapRecords.Count} {(laps.Count == 1 ? "record" : "records")}");

        if (!showRecords)
        {
          continue;
        }

        foreach (var rec in lapRecords)
        {
          //Log.Info($"        At {rec.Start():HH:mm:ss}: {rec.GetDistance():0.##} m, {rec.GetEnhancedSpeed():0.##} m/s, {rec.GetHeartRate()} bpm, {(rec.GetCadence() + rec.GetFractionalCadence()) * 2} cad");

          var speed = new Speed { Unit = SpeedUnit.MetersPerSecond, Value = (double)rec.GetEnhancedSpeed() };
          var distance = new Distance { Unit = DistanceUnit.Meter, Value = (double)rec.GetDistance() };

          // Print the fractional part of the given number as
          // seconds of a minute e.g. 8.9557 => 8:57
          string pretty(double minPerMile)
          {
            int floor = (int)Math.Floor(minPerMile);
            return $"{floor}:{(int)((minPerMile - floor)*60):00}";
          }

          Log.Info($"        At {rec.Start():HH:mm:ss}: {distance.Miles():0.##} mi, {pretty(speed.MinutesPerMile())} min/mi, {rec.GetHeartRate()} bpm, {(rec.GetCadence() + rec.GetFractionalCadence()) * 2} cad");
        }
      }
    }

    /// <summary>
    /// Pretty-print everything in the given FIT file.
    /// </summary>
    public static string PrintAll(this FitFile f) => JsonConvert.SerializeObject(f, Formatting.Indented);

    /// <summary>
    /// Recalculate the workout as if each lap was run at the corresponding constant speed.
    /// Return the same modified FitFile.
    /// </summary>
    public static FitFile ApplySpeeds(this FitFile fitFile, List<Speed> speeds)
    {
      var laps = fitFile.Get<LapMesg>();
      var records = fitFile.Get<RecordMesg>();
      var sessions = fitFile.Get<SessionMesg>();

      if (laps.Count != speeds.Count)
      {
        throw new ArgumentException($"Found {laps.Count} laps but {speeds.Count} speeds");
      }

      if (!records.Any())
      {
        throw new ArgumentException($"Could not find any records");
      }

      if (!sessions.Any())
      {
        throw new ArgumentException($"Could not find any sessions");
      }

      foreach (int i in Enumerable.Range(0, laps.Count))
      {
        laps[i].Apply(speeds[i]);
      }

      var distance = new Distance { Unit = DistanceUnit.Meter };

      System.DateTime lastTimestamp = records.First().Start();

      foreach (RecordMesg record in records)
      {
        LapMesg lap = record.FindLap(laps);

        int j = laps.IndexOf(lap);

        double speed = speeds[j].MetersPerSecond();

        System.DateTime timestamp = record.Start();
        double elapsedSeconds = (timestamp - lastTimestamp).TotalSeconds;
        lastTimestamp = timestamp;

        distance.Value += speed * elapsedSeconds;

        lap.SetTotalDistance((float)distance.Meters());
        record.SetDistance((float)distance.Meters());
        record.SetEnhancedSpeed((float)speed);
      }

      SessionMesg session = sessions.FirstOrDefault();
      session?.Apply(distance, speeds.MaxBy(s => s.Value));

      return fitFile;
    }
  }
}