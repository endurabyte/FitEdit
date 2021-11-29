using Dauer.Model.Workouts;
using Dynastream.Fit;

namespace Dauer.Data.Fit
{
  public static class MessageExtensions
  {
    public static LapMesg Apply(this LapMesg lap, Speed speed)
    {
      lap?.SetEnhancedAvgSpeed((float)speed.MetersPerSecond());
      lap?.SetEnhancedMaxSpeed((float)speed.MetersPerSecond());

      return lap;
    }

    public static SessionMesg Apply(this SessionMesg session, Distance distance, Speed speed)
    {
      session?.SetTotalDistance((float)distance.Meters());
      session?.SetEnhancedAvgSpeed((float)(distance.Meters() / session.GetTotalTimerTime()));
      session?.SetEnhancedMaxSpeed((float)speed.MetersPerSecond());

      return session;
    }

    /// <summary>
    /// Find the lap that the record is for by timestamp.
    /// </summary>
    public static LapMesg FindLap(this RecordMesg record, List<LapMesg> laps) => laps.FirstOrDefault(lap =>
    {
      System.DateTime lapStartTime = lap.Start();
      System.DateTime lapEndTime = lap.End();
      System.DateTime recordStartTime = record.Start();

      return lapStartTime <= recordStartTime && recordStartTime <= lapEndTime;
    });

    public static System.DateTime Start(this RecordMesg record) => record.GetTimestamp().GetDateTime();
    public static System.DateTime Start(this LapMesg lap) => lap.GetStartTime().GetDateTime();
    public static System.DateTime End(this LapMesg lap) => lap.GetTimestamp().GetDateTime();
    public static System.DateTime Start(this SessionMesg sess) => sess.GetStartTime().GetDateTime();
    public static System.DateTime End(this SessionMesg sess) => sess.GetTimestamp().GetDateTime();

    public static Comparison<RecordMesg> Sort => (a, b) => a.GetTimestamp().CompareTo(b.GetTimestamp());

  }
}