using System;
using System.Collections.Generic;
using System.Linq;
using Dauer.Data.Fit;
using Dauer.Model.Units;
using Dynastream.Fit;
using Newtonsoft.Json;

namespace Dauer.App
{
  public class Distance
  {
    public double Value { get; set; }
    public DistanceUnit Unit { get; set; }
  }

  public class Speed
  {
    public double Value { get; set; }
    public SpeedUnit Unit { get; set; }
  }

  public class Workout
  {
    public List<Lap> Laps { get; set; } = new();

    public List<Speed> Speeds => Laps.Select(lap => lap.Speed).ToList();

    public Workout() { }
    public Workout(params Lap[] laps)
    {
      Laps = laps.ToList();
    }

    public Workout Add(Lap lap)
    {
      Laps.Add(lap);
      return this;
    }
  }

  public class Lap
  {
    public TimeSpan Duration { get; set; }
    public Distance Distance { get; set; }
    public Speed Speed { get; set; }

    public Lap() { }

    public Lap WithDuration(TimeSpan duration)
    {
      Duration = duration;
      return this;
    }

    public Lap WithDistance(Distance distance)
    {
      Distance = distance;
      return this;
    }

    public Lap WithSpeed(Speed speed)
    {
      Speed = speed;
      return this;
    }

    /// <summary>
    /// Recalculate duration from Distance and Speed
    /// Duration = Distance / Speed
    /// </summary>
    public Lap UpdateDuration()
    {
      Duration = TimeSpan.FromSeconds(Distance.Unit.Meters(Distance.Value) / Speed.Unit.MetersPerSecond(Speed.Value));
      return this;
    }

    /// <summary>
    /// Recalculate distance from Duration and Speed
    /// Distance = Speed * Duration.
    /// </summary>
    public Lap UpdateDistance()
    {
      Distance.Value = Speed.Unit.MetersPerSecond(Speed.Value) * Duration.TotalSeconds;
      Distance.Unit = DistanceUnit.Meter;
      return this;
    }

    /// <summary>
    /// Recalculate speed from Distance and Duration.
    /// Speed = Distance / Duration
    /// </summary>
    public Lap UpdateSpeed()
    {
      Speed.Value = Distance.Unit.Meters(Distance.Value) / Duration.Seconds;
      Speed.Unit = SpeedUnit.MetersPerSecond;
      return this;
    }
  }

  public class Program
  {
    public static void Main(string[] args)
    {
      if (args.Length == 1)
      {
        DumpToJson(args[0]);
        return;
      }

      if (args.Length != 2)
      {
        Console.WriteLine("Usage: dauer <input.fit> [<output.fit>]");
        return;
      }

      //Copy(args[0], args[1]);

      var workout = new Workout
      (
        new Lap().WithSpeed(new Speed { Value = 6.7, Unit = SpeedUnit.MiPerHour }),
        new Lap().WithSpeed(new Speed { Value = 9.2, Unit = SpeedUnit.MiPerHour }),
        new Lap().WithSpeed(new Speed { Value = 6, Unit = SpeedUnit.MiPerHour })
      );

      ApplyLaps(args[0], args[1], workout.Speeds);
    }

    public static void DumpToJson(string source)
    {
      var fitFile = new Reader().Read(source);

      Console.WriteLine(JsonConvert.SerializeObject(fitFile, Formatting.Indented));
    }

    public static void Copy(string sourceFile, string destFile)
    {
      var fitFile = new Reader().Read(sourceFile);
      new Writer().Write(fitFile, destFile);
    }

    public static void ApplyLaps(string sourceFile, string destFile, List<Speed> speeds)
    {
      var fitFile = new Reader().Read(sourceFile);

      Dictionary<int, LapMesg> laps = fitFile.Messages
        .Where(message => message.Num == MesgNum.Lap)
        .Select((message, index) => new { index = fitFile.Messages.IndexOf(message), message = new LapMesg(message) })
        .ToDictionary(obj => obj.index, obj => obj.message);
    
      Dictionary<int, RecordMesg> records = fitFile.Messages
        .Where(message => message.Num == MesgNum.Record)
        .Select((message, index) => new { index = fitFile.Messages.IndexOf(message), message = new RecordMesg(message) })
        .ToDictionary(obj => obj.index, obj => obj.message);
      
      Dictionary<int, SessionMesg> sessions = fitFile.Messages
        .Where(message => message.Num == MesgNum.Session)
        .Select((message, index) => new { index = fitFile.Messages.IndexOf(message), message = new SessionMesg(message) })
        .ToDictionary(obj => obj.index, obj => obj.message);

      if (laps.Count != speeds.Count)
      {
        throw new ArgumentException($"Found {laps.Count} laps but {speeds.Count} speeds");
      }

      Dictionary<int, int> lapMap = new();

      int lapIndex = 0;
      foreach (var lap in laps)
      {
        lapMap[lap.Key] = lapIndex;
        lap.Value.SetEnhancedAvgSpeed((float)speeds[lapIndex].Unit.MetersPerSecond(speeds[lapIndex].Value));
        lapIndex++;
      }

      // Sort earliest to latest
      List<KeyValuePair<int, RecordMesg>> recordsList = records.ToList();
      recordsList.Sort((a, b) => a.Value.GetTimestamp().CompareTo(b.Value.GetTimestamp()));

      double distance = 0;
      int lastTimestamp = (int)recordsList[0].Value.GetTimestamp().GetTimeStamp();

      foreach (int i in Enumerable.Range(0, recordsList.Count))
      {
        // Find relevant lap
        var lap = laps.First(lap =>
        {
          uint lapStartTime = lap.Value.GetStartTime().GetTimeStamp();
          uint lapEndTime = lap.Value.GetTimestamp().GetTimeStamp();
          uint recordStartTime = recordsList[i].Value.GetTimestamp().GetTimeStamp();

          return lapStartTime <= recordStartTime && recordStartTime <= lapEndTime;
        });

        int j = lapMap[lap.Key];

        double speed = speeds[j].Unit.MetersPerSecond(speeds[j].Value);

        int timestamp = (int)recordsList[i].Value.GetTimestamp().GetTimeStamp();
        int elapsedSeconds = timestamp - lastTimestamp;
        lastTimestamp = timestamp;

        distance += speed * elapsedSeconds;

        lap.Value.SetTotalDistance((float)distance);
        recordsList[i].Value.SetDistance((float)distance);
        recordsList[i].Value.SetEnhancedSpeed((float)speed);
      }

      sessions.First().Value.SetTotalDistance((float)distance);

      // Write changed records back into FitFile
      foreach (var kvp in laps)
      {
        fitFile.Messages[kvp.Key] = kvp.Value;
      }
      foreach (var kvp in records)
      {
        fitFile.Messages[kvp.Key] = kvp.Value;
      }
      foreach (var kvp in sessions)
      {
        fitFile.Messages[kvp.Key] = kvp.Value;
      }

      // Write to File
      new Writer().Write(fitFile, destFile);
    }
  }
}