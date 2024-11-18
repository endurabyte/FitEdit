using System.Text;
using System.Text.Json;
using FitEdit.Model.Extensions;
using FitEdit.Model.Workouts;
using Dynastream.Fit;
using Units;
using DateTime = System.DateTime;

namespace FitEdit.Data.Fit;

public static class FitFileExtensions
{
  public static void Append(this FitFile f, FitFile other)
  {
    f.Events.AddRange(other.Events);

    foreach (var kvp in other.MessageDefinitions)
    {
      f.MessageDefinitions[kvp.Key] = kvp.Value;
    }

    foreach (var kvp in other.MessagesByDefinition)
    {
      f.MessagesByDefinition[kvp.Key] = kvp.Value;
    }

    f.Sessions.AddRange(other.Sessions);
    f.Laps.AddRange(other.Laps);
    f.Records.AddRange(other.Records);
  }

  public static byte[] GetBytes(this FitFile f)
  {
    var ms = new MemoryStream();
    new Writer().Write(f, ms);
    return ms.ToArray();
  }

  /// <summary>
  /// Return the timestamp that the workout started. Try to get it in the following order:
  /// - From the first FileID message. 
  /// - From the first Event message
  /// - From the first Record message.
  /// - From the first Lap message.
  /// - From the first Session message.
  /// - From the first Activity message.
  /// Return default if none of the above are found.
  /// </summary>
  public static DateTime GetStartTime(this FitFile f) =>
        (f.Messages.FirstOrDefault(mesg => mesg is FileIdMesg) as FileIdMesg)?.GetTimeCreated()?.GetDateTime()
      ?? (f.Messages.FirstOrDefault(mesg => mesg is EventMesg) as EventMesg)?.Start()
      ?? f.Records.FirstOrDefault()?.InstantOfTime()
      ?? f.Laps.FirstOrDefault()?.Start()
      ?? f.Sessions.FirstOrDefault()?.Start()
      ?? (f.Messages.FirstOrDefault(mesg => mesg is ActivityMesg) as ActivityMesg)?.InstantOfTime()
      ?? default;

  /// <summary>
  /// Return the timestamp that the workout started. Try to get it in the following order:
  /// - From the last Record message.
  /// - From the last Event message
  /// - From the last Lap message.
  /// - From the last Session message.
  /// - From the last Activity message.
  /// Return default if none of the above are found.
  /// </summary>
  public static DateTime GetEndTime(this FitFile f) =>
         f.Records.LastOrDefault()?.InstantOfTime()
      ?? (f.Messages.LastOrDefault(mesg => mesg is EventMesg) as EventMesg)?.End()
      ?? f.Laps.LastOrDefault()?.GetLapEndTime()
      ?? f.Sessions.LastOrDefault()?.End()
      ?? (f.Messages.LastOrDefault(mesg => mesg is ActivityMesg) as ActivityMesg)?.InstantOfTime()
      ?? default;

  public static List<T> Get<T>(this FitFile f) where T : Mesg => f.Messages
    .Where(message => message.Num == MessageFactory.MesgNums[typeof(T)])
    .Select(message => message as T)
    .ToList();

  /// <summary>
  /// Return only the <see cref="T"/>s which occur between the given times.
  /// Used for e.g. Laps, Sessions, and other messages hich span a duration of time and don't only occupy an instant in time.
  /// </summary>
  public static IEnumerable<T> DurationBetween<T>(this IEnumerable<T> ts, DateTime after = default, DateTime before = default)
    where T : IDurationOfTime => ts.Where(t => t.Start() >= after && t.End() < before);

  /// <summary>
  /// Return only the <see cref="T"/>s which occur in the given <see cref="System.DateTime"/> range.
  /// Used for e.g. Records and other messages which don't span a duration of time and instead only occupy an instant in time.
  /// </summary>
  public static IEnumerable<T> InstantBetween<T>(this IEnumerable<T> ts, DateTime after = default, DateTime before = default)
    where T : IInstantOfTime => ts.Where(t => t.InstantOfTime() >= after && t.InstantOfTime() < before);

  /// <summary>
  /// Return only the <see cref="T"/>s which occur in the given <see cref="Dynastream.Fit.DateTime"/> range.
  /// Used for e.g. Records and other messages which don't span a duration of time and instead only occupy an instant in time.
  /// </summary>
  public static IEnumerable<T> InstantBetween<T>(this IEnumerable<T> ts, 
    Dynastream.Fit.DateTime after = default, 
    Dynastream.Fit.DateTime before = default
  ) where T : IInstantOfTime => ts.Where(t =>
    {
      var timestamp = t.GetTimestamp();
      if (timestamp is null) { return false; }

      return timestamp.CompareTo(after) >= 0 && timestamp.CompareTo(before) < 0;
    });

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
    > 1 => $"From {f.Sessions.First().Start()} to {f.Sessions.Last().End()}: {f.Sessions.TotalDistance()} m in {f.Sessions.TotalElapsedTime()}s",
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
  private static FitFile Print(this FitFile f, Action<string> print, bool showRecords)
  {
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

      var lapRecords = records.InstantBetween(lap.Start(), lap.End()).ToList();

      print($"      {lapRecords.Count} {(lapRecords.Count == 1 ? "record" : "records")}");

      if (!showRecords)
      {
        continue;
      }

      foreach (var rec in lapRecords)
      {
        var speed = new Speed { Unit = Unit.MetersPerSecond, Value = rec.GetEnhancedSpeed() ?? 0 };
        var distance = new Distance { Unit = Unit.Meter, Value = rec.GetDistance() ?? 0 };

        print($"        At {rec.InstantOfTime():HH:mm:ss}: {distance.Miles():0.##} mi, {speed.Convert(Unit.MinutesPerMile)}, {rec.GetHeartRate()} bpm, {(rec.GetCadence() + rec.GetFractionalCadence()) * 2} cad");
        //print($"        At {rec.Start():HH:mm:ss}: {rec.GetDistance():0.##} m, {rec.GetEnhancedSpeed():0.##} m/s, {rec.GetHeartRate()} bpm, {(rec.GetCadence() + rec.GetFractionalCadence()) * 2} cad");
      }
    }

    return f;
  }

  public static List<string> PrintEvents(this FitFile fitFile) =>
    fitFile.Events.Select(message => message switch
    {
      MesgEventArgs mesgArgs => mesgArgs.MapString(),
      MesgDefinitionEventArgs mesgDefArgs => mesgDefArgs.MapString(),
      _ => ""
    }).ToList();

  /// <summary>
  /// Pretty-print everything in the given FIT file.
  /// </summary>
  public static string ToJson(this FitFile f) => JsonSerializer.Serialize(f, new JsonSerializerOptions { WriteIndented = true });

  public static string PrintBytes(this FitFile f)
  {
    var sb = new StringBuilder();
    f.PrintBytes(s => sb.AppendLine(s));
    return sb.ToString();
  }

  public static void PrintBytes(this FitFile f, Action<string> print)
  {
    foreach (var e in f.Events)
    {
      string data = e.PrintBytes();
      print(data);
    }
  }

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

    DateTime lastTimestamp = records.First().InstantOfTime();

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

      DateTime timestamp = record.InstantOfTime();
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

#nullable enable

  public static FitFile? Merge(List<FitFile> files)
  {
    if (files.Count < 2) { return null; }

    List<FitFile> sorted = files
      .OrderBy(f => f.GetStartTime())
      .ToList();

    var merged = new FitFile();

    // Strategy: Concatenate all messages and remove the duplicates.
    // e.g. there must only be one ActivityMesg, so keep only the first.
    foreach (var file in sorted)
    {
      merged.Append(file);
    }

    var activityMesg = merged.FindFirst<ActivityMesg>();
    DateTime firstActivityTime = activityMesg?.GetTimestamp().GetDateTime() ?? default;

    merged.RemoveNonfirst(typeof(FileIdMesg));
    merged.RemoveNonfirst(typeof(FileCreatorMesg));
    merged.RemoveNonfirst(typeof(DeviceSettingsMesg));
    merged.RemoveNonfirst(typeof(UserProfileMesg));
    merged.RemoveNonfirst(typeof(ActivityMesg));
    merged.RemoveAll(typeof(DeviceInfoMesg), after: firstActivityTime);

    return merged;
  }

  
  public static (FitFile first, FitFile second) SplitAt(this FitFile source, DateTime at)
  {
    DateTime start = source.GetStartTime();
    DateTime end = source.GetEndTime();

    return (
      source.ExtractRecords(start, at)!,
      source.ExtractRecords(at, end)!
    );
  }
  
  public static List<FitFile> SplitByLap(this FitFile? source)
  {
    var laps = source.Get<LapMesg>();

    return laps
      .Select(lap => source.ExtractRecords(lap.Start(), lap.End())!)
      .ToList();
  }

  private static FitFile? ExtractRecords(this FitFile? source, DateTime start, DateTime end) => 
    RepairAdditively(source, GetRecords(source, start, end));

  public static List<LapMesg> GetLaps(this FitFile? source, DateTime start, DateTime end) => source
      .Get<LapMesg>()
      .InstantBetween(start, end)
      .ToList()
      .Sorted(MessageExtensions.SortByTimestamp);
  
  public static List<RecordMesg> GetRecords(this FitFile? source, DateTime start, DateTime end) => source
      .Get<RecordMesg>()
      .InstantBetween(start, end)
      .ToList()
      .Sorted(MessageExtensions.SortByTimestamp);

  private static List<T> FindAll<T>(this FitFile f) where T : Mesg => f.FindAll(typeof(T)).Cast<T>().ToList(); 

  private static List<Mesg> FindAll(this FitFile f, Type t) => f.Events.OfType<MesgEventArgs>()
      .Where(mea => mea.mesg.GetType() == t)
      .Select(mea => mea.mesg)
      .ToList();

  private static T? FindFirst<T>(this FitFile f) where T : Mesg => f.FindFirst(typeof(T)) as T;

  private static Mesg? FindFirst(this FitFile f, Type t) => f.Events.OfType<MesgEventArgs>()
      .Where(mea => mea.mesg.GetType() == t)
      .Select(mea => mea.mesg)
      .FirstOrDefault();

  /// <summary>
  /// Build a new FIT file from the given file by removing unnecessary messages.
  ///
  /// <para/>
  /// This method preserves more information than <see cref="RepairAdditively(FitFile?)"/>,
  /// such as individual sports in multisport events like triathlons, sessions, and laps.
  /// Use that one when this one does not work.
  /// </summary>
  public static FitFile? RepairSubtractively(this FitFile? source)
  {
    if (source == null) { return null; }
    var dest = new FitFile();

    var typeFilter = new HashSet<Type>
    {
      typeof(FileIdMesg),
      typeof(FileCreatorMesg),
      typeof(EventMesg),
      typeof(DeviceInfoMesg),
      typeof(DeviceSettingsMesg),
      typeof(UserProfileMesg),
      typeof(SportMesg),
      typeof(SessionMesg),
      typeof(RecordMesg),
      typeof(LapMesg),
      typeof(ActivityMesg),
    };
    var mesgNumFilter = new HashSet<ushort>(typeFilter.Select(type => MessageFactory.MesgNums[type]));

    var filteredEvents = source.Events.Where(e =>
    {
      if (e is MesgEventArgs mea && typeFilter.Contains(mea.mesg.GetType()))
      {
        if (!dest.MessagesByDefinition.ContainsKey(mea.mesg.Num))
        {
          dest.MessagesByDefinition[mea.mesg.Num] = new List<Mesg>() { mea.mesg };
        }
        else
        {
          dest.MessagesByDefinition[mea.mesg.Num].Add(mea.mesg);
        }
        return true;
      }

      if (e is MesgDefinitionEventArgs mdea && mesgNumFilter.Contains(mdea.mesgDef.GlobalMesgNum))
      {
        dest.MessageDefinitions[mdea.mesgDef.GlobalMesgNum] = mdea.mesgDef;
        return true;
      }

      return false;
    });

    dest.Events.AddRange(filteredEvents);
    dest.ForwardfillEvents();

    return dest;
  }

  /// <summary>
  /// Build a new FIT file from the given file by condensing it into one each of: Session, Lap, Sport, and Activity,
  /// along with the other messages required to make it uploadable to Garmin Connect.
  /// 
  /// <para/>
  /// For example, multisport events like triathlons will be condensed into one sport.
  /// The sport chosen will be the first in the given file, i.e. typically open water swim. 
  /// This can be changed on Garmin Connect, Strava, and other online tools.
  /// The repaired file includes all lat/lon records except those with speed spikes (speeds greater than 1000m/s)
  /// 
  /// <para/>
  /// This method does not preserve as much information as <see cref="RepairSubtractively(FitFile?)"/>,
  /// such as individual sports in multisport events, sessions, and laps.
  /// Use this one when that one does not work.
  /// </summary>
  public static FitFile? RepairAdditively(this FitFile? source)
  {
    if (source == null) { return null; }

    List<RecordMesg> records = source
      .Get<RecordMesg>()
      .Where(r => r.GetEnhancedSpeed() == null || r.GetEnhancedSpeed() < 1000) // filter out speed spikes
      .ToList()
      .Sorted(MessageExtensions.SortByTimestamp);

    return RepairAdditively(source, records);
  }

  private static FitFile? RepairAdditively(this FitFile? source, List<RecordMesg> records)
  {
    if (source == null) { return null; }

    var start = new Dynastream.Fit.DateTime(source.GetStartTime());
    var end = new Dynastream.Fit.DateTime(source.GetEndTime());

    var fileId = source.Get<FileIdMesg>().FirstOrDefault() ?? new FileIdMesg();
    fileId.SetTimeCreated(start);

    var fileCreator = source.Get<FileCreatorMesg>().FirstOrDefault();
    var deviceInfo = source.Get<DeviceInfoMesg>().InstantBetween(start, end);
    var deviceSettings = source.Get<DeviceSettingsMesg>().FirstOrDefault();
    var userProfile = source.Get<UserProfileMesg>().FirstOrDefault();
    var sport = source.Get<SportMesg>().FirstOrDefault();

    var startEvent = new EventMesg();
    startEvent.SetTimestamp(start);
    startEvent.SetEvent(Event.Timer);
    startEvent.SetEventType(EventType.Start);

    var stopEvent = new EventMesg();
    stopEvent.SetTimestamp(end);
    stopEvent.SetEvent(Event.Timer);
    stopEvent.SetEventType(EventType.StopDisableAll);

    List<RecordMesg> records2 = EnsureCumulativeDistance(records);
    SessionMesg session = ReconstructSession(source, records2);
    LapMesg lap = source.ReconstructLap(records2);
    ActivityMesg activity = ReconstructActivity(source);

    List<Mesg> mesgs = new();

    mesgs.Add(fileId);
    if (fileCreator != null) { mesgs.Add(fileCreator); }
    mesgs.Add(startEvent);
    if (deviceInfo != null) { mesgs.AddRange(deviceInfo); }
    if (deviceSettings != null) { mesgs.Add(deviceSettings); }
    if (userProfile != null) { mesgs.Add(userProfile); }
    if (sport != null)
    {
      session.SetSport(sport.GetSport());
      session.SetSubSport(sport.GetSubSport());

      lap.SetSport(sport.GetSport());
      lap.SetSubSport(sport.GetSubSport());

      mesgs.Add(sport);
    }

    mesgs.Add(session);
    mesgs.AddRange(records2);
    mesgs.Add(stopEvent);
    mesgs.Add(lap);
    mesgs.Add(activity);

    var dest = new FitFile();

    foreach (Mesg mesg in mesgs)
    {
      dest.Add(mesg);
    }

    dest.Events.AddRange(dest.Records.Select(r => new MesgEventArgs(r)));
    dest.ForwardfillEvents();

    return dest;
  }

  /// <summary>
  /// The distance of each record should be cumulative.
  /// If it is not, assume the discontinuity is due to a sketchy FIT file merge
  /// and repair it by making the distance cumulative.
  /// Return a new list of records; do not modify the given list.
  /// 
  /// <para/>
  /// Algorithm:
  /// Assume the given records are sorted by timestamp.
  /// Iterate over all records from earliest to latest. 
  /// If a record's distance is less than the previous record's distance, 
  /// add the previous record's distance to the current record's distance.
  /// 
  /// <para/>
  /// Example: A good file.
  /// 
  /// <code>
  /// Record | Distance
  ///   1        10m
  ///   2        20m
  ///   3        30m
  ///   4        40m
  ///       ...
  ///   100      100m
  /// </code>
  /// 
  /// <para/>
  /// Example: A problematic file.
  /// 
  /// <code>
  /// Record | Distance
  ///   1        10m
  ///   2        20m
  ///   3         0m   // Distance should be 20m (== 20m + 0m).
  ///                  // Somebody probably tried to merge two FIT files.
  ///   4        10m   // Should be 30m ( == 20m + 10m)
  ///       ...
  ///   100      100m
  /// </code>
  /// 
  /// </summary>
  private static List<RecordMesg> EnsureCumulativeDistance(List<RecordMesg> source)
  {
    var dest = source.Select(r => new RecordMesg(r)).ToList();

    double cumulative = 0;

    foreach (int i in Enumerable.Range(0, dest.Count))
    {
      double distance = dest[i].GetDistance() ?? 0;

      if (distance < cumulative)
      {
        // Add cumulative distance to all subsequent records
        dest[i..].AddDistance(cumulative);
      }
      else
      {
        cumulative = distance;
      }
    }

    return dest;
  } 

  private static void AddDistance(this IEnumerable<RecordMesg> records, double distance)
  {
    foreach (RecordMesg record in records)
    {
      record.AddDistance(distance);
    }
  }

  private static void AddDistance(this RecordMesg record, double distance)
  {
    double? current = record.GetDistance();
    record.SetDistance((float)((current ?? 0) + distance));
  }

  /// <summary>
  /// Repair by adding missing Session and Activity messages
  /// </summary>
  public static FitFile? RepairBackfillMissingFields(this FitFile? source)
  {
    if (source == null) { return null; }

    var dest = new FitFile(source);

    // First try to reconstruct sessions from laps
    ReconstructSessions(source, dest);

    const double spike = 1000; // meters per second

    List<RecordMesg> records = source
      .Get<RecordMesg>()
      .Where(r => r.GetEnhancedSpeed() < spike)
      .ToList()
      .Sorted(MessageExtensions.SortByTimestamp);

    var sport = source.Get<SportMesg>().FirstOrDefault();
    var firstLap = source.Get<LapMesg>().FirstOrDefault();
    var session = source.Get<SessionMesg>().FirstOrDefault();
    var activity = source.Get<ActivityMesg>().FirstOrDefault();

    // Garmin requires FileId message to have a time created
    var fileId = source.Get<FileIdMesg>().FirstOrDefault();
    fileId!.SetTimeCreated(new Dynastream.Fit.DateTime(source.GetStartTime()));

    if (!records.Any())
    {
      DateTime start = source.GetStartTime();
      DateTime end = source.GetEndTime();

      records.Add(GetFakeRecord(when: new Dynastream.Fit.DateTime(start)));
      records.Add(GetFakeRecord(when: new Dynastream.Fit.DateTime(end)));
      records.ForEach(dest.Add);
    }

    if (firstLap is null)
    {
      firstLap = source.ReconstructLap(records);

      if (sport != null)
      {
        firstLap.SetSport(sport.GetSport());
        firstLap.SetSubSport(sport.GetSubSport());
      }

      dest.Add(firstLap);
    }

    // If there are still no session messages, fallback to creating one from records
    if (session is null)
    {
      session = ReconstructSession(source, records);
      if (sport != null)
      {
        session.SetSport(sport.GetSport());
        session.SetSubSport(sport.GetSubSport());
      }

      dest.Add(session);
    }

    if (activity is null)
    {
      activity = ReconstructActivity(source);
      dest.Add(activity);
    }

    dest.ForwardfillEvents();
    return dest;
  }

  private static DateTime? GetLapEndTime(this LapMesg lap)
  {
    // The lap Timestamp is often incorrect. Instead, use the start time + duration
    //return lap.End();

    Dynastream.Fit.DateTime? lapEnd = lap.GetStartTime();
    float? elapsed = lap.GetTotalElapsedTime();

    if (elapsed != null)
    {
      lapEnd.Add(new Dynastream.Fit.DateTime((uint)elapsed));
    }

    return lapEnd?.GetDateTime();
  } 

  public static LapMesg ReconstructLap(List<RecordMesg> records, DateTime start, DateTime end) =>
    ReconstructLap(records, new Dynastream.Fit.DateTime(start), new Dynastream.Fit.DateTime(end));
  
  private static LapMesg ReconstructLap(List<RecordMesg> records, Dynastream.Fit.DateTime start, Dynastream.Fit.DateTime end)
  {
    double avgSpeed = records.Average(r => r.GetEnhancedSpeed() ?? 0);
    double maxSpeed = records.Max(r => r.GetEnhancedSpeed() ?? 0);
    double avgHr = records.Average(r => r.GetHeartRate() ?? 0);
    double maxHr = records.Max(r => r.GetHeartRate() ?? 0);
    double avgCadence = records.Average(r => r.GetCadence() ?? 0);
    double maxCadence = records.Max(r => r.GetCadence() ?? 0);
    double avgPower = records.Average(r => r.GetPower() ?? 0);
    double maxPower = records.Max(r => r.GetPower() ?? 0);
    uint totalCalories = (records.Last().GetCalories() ?? 0U) - (records.First().GetCalories() ?? 0U);
    double totalDistance = (records.Last().GetDistance() ?? 0) - (records.First().GetDistance() ?? 0);
    
    var lap = new LapMesg();
    lap.SetStartTime(start);
    lap.SetTimestamp(end);
    lap.SetEvent(Event.Lap);
    lap.SetEventType(EventType.Stop);
    lap.SetStartPositionLat(records.First().GetPositionLat());
    lap.SetStartPositionLong(records.First().GetPositionLong());
    lap.SetEndPositionLat(records.Last().GetPositionLat());
    lap.SetEndPositionLong(records.Last().GetPositionLong());
    lap.SetTotalElapsedTime(end.GetTimeStamp() - start.GetTimeStamp());
    lap.SetTotalTimerTime(end.GetTimeStamp() - start.GetTimeStamp());
    lap.SetTotalDistance((float)totalDistance);
    lap.SetTotalCalories((ushort)totalCalories);
    lap.SetEnhancedAvgSpeed((float)avgSpeed);
    lap.SetEnhancedMaxSpeed((float)maxSpeed);
    lap.SetAvgHeartRate((byte)avgHr);
    lap.SetMaxHeartRate((byte)maxHr);
    lap.SetAvgCadence((byte)avgCadence);
    lap.SetMaxCadence((byte)maxCadence);
    lap.SetAvgPower((ushort)avgPower);
    lap.SetMaxPower((ushort)maxPower);
    
    lap.SetLapTrigger(LapTrigger.SessionEnd);

    return lap;
  }
  
  private static LapMesg ReconstructLap(this FitFile source, List<RecordMesg> records) =>
    records.Any() 
      ? ReconstructLap(records, source.GetStartTime(), source.GetEndTime()) 
      : GetFakeLap(source);

  private static SessionMesg ReconstructSession(FitFile source, List<RecordMesg> records)
  {
    if (!records.Any())
    {
      return GetFakeSession(source);
    }

    var start = new Dynastream.Fit.DateTime(source.GetStartTime());
    var end = new Dynastream.Fit.DateTime(source.GetEndTime());

    var session = new SessionMesg();
    session.SetStartTime(start);
    session.SetTimestamp(end);
    session.SetEvent(Event.Lap);
    session.SetEventType(EventType.Stop);
    session.SetStartPositionLat(records.First().GetPositionLat());
    session.SetStartPositionLong(records.First().GetPositionLong());
    session.SetEndPositionLat(records.Last().GetPositionLat());
    session.SetEndPositionLong(records.Last().GetPositionLong());
    session.SetTotalElapsedTime(end.GetTimeStamp() - start.GetTimeStamp());
    session.SetTotalTimerTime(end.GetTimeStamp() - start.GetTimeStamp());
    session.SetTotalDistance(records.Last().GetDistance() - records.First().GetDistance());
    session.SetTrigger(SessionTrigger.ActivityEnd);
    return session;
  }

  private static RecordMesg GetFakeRecord(Dynastream.Fit.DateTime when)
  {
    var rec = new RecordMesg();
    rec.SetTimestamp(when);
    rec.SetPositionLat(0);
    rec.SetPositionLong(0);
    rec.SetDistance(0);
    rec.SetSpeed(0);
    rec.SetHeartRate(0);
    rec.SetCadence(0);
    return rec;
  }

  private static LapMesg GetFakeLap(FitFile source)
  {
    DateTime start = source.GetStartTime();
    DateTime end = source.GetEndTime();

    var lap = new LapMesg();
    lap.SetStartTime(new Dynastream.Fit.DateTime(start));
    lap.SetTimestamp(new Dynastream.Fit.DateTime(end));
    lap.SetEvent(Event.Lap);
    lap.SetEventType(EventType.Stop);
    lap.SetLapTrigger(LapTrigger.SessionEnd);
    return lap;
  }

  private static SessionMesg GetFakeSession(FitFile source)
  {
    DateTime start = source.GetStartTime();
    DateTime end = source.GetEndTime();

    var session = new SessionMesg();
    session.SetStartTime(new Dynastream.Fit.DateTime(start));
    session.SetTimestamp(new Dynastream.Fit.DateTime(end));
    session.SetEvent(Event.Lap);
    session.SetEventType(EventType.Stop);
    session.SetTrigger(SessionTrigger.ActivityEnd);
    return session;
  }

  /// <summary>
  /// Sum the distance between all records
  /// </summary>
  private static double SumDistance(List<RecordMesg> records)
  {
    var penultimate = Math.Max(0, records.Count - 1);

    return Enumerable.Range(1, penultimate)
      .Aggregate<int, double>(0, (current, i) => 
        current + ((records[i].GetDistance() ?? 0) - (records[i - 1].GetDistance() ?? 0)));
  }
  
  /// <summary>
  /// Reconstruct missing Session messages from Lap messages.
  /// 
  /// <para/>
  /// GC import validates that each Sport message in the FIT file has a corresponding Session message.
  /// Garmin devices often fail to record the last Session message and last Activity message.
  /// </summary>
  private static void ReconstructSessions(FitFile? source, FitFile dest)
  {
    var sessions = source.Get<SessionMesg>();
    var sports = source.Get<SportMesg>();

    if (sessions.Count > 0 && sessions.Count == sports.Count)
    {
      return;
    }

    // Reconstruct sports from sessions
    if (sessions.Count > sports.Count)
    {
      foreach (SessionMesg session in sessions)
      {
        var sport = new SportMesg();
        sport.SetSport(session.GetSport());
        sport.SetSubSport(session.GetSubSport());
        dest.Add(sport);
      }

      return;
    }

    // Reconstruct sessions from sports and laps
    var activity = source.Get<ActivityMesg>().First();
    activity.SetNumSessions((ushort)sports.Count);

    var laps = source.Get<LapMesg>();
    laps.Sort((l1, l2) => l1.Start().CompareTo(l2.Start()));

    foreach (int i in Enumerable.Range(0, sports.Count))
    {
      SessionMesg? existing = i < sessions.Count ? sessions[i] : null;
      SportMesg sportMesg = sports[i];
      Sport? sport = sportMesg.GetSport();
      SubSport? subSport = sportMesg.GetSubSport();

      // Sport already has a session
      if (existing is not null && existing.GetSport() == sport) { continue; }

      LapMesg? firstMatchingLap = laps.FirstOrDefault(l => l.GetSport() == sport);
      LapMesg? lastMatchingLap = laps.LastOrDefault(l => l.GetSport() == sport);

      if (firstMatchingLap == null) { continue; }
      if (lastMatchingLap == null) { continue; }

      var sess = new SessionMesg();
      sess.SetStartTime(firstMatchingLap.GetStartTime());
      sess.SetTimestamp(lastMatchingLap.GetTimestamp());
      sess.SetEvent(Event.Lap);
      sess.SetEventType(EventType.Stop);
      sess.SetStartPositionLat(firstMatchingLap.GetStartPositionLat());
      sess.SetStartPositionLong(firstMatchingLap.GetStartPositionLong());
      sess.SetEndPositionLat(lastMatchingLap.GetEndPositionLat());
      sess.SetEndPositionLong(lastMatchingLap.GetEndPositionLong());
      float elapsed = (float)(lastMatchingLap.GetTimestamp().GetDateTime() - firstMatchingLap.GetStartTime().GetDateTime()).TotalSeconds;
      sess.SetTotalElapsedTime(elapsed);
      sess.SetTotalTimerTime(elapsed);

      float? dist = laps.Where(l => l.GetSport() == sport).Sum(l => l.GetTotalDistance());
      sess.SetTotalDistance(dist ?? 0);

      sess.SetSport(sport);
      sess.SetSubSport(subSport);

      dest.Add(sess);
    }
  }

  private static ActivityMesg ReconstructActivity(FitFile source)
  {
    var start = new Dynastream.Fit.DateTime(source.GetStartTime());
    var end = new Dynastream.Fit.DateTime(source.GetEndTime());
    var activity = new ActivityMesg();

    activity.SetTimestamp(start);
    activity.SetTotalTimerTime(end.GetTimeStamp() - start.GetTimeStamp());
    activity.SetNumSessions(1);
    activity.SetType(Activity.Manual);
    activity.SetEvent(Event.Activity);
    activity.SetEventType(EventType.Stop);

    return activity;
  }

  public static void Add(this FitFile dest, Mesg mesg)
  {
    if (!dest.MessagesByDefinition.ContainsKey(mesg.Num))
    {
      dest.MessagesByDefinition[mesg.Num] = new List<Mesg>();
    }
    dest.MessagesByDefinition[mesg.Num].Add(mesg);

    var def = new MesgDefinition(mesg);
    dest.Events.Add(new MesgDefinitionEventArgs(def));
    dest.Events.Add(new MesgEventArgs(mesg));
  }

  /// <summary>
  /// Remove the given message.
  /// </summary>
  public static void Remove(this FitFile f, Mesg mesg)
  {
    f.MessagesByDefinition[mesg.Num].Remove(mesg);
    f.Events.RemoveAll(ea => ea is MesgEventArgs mea && mea.mesg == mesg);
  }

  /// <summary>
  /// Remove all messages and message definitions for the given message type.
  /// </summary>
  public static void RemoveAll<T>(this FitFile fit) where T : Mesg
  {
    var matches = fit.FindAll<T>();
    foreach (var match in matches)
    {
      fit.Remove(match);
    }
  }

  /// <summary>
  /// Remove all messages and message definitions for the given message type.
  /// </summary>
  public static void RemoveAllOfType(this FitFile fit, Type t)
  {
    var matches = fit.FindAll(t);
    foreach (var match in matches)
    {
      fit.Remove(match);
    }
  }

  /// <summary>
  /// Remove all messages for the given message type except the first instance.
  /// </summary>
  public static void RemoveNonfirst(this FitFile fit, Type t)
  {
    var match = fit.FindFirst(t);
    if (match is null) { return; }

    fit.RemoveOthers(match);
  }

  /// <summary>
  /// Remove all messages for the given message type except the given instance.
  /// </summary>
  public static void RemoveOthers(this FitFile fit, Mesg mesg) => 
    fit.Events.RemoveAll(e => e.HasMessageOfType(mesg.GetType()) && e is MesgEventArgs mea && !ReferenceEquals(mea.mesg, mesg));

  /// <summary>
  /// Remove all messages and message definitions for the given message type that occur between the given DateTimes.
  /// </summary>
  public static void RemoveAll(this FitFile fit, Type t, DateTime after = default, DateTime before = default) => 
    fit.Events.RemoveAll(e => HasMessageOfType(e, t) && e is MesgEventArgs mea && mea.mesg.IsBetween(after, before));

  /// <summary>
  /// Return true if the given event contains a message of the given type, e.g. <see cref="ActivityMesg"/>.
  /// </summary>
  private static bool HasMessageOfType(this EventArgs e, Type t) => 
       e is MesgEventArgs mea            && mea.mesg.Num               == MessageFactory.MesgNums[t]
    || e is MesgDefinitionEventArgs mdea && mdea.mesgDef.GlobalMesgNum == MessageFactory.MesgNums[t];
}