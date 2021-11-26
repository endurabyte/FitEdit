using Dauer.Data.Extensions;
using Dauer.Model;
using System;
using System.Linq;

namespace Dauer.Data.Fit
{
  public class Mapper
  {
    public Workout Map(FitFile fit)
    {
      var sessions = fit.Messages.All<SessionMesg>();
      var laps = fit.Messages.All<LapMesg>();
      var records = fit.Messages.All<RecordMesg>();

      return new Workout
      {
        Sequences = sessions.Select(session =>
        {
          var sessionStart = session.GetStartTime().GetDateTime();
          var sessionDuration = (double)session.GetTotalElapsedTime();
          var sessionEnd = sessionStart + TimeSpan.FromSeconds(sessionDuration);

          return (ISequence)new NodeSequence
          {
            When = sessionStart,
            Sequences = laps
                          .Where(lap =>
                          {
                            var lapStart = lap.GetStartTime().GetDateTime();
                            var lapDuration = (double)lap.GetTotalElapsedTime();
                            var lapEnd = lapStart + TimeSpan.FromSeconds(lapDuration);

                            return lapStart >= sessionStart && lapEnd < sessionEnd;
                          })
                          .Select(lap =>
                          {
                            var lapStart = lap.GetStartTime().GetDateTime();
                            var lapDuration = (double)lap.GetTotalElapsedTime();
                            var lapEnd = lapStart + TimeSpan.FromSeconds(lapDuration);

                            return (ISequence)new LeafSequence
                            {
                              When = lapStart,
                              Samples = records
                                            .Where(record =>
                                            {
                                              var when = record.GetTimestamp().GetDateTime();
                                              return when >= lapStart && when < lapEnd;
                                            })
                                            .Select(record =>
                                            {
                                              return (ISample)new GpsRunSample
                                              {
                                                When = record.GetTimestamp().GetDateTime(),
                                                Distance = record.GetDistance() != default ? (double)record.GetDistance() : default,
                                                Speed = record.GetEnhancedSpeed() != default ? (double)record.GetEnhancedSpeed() : default,
                                                Cadence = record.GetCadence() != default ? (double)record.GetCadence() : default,
                                                HeartRate = record.GetHeartRate() != default ? (double)record.GetHeartRate() : default,
                                                Altitude = record.GetEnhancedAltitude(),
                                                Latitude = record.GetPositionLat() != default ? (double)record.GetPositionLat() * (180.0 / Math.Pow(2.0, 31)) : default,
                                                Longitude = record.GetPositionLong() != default ? (double)record.GetPositionLong() * (180.0 / Math.Pow(2.0, 31)) : default,
                                              };

                                            }).ToList()
                            };
                          }).ToList()
          };
        }).ToList()
      };
    }

    public FitFile MapToFit(Workout workout)
    {
      throw new NotImplementedException();
    }
  }
}
