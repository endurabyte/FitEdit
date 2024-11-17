using FitEdit.Model.Workouts;
using Dynastream.Fit;
using Units;
using FitEdit.Adapters.Fit.Extensions;
using System.Diagnostics;
using System.Text;
using FitEdit.Model;

namespace FitEdit.Data.Fit;

public static class MessageExtensions
{
  public static void DebugLog(this DeveloperFieldDescriptionEventArgs e)
  {
    if (!Debugger.IsAttached) { return; }
    Log.Debug(e.MapString());
  }

  public static void DebugLog(this MesgEventArgs s)
  {
    if (!Debugger.IsAttached) { return; }
    Log.Debug(s.MapString());
  }

  public static void DebugLog(this MesgDefinitionEventArgs e)
  {
    if (!Debugger.IsAttached) { return; }
    Log.Debug(e.MapString());
  }

  public static string MapString(this DeveloperFieldDescriptionEventArgs s)
  {
    StringBuilder sb = new();
      
    sb.Append($"{nameof(DeveloperFieldDescription)}")
      .AppendLine($"  ApplicationId {s.Description.ApplicationId}")
      .AppendLine($"  ApplicationVersion {s.Description.ApplicationVersion}")
      .AppendLine($"  FieldDefinitionNumber {s.Description.FieldDefinitionNumber}");
    
    sb.AppendLine($"{s.PrintBytes()}");
      
    return sb.ToString();
  }

  public static string MapString(this MesgEventArgs s)
  {
    StringBuilder sb = new();
    sb.Append($"{nameof(Mesg)} ")
      .AppendLine($"  Name {s.mesg.Name}. ")
      .AppendLine($"  Num {s.mesg.Num}")
      .AppendLine($"  LocalNum {s.mesg.LocalNum}");
      
    sb.AppendLine("  Fields:");
    sb.AppendLine($"    {"FieldNum",-10} {"Name",-20} {"Data"}");

    sb.AppendLine($"    {string.Join("\n    ", s.mesg.Fields.Values.Select(field =>
           $"{field.Num,-10} "
         + $"{field.Name,-20} "
         + $"{string.Join(" ", field.SourceData?.Select(b => 
             $"{b:X2}") ?? Array.Empty<string>())}"))}");
    
    sb.AppendLine(s.PrintBytes());
    return sb.ToString();
  }

public static string MapString(this MesgDefinitionEventArgs s)
{
    StringBuilder sb = new();

    sb.AppendLine($"{nameof(MesgDefinition)}")
      .AppendLine($"  GlobalMesgNum {s.mesgDef.GlobalMesgNum}")
      .AppendLine($"  LocalMesgNum {s.mesgDef.LocalMesgNum}");

    int fieldIndex = 0;
    sb.AppendLine("  Field Definitions");
    sb.AppendLine($"    {"Index",-12} {"Num",-6} {"Size",-6} {"Type",-8} {"TypeName",-15} {"FieldName",-20} (Hex Values)");

    foreach (var fieldDef in s.mesgDef.GetFields())
    {
        var typeName = FitTypes.TypeMap.TryGetValue(fieldDef.Type, out var type) ? type.typeName : "Unknown Type";
        var fieldName = Profile.GetField(s.mesgDef.GlobalMesgNum, fieldDef.Num)?.Name ?? "Unknown Field";
        var hexValues = $"({fieldDef.Num:X2} {fieldDef.Size:X2} {fieldDef.Type:X2})";

        sb.AppendLine($"    {fieldIndex++, -12} {fieldDef.Num, -6} {fieldDef.Size, -6} {fieldDef.Type, -8} {typeName, -15} {fieldName, -20} {hexValues}");
    }

    sb.Append(s.PrintBytes());
    return sb.ToString();
}

  public static string PrintBytes(this EventArgs e) => e switch
  {
    MesgEventArgs me => me.mesg.PrintBytes(),
    MesgDefinitionEventArgs mde => mde.mesgDef.PrintBytes(),
    DeveloperFieldDescriptionEventArgs => "",
    MesgBroadcastEventArgs => "",
    
    _ => throw new ArgumentException($"Unknown event type {e.GetType()}"),
  };

  private static string PrintBytes(this MessageBase msg) => msg switch
  {
    Mesg me => 
       $"  Data"
     + $"\n    Source byte range {me.SourceIndex} - {me.SourceIndex + me.SourceLength - 1}"
     + $"\n    Bytes\n"
     + $"{string.Join("\n", me.SourceData.Select((b, i) => new { b, i })
         .GroupBy(x => x.i / 10) // Group by octets of 10
         .Select(group => "      " + string.Join(" ", group.Select(x => $"{x.b:X2}"))))}",
      
    MesgDefinition mde => 
        $"  Data"
      + $"\n    Source byte range {mde.SourceIndex} - {mde.SourceIndex + mde.SourceLength - 1}"
      + $"\n    Bytes\n" 
      + $"{string.Join("\n", mde.SourceData.Select((b, i) => new { b, i })
        .GroupBy(x => x.i / 10) // Group by octets of 10
        .Select(group => "      " + string.Join(" ", group.Select(x => $"{x.b:X2}"))))}",
    
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