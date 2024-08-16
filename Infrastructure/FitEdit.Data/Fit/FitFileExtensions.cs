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
  /// Return the timestamp from the first FileID message. Return default if there is no such message.
  /// </summary>
  public static DateTime GetStartTime(this FitFile f) => f.Messages.FirstOrDefault(mesg => mesg is FileIdMesg) is FileIdMesg mesg
    ? mesg.GetTimeCreated().GetDateTime()
    : default;

  public static List<T> Get<T>(this FitFile f) where T : Mesg => f.Messages
    .Where(message => message.Num == MessageFactory.MesgNums[typeof(T)])
    .Select(message => message as T)
    .ToList();

  /// <summary>
  /// Return only the <see cref="T"/>s which occur between the given times.
  /// Used for e.g. Laps, Sessions, and other messages hich span a duration of time and don't only occupy an instant in time.
  /// </summary>
  public static IEnumerable<T> DurationBetween<T>(this IEnumerable<T> ts, DateTime after = default, DateTime before = default)
    where T : IDurationOfTime => ts.Where(t => t.Start() > after && t.End() < before);

  /// <summary>
  /// Return only the <see cref="T"/>s which occur in the given <see cref="System.DateTime"/> range.
  /// Used for e.g. Records and other messages which don't span a duration of time and instead only occupy an instant in time.
  /// </summary>
  public static IEnumerable<T> InstantBetween<T>(this IEnumerable<T> ts, DateTime after = default, DateTime before = default)
    where T : IInstantOfTime => ts.Where(t => t.InstantOfTime() > after && t.InstantOfTime() < before);

  /// <summary>
  /// Return only the <see cref="T"/>s which occur in the given <see cref="Dynastream.Fit.DateTime"/> range.
  /// Used for e.g. Records and other messages which don't span a duration of time and instead only occupy an instant in time.
  /// </summary>
  public static IEnumerable<T> InstantBetween<T>(this IEnumerable<T> ts, Dynastream.Fit.DateTime after = default, Dynastream.Fit.DateTime before = default)
    where T : IInstantOfTime => ts.Where(t => t.GetTimestamp().CompareTo(after) >= 0 && t.GetTimestamp().CompareTo(before) < 0);

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

      var lapRecords = records.InstantBetween(lap.Start(), lap.End()).ToList();

      print($"      {lapRecords.Count} {(lapRecords.Count == 1 ? "record" : "records")}");

      if (!showRecords)
      {
        continue;
      }

      foreach (var rec in lapRecords)
      {
        var speed = new Speed { Unit = Unit.MetersPerSecond, Value = (double)rec.GetEnhancedSpeed() };
        var distance = new Distance { Unit = Unit.Meter, Value = (double)rec.GetDistance() };

        print($"        At {rec.InstantOfTime():HH:mm:ss}: {distance.Miles():0.##} mi, {speed.Convert(Unit.MinutesPerMile)}, {rec.GetHeartRate()} bpm, {(rec.GetCadence() + rec.GetFractionalCadence()) * 2} cad");
        //print($"        At {rec.Start():HH:mm:ss}: {rec.GetDistance():0.##} m, {rec.GetEnhancedSpeed():0.##} m/s, {rec.GetHeartRate()} bpm, {(rec.GetCadence() + rec.GetFractionalCadence()) * 2} cad");
      }
    }

    return f;
  }

  /// <summary>
  /// Pretty-print everything in the given FIT file.
  /// </summary>
  public static string PrintAll(this FitFile f) => JsonSerializer.Serialize(f, new JsonSerializerOptions { WriteIndented = true });

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
    DateTime end = source.Records.Last().InstantOfTime();

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

  private static FitFile? ExtractRecords(this FitFile? source, DateTime start, DateTime end)
  {
    List<RecordMesg> records = source
      .Get<RecordMesg>()
      .InstantBetween(start, end)
      .ToList()
      .Sorted(MessageExtensions.Sort);

    return RepairAdditively(source, records);
  }

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
  /// This method preserves more information than <see cref="RepairAdditively(FitFile?)(FitFile?)"/>,
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
      .Sorted(MessageExtensions.Sort);

    return RepairAdditively(source, records);
  }

  private static FitFile? RepairAdditively(this FitFile? source, List<RecordMesg> records)
  {
    if (!records.Any()) { return null; }
    var start = records.First().GetTimestamp();
    var end = records.Last().GetTimestamp();

    var fileId = source.Get<FileIdMesg>().FirstOrDefault();
    fileId!.SetTimeCreated(start);

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
    SessionMesg session = ReconstructSession(records2);
    LapMesg lap = ReconstructLap(records2);
    ActivityMesg activity = ReconstructActivity(records2);

    List<Mesg> mesgs = new();

    if (fileId != null) { mesgs.Add(fileId); }
    if (fileCreator != null) { mesgs.Add(fileCreator); }
    if (startEvent != null) { mesgs.Add(startEvent); }
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
      .Sorted(MessageExtensions.Sort);

    var sport = source.Get<SportMesg>().FirstOrDefault();
    var lap = source.Get<LapMesg>().FirstOrDefault();
    var session = dest.Get<SessionMesg>().FirstOrDefault();
    var activity = source.Get<ActivityMesg>().FirstOrDefault();

    if (!records.Any())
    {
      var events = source.Get<EventMesg>();
      var start = events.First().GetTimestamp();
      var end = events.Last().GetTimestamp();

      records.Add(GetFakeRecord(when: start));
      records.Add(GetFakeRecord(when: end));
      lap = GetFakeLap(source);
      session = GetFakeSession(source);
      activity = GetFakeActivity(source);

      records.ForEach(dest.Add);
      dest.Add(lap);
      dest.Add(session);
      dest.Add(activity);
    }

    if (lap is null)
    {
      lap = ReconstructLap(records);

      if (sport != null)
      {
        lap.SetSport(sport.GetSport());
        lap.SetSubSport(sport.GetSubSport());
      }

      dest.Add(lap);
    }

    // If there are still no session messages, fallback to creating one from records
    if (session is null)
    {
      session = ReconstructSession(records);
      if (sport != null)
      {
        session.SetSport(sport.GetSport());
        session.SetSubSport(sport.GetSubSport());
      }

      dest.Add(session);
    }

    if (activity is null)
    {
      activity = ReconstructActivity(records);
      dest.Add(activity);
    }

    dest.ForwardfillEvents();
    return dest;
  }

  private static LapMesg ReconstructLap(List<RecordMesg> records)
  {
    var start = records.First().GetTimestamp();
    var end = records.Last().GetTimestamp();

    float totalDistance = SumDistance(records);

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
    lap.SetTotalDistance(totalDistance);
    lap.SetLapTrigger(LapTrigger.SessionEnd);

    return lap;
  }

  private static SessionMesg ReconstructSession(List<RecordMesg> records)
  {
    var start = records.First().GetTimestamp();
    var end = records.Last().GetTimestamp();

    float totalDistance = SumDistance(records);

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
    session.SetTotalDistance(totalDistance);
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
    var events = source.Get<EventMesg>();
    var start = events.First().GetTimestamp();
    var end = events.Last().GetTimestamp();

    var session = new LapMesg();
    session.SetStartTime(start);
    session.SetTimestamp(end);
    session.SetEvent(Event.Lap);
    session.SetEventType(EventType.Stop);
    session.SetLapTrigger(LapTrigger.SessionEnd);
    return session;
  }

  private static SessionMesg GetFakeSession(FitFile source)
  {
    var events = source.Get<EventMesg>();
    var start = events.First().GetTimestamp();
    var end = events.Last().GetTimestamp();

    var session = new SessionMesg();
    session.SetStartTime(start);
    session.SetTimestamp(end);
    session.SetEvent(Event.Lap);
    session.SetEventType(EventType.Stop);
    session.SetTrigger(SessionTrigger.ActivityEnd);
    return session;
  }

  private static ActivityMesg GetFakeActivity(FitFile source)
  {
    var events = source.Get<EventMesg>();
    var start = events.First().GetTimestamp();
    var end = events.Last().GetTimestamp();

    var activity = new ActivityMesg();
    activity.SetTimestamp(start);
    activity.SetTotalTimerTime(end.GetTimeStamp() - start.GetTimeStamp());
    activity.SetNumSessions(1);
    activity.SetType(Activity.Manual);
    activity.SetEvent(Event.Activity);
    activity.SetEventType(EventType.Stop);
    return activity;
  }

  /// <summary>
  /// Sum the distance between all records
  /// </summary>
  private static float SumDistance(List<RecordMesg> records)
  {
    double? sum = 0;

    foreach (int i in Enumerable.Range(1, records.Count - 1))
    {
      sum += records[i].GetDistance() - (double?)records[i - 1].GetDistance();
    }

    return (float)(sum ?? 0);
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

    if (sessions.Count >= sports.Count)
    {
      foreach (SessionMesg session in sessions)
      {
        dest.Add(session);
      }
      return;
    }

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

  private static ActivityMesg ReconstructActivity(List<RecordMesg> records)
  {
    var start = records.First().GetTimestamp();
    var end = records.Last().GetTimestamp();
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
  public static void RemoveAll(this FitFile fit, Type t)
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

  /// <summary>
  /// Return true if the given message occurs between the given DateTimes.
  /// If either DateTime is not specified, it is not considered.
  /// </summary>
  private static bool IsBetween(this Mesg mesg, DateTime after = default, DateTime before = default) => mesg switch
  {
    _ when mesg is IDurationOfTime dur
      && (after == default || dur.GetStartTime().GetDateTime() > after)
      && (before == default || dur.GetTimestamp().GetDateTime() <= before) => true,

    _ when mesg is IInstantOfTime inst
      && (after == default || inst.GetTimestamp().GetDateTime() > after)
      && (before == default || inst.GetTimestamp().GetDateTime() <= before) => true,

    _ => false,
  };
}
