using FitEdit.Model.Workouts;
using Dynastream.Fit;
using Units;
using FitEdit.Adapters.Fit.Extensions;
using System.Diagnostics;
using FitEdit.Model;

namespace FitEdit.Data.Fit
{
  public static class MessageExtensions
  {
    public static void DebugLog(this DeveloperFieldDescriptionEventArgs s)
    {
      if (!Debugger.IsAttached) { return; }

      Log.Debug($"|--- {nameof(DeveloperFieldDescription)}. (ApplicationId, ApplicationVersion, FieldDefinitionNumber) = ({s.Description.ApplicationId}, {s.Description.ApplicationVersion}, {s.Description.FieldDefinitionNumber}");
      Log.Debug(s.PrintBytes());
    }

    public static void DebugLog(this MesgEventArgs s)
    {
      if (!Debugger.IsAttached) { return; }

      Log.Debug($"|--- {nameof(Mesg)} \'{s.mesg.Name}\'. (Num, LocalNum) = ({s.mesg.Num}, {s.mesg.LocalNum}).");
      Log.Debug($"  Fields:");
      Log.Debug($"    FieldNum\t Name\t Data");
      Log.Debug($"    {string.Join("\n    ", s.mesg.Fields.Values
                        .Select(field => $"{field.Num}\t " +
                                         $"\'{field.Name}\'\t " +
                                         $"{string.Join(" ", field.SourceData?.Select(b => $"{b:X2}") ?? new List<string>())}"))}"
      );

      Log.Debug(s.PrintBytes());
    }

    public static void DebugLog(this MesgDefinitionEventArgs s)
    {
      if (!Debugger.IsAttached) { return; }

      Log.Debug($"|--- {nameof(MesgDefinition)}. (GlobalMesgNum, LocalMesgNum) = ({s.mesgDef.GlobalMesgNum}, {s.mesgDef.LocalMesgNum}).");

      int fieldIndex = 0;
      Log.Debug($"  Field Definitions");
      Log.Debug($"    FieldIndex\t Num\t Size\t Type\t TypeName\t FieldName\t (Hex Values)");
      Log.Debug($"    {string.Join("\n    ", s.mesgDef.GetFields()
          .Select(fieldDef => $"{fieldIndex++}\t " +
                              $"{fieldDef.Num}\t " +
                              $"{fieldDef.Size}\t " +
                              $"{fieldDef.Type}\t " +
                              $"{(FitTypes.TypeMap.TryGetValue(fieldDef.Type, out var type) ? type.typeName : "Unknown Type")}\t " +
                              $"\'{Profile.GetField(s.mesgDef.GlobalMesgNum, fieldDef.Num)?.Name ?? "Unknown Field"}\'\t " +
                              $"({fieldDef.Num:X2} {fieldDef.Size:X2} {fieldDef.Type:X2})"))}");

      Log.Debug(s.PrintBytes());
    }

    public static string PrintBytes(this EventArgs e) => e switch
    {
      MesgEventArgs me => me.mesg.PrintBytes(),
      MesgDefinitionEventArgs mde => mde.mesgDef.PrintBytes(),
      DeveloperFieldDescriptionEventArgs => "",
      MesgBroadcastEventArgs => "",
      _ => throw new ArgumentException($"Unknown event type {e.GetType()}"),
    };

    public static string PrintBytes(this MessageBase msg) => msg switch
    {
      Mesg me => $"Message data: Source index {me.SourceIndex}-{me.SourceIndex + me.SourceLength - 1} Bytes: {string.Join(" ", me.SourceData.Select(b => $"{b:X2}"))}",
      MesgDefinition mde => $"Definition data: Source index {mde.SourceIndex}-{mde.SourceIndex + mde.SourceLength - 1} Bytes: {string.Join(" ", mde.SourceData.Select(b => $"{b:X2}"))}",
      _ => throw new ArgumentException($"Unknown message {msg.GetType()}"),
    };

    public static LapMesg Apply(this LapMesg lap, Speed speed)
    {
      var metersPerSecond = (float)speed.Convert(Unit.MetersPerSecond).Value;

      lap?.SetEnhancedAvgSpeed(metersPerSecond);
      lap?.SetEnhancedMaxSpeed(metersPerSecond);

      return lap;
    }

    public static SessionMesg Apply(this SessionMesg session, Distance distance, Speed speed)
    {
      var metersPerSecond = (float)speed.Convert(Unit.MetersPerSecond).Value;

      session?.SetTotalDistance((float)distance.Meters());
      session?.SetEnhancedAvgSpeed((float)(distance.Meters() / session.GetTotalTimerTime()));
      session?.SetEnhancedMaxSpeed(metersPerSecond);

      return session;
    }

    /// <summary>
    /// Find the lap that the record is for by timestamp.
    /// </summary>
    public static LapMesg FindLap(this RecordMesg record, List<LapMesg> laps) => laps.FirstOrDefault(lap =>
    {
      System.DateTime lapStartTime = lap.Start();
      System.DateTime lapEndTime = lap.End();
      System.DateTime recordStartTime = record.InstantOfTime();

      return lapStartTime <= recordStartTime && recordStartTime <= lapEndTime;
    });

    public static System.DateTime Start(this IDurationOfTime mesg) => mesg.GetStartTime()?.GetDateTime() ?? System.DateTime.MinValue;
    public static System.DateTime End(this IDurationOfTime hts) => hts.GetTimestamp()?.GetDateTime() ?? System.DateTime.MinValue;
    public static System.DateTime InstantOfTime(this IInstantOfTime hts) => hts.GetTimestamp()?.GetDateTime() ?? System.DateTime.MinValue;

    public static Comparison<IInstantOfTime> SortByTimestamp => (a, b) => a.GetTimestamp().CompareTo(b.GetTimestamp());
    public static Comparison<IDurationOfTime> SortByStartTime => (a, b) => a.GetStartTime().CompareTo(b.GetStartTime());
  }
}