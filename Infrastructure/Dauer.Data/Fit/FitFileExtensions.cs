using System.Text;
using System.Text.Json;
using Dauer.Model.Workouts;
using Dynastream.Fit;
using Units;

namespace Dauer.Data.Fit
{
  public static class FitFileExtensions
  {
    public static List<T> Get<T>(this FitFile f) where T : Mesg => f.Messages
      .Where(message => message.Num == MessageFactory.MesgNums[typeof(T)])
      .Select(message => message as T)
      .ToList();

    /// <summary>
    /// Compute Session, Records, and Laps from Events
    /// </summary>
    public static FitFile ForwardfillEvents(this FitFile f)
    {
      f.Sessions = f.Get<SessionMesg>();
      f.Laps = f.Get<LapMesg>();
      f.Records = f.Get<RecordMesg>();
      // Expensive; 2000 records take ~0.3s in WASM
      //f.Records = f.Get<RecordMesg>().Sorted(MessageExtensions.Sort);

      return f;
    }

    /// <summary>
    /// Fill modified Session, Records, Laps, etc, into Events
    /// </summary>
    public static FitFile BackfillEvents(this FitFile f, int resolution = 100, Action<int, int> handleProgress = null)
    {
      int li = 0;
      int ri = 0;
      int si = 0;

      int i = 0;

      // Sources
      var sessions = f.Sessions;
      var laps = f.Laps;
      var records = f.Records;

      // Destination
      var events = f.Events.OfType<MesgEventArgs>().ToList();

      foreach (MesgEventArgs e in events)
      {
        if (i % resolution == 0)
        {
          handleProgress?.Invoke(i, f.Events.Count);
        }
        i++;

        if (!MessageFactory.Types.TryGetValue(e.mesg.Num, out Type t))
        {
          continue;
        }

        if (t == typeof(SessionMesg))
        {
          e.mesg = sessions[si++];
        }
        else if (t == typeof(LapMesg))
        {
          e.mesg = laps[li++];
        }
        else if (t == typeof(RecordMesg))
        {
          e.mesg = records[ri++];
        }
      }

      return f;
    }

    public static float? TotalDistance(this IEnumerable<SessionMesg> sessions) => sessions.Sum(sess => sess.GetTotalDistance());
    public static float? TotalElapsedTime(this IEnumerable<SessionMesg> sessions) => sessions.Sum(sess => sess.GetTotalElapsedTime());

    /// <summary>
    /// Return a one-line description of a fit file
    /// </summary>
    public static string OneLine(this FitFile f) => f.Sessions.Count switch
    {
      1 => $"From {f.Sessions[0].Start()} to {f.Sessions[0].End()}: {f.Sessions[0].GetTotalDistance()} m in {f.Sessions[0].GetTotalElapsedTime()}s ({f.Sessions[0].GetEnhancedAvgSpeed():0.##} m/s)",
      _ when f.Sessions.Count > 1 => $"From {f.Sessions.First().Start()} to {f.Sessions.Last().End()}: {f.Sessions.TotalDistance()} m in {f.Sessions.TotalElapsedTime()}s",
      _ => "No sessions",
    };

    public static string Print(this FitFile f, bool showRecords)
    {
      var sb = new StringBuilder();
      f.Print(s => sb.AppendLine(s), showRecords);
      return sb.ToString();
    }

    /// <summary>
    /// Pretty-print useful information from a fit file: Session, Laps, and Records
    /// </summary>
    public static FitFile Print(this FitFile f, Action<string> print, bool showRecords)
    {
      if (f == null)
      {
        return f;
      }

      var sessions = f.Sessions;
      var laps = f.Laps;
      var records = f.Records;

      print($"Fit File: ");
      print($"  {records.Count} {(laps.Count == 1 ? "record" : "records")}");
      print($"  {sessions.Count} {(sessions.Count == 1 ? "session" : "sessions")}:");

      foreach (var sess in sessions)
      {
        print($"    From {sess.Start()} to {sess.End()}: {sess.GetTotalDistance()} m in {sess.GetTotalElapsedTime()}s ({sess.GetEnhancedAvgSpeed():0.##} m/s)");
      }

      print($"  {laps.Count} {(laps.Count == 1 ? "lap" : "laps")}:");
      foreach (var lap in laps)
      {
        print($"    From {lap.Start()} to {lap.End()}: {lap.GetTotalDistance()} m in {lap.GetTotalElapsedTime()}s ({lap.GetEnhancedAvgSpeed():0.##} m/s)");

        var lapRecords = records.Where(rec => rec.Start() > lap.Start() && rec.Start() < lap.End())
                                .ToList();

        print($"      {lapRecords.Count} {(lapRecords.Count == 1 ? "record" : "records")}");

        if (!showRecords)
        {
          continue;
        }

        foreach (var rec in lapRecords)
        {
          var speed = new Speed { Unit = Unit.MetersPerSecond, Value = (double)rec.GetEnhancedSpeed() };
          var distance = new Distance { Unit = Unit.Meter, Value = (double)rec.GetDistance() };

          print($"        At {rec.Start():HH:mm:ss}: {distance.Miles():0.##} mi, {speed.Convert(Unit.MinutesPerMile)}, {rec.GetHeartRate()} bpm, {(rec.GetCadence() + rec.GetFractionalCadence()) * 2} cad");
          //print($"        At {rec.Start():HH:mm:ss}: {rec.GetDistance():0.##} m, {rec.GetEnhancedSpeed():0.##} m/s, {rec.GetHeartRate()} bpm, {(rec.GetCadence() + rec.GetFractionalCadence()) * 2} cad");
        }
      }

      return f;
    }

    /// <summary>
    /// Pretty-print everything in the given FIT file.
    /// </summary>
    public static string PrintAll(this FitFile f) => JsonSerializer.Serialize(f, new JsonSerializerOptions { WriteIndented = true });

    /// <summary>
    /// Recalculate the workout as if each lap was run at the corresponding constant speed.
    /// Return the same modified FitFile.
    /// </summary>
    public static FitFile ApplySpeeds(this FitFile fitFile, Dictionary<int, Speed> lapSpeeds, int resolution = 100, Action<int, int> handleProgress = null)
    {
      var laps = fitFile.Laps;
      var records = fitFile.Records;
      var sessions = fitFile.Sessions;

      if (!lapSpeeds.Any())
      {
        return fitFile;
      }

      if (!records.Any())
      {
        throw new ArgumentException($"Could not find any records");
      }

      if (!sessions.Any())
      {
        throw new ArgumentException($"Could not find any sessions");
      }

      foreach (int i in lapSpeeds.Keys)
      {
        laps[i].Apply(lapSpeeds[i]);
      }

      var distance = new Distance { Unit = Unit.Meter };
      var lapDistances = Enumerable.Range(0, laps.Count)
        .Select(_ => new Distance { Unit = Unit.Meter })
        .ToList();

      System.DateTime lastTimestamp = records.First().Start();

      int recordIndex = 0;
      foreach (RecordMesg record in records)
      {
        if (recordIndex % resolution == 0)
        {
          handleProgress?.Invoke(recordIndex, records.Count);
        }
        recordIndex++;

        LapMesg lap = record.FindLap(laps);

        int lapIndex = laps.IndexOf(lap);

        double speed = lapSpeeds.TryGetValue(lapIndex, out Speed value) 
          ? value.Convert(Unit.MetersPerSecond).Value 
          : record.GetEnhancedSpeed() ?? 0;

        System.DateTime timestamp = record.Start();
        double elapsedSeconds = (timestamp - lastTimestamp).TotalSeconds;
        lastTimestamp = timestamp;

        distance.Value += speed * elapsedSeconds;
        lapDistances[lapIndex].Value += speed * elapsedSeconds;

        record.SetDistance((float)distance.Meters());
        record.SetEnhancedSpeed((float)speed);
      }

      foreach (int i in Enumerable.Range(0, laps.Count))
      {
        laps[i].SetTotalDistance((float)lapDistances[i].Meters());
      }

      SessionMesg session = sessions.FirstOrDefault();
      session?.Apply(distance, lapSpeeds.Values.MaxBy(s => s.Value));

      return fitFile;
    }
  }
}