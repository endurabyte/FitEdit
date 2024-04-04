using FitEdit.Adapters.Fit;
using FitEdit.Model.Workouts;
using Dynastream.Fit;
using Units;

namespace FitEdit.Data.Fit
{
  public static class MessageExtensions
  {
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

    public static Comparison<IInstantOfTime> Sort => (a, b) => a.GetTimestamp().CompareTo(b.GetTimestamp());
  }
}