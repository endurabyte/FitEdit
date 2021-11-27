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
      FitFile fitFile = new Reader().Read(sourceFile);

      var laps = fitFile.Get<LapMesg>();
      var records = fitFile.Get<RecordMesg>();
      var sessions = fitFile.Get<SessionMesg>();

      if (laps.Count != speeds.Count)
      {
        throw new ArgumentException($"Found {laps.Count} laps but {speeds.Count} speeds");
      }

      int lapIndex = 0;
      foreach (int i in Enumerable.Range(0, laps.Count))
      {
        laps[i].SetEnhancedAvgSpeed((float)speeds[lapIndex].Unit.MetersPerSecond(speeds[lapIndex].Value));
      }

      // Sort earliest to latest
      records.Sort((a, b) => a.GetTimestamp().CompareTo(b.GetTimestamp()));

      double distance = 0;
      int lastTimestamp = (int)records[0].GetTimestamp().GetTimeStamp();

      foreach (int i in Enumerable.Range(0, records.Count))
      {
        // Find relevant lap
        var lap = laps.First(lap =>
        {
          uint lapStartTime = lap.GetStartTime().GetTimeStamp();
          uint lapEndTime = lap.GetTimestamp().GetTimeStamp();
          uint recordStartTime = records[i].GetTimestamp().GetTimeStamp();

          return lapStartTime <= recordStartTime && recordStartTime <= lapEndTime;
        });

        int j = laps.IndexOf(lap);

        double speed = speeds[j].Unit.MetersPerSecond(speeds[j].Value);

        int timestamp = (int)records[i].GetTimestamp().GetTimeStamp();
        int elapsedSeconds = timestamp - lastTimestamp;
        lastTimestamp = timestamp;

        distance += speed * elapsedSeconds;

        lap.SetTotalDistance((float)distance);
        records[i].SetDistance((float)distance);
        records[i].SetEnhancedSpeed((float)speed);
      }

      sessions.First().SetTotalDistance((float)distance);

      // Write to File
      new Writer().Write(fitFile, destFile);
    }
  }
}