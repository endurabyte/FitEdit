using Dauer.Data.Extensions;
using Dauer.Data.Tcx.Entities;
using Dauer.Model;
using System.Linq;

namespace Dauer.Data.Tcx
{
  public class Mapper
  {
    public Workout Map(TrainingCenterDatabase db)
    {
      return new Workout
      {
        Sequences = db.Activities.Select(activity => (ISequence)new NodeSequence
        {
          When = System.DateTime.Parse(activity.Id),
          Sequences = activity.Laps.Select(lap => (ISequence)new LeafSequence
          {
            When = lap.StartTime,
            Samples = lap.Track.Trackpoints.Select(trackpoint => (ISample)new GpsRunSample
            {
              When = trackpoint.Time,
              Distance = trackpoint.DistanceMeters,
              Speed = trackpoint.Extensions.Speed,
              Cadence = trackpoint.Extensions.RunCadence,
              HeartRate = trackpoint.HeartRateBpm,
              Altitude = trackpoint.AltitudeMeters,
              Latitude = trackpoint.Position?.LatitudeDegrees,
              Longitude = trackpoint.Position?.LongitudeDegrees,

            }).ToList()
          }).ToList()
        }).ToList()
      };
    }

    public TrainingCenterDatabase MapToTcx(Workout workout)
    {
      return new TrainingCenterDatabase
      {
        Activities = workout.Sequences
              .All<NodeSequence>()
              .Select(activitySequence => new Activity
              {
                Laps = activitySequence.Sequences
                      .All<LeafSequence>()
                      .Select(lapSequence => new Lap
                      {
                        Track = new Track
                        {
                          Trackpoints = lapSequence.Samples.Select(sample =>
                                {
                                  var runSample = sample as GpsRunSample;

                                  return new Trackpoint
                                  {
                                    Time = runSample.When,
                                    DistanceMeters = runSample.Distance,
                                    Extensions = new TrackpointExtensions
                                    {
                                      Speed = runSample.Speed,
                                      RunCadence = runSample.Cadence,
                                    },
                                    HeartRateBpm = runSample.HeartRate,
                                    AltitudeMeters = runSample.Altitude,
                                    Position = runSample.HasPosition ? default : new Position
                                    {
                                      LatitudeDegrees = runSample.Latitude ?? default,
                                      LongitudeDegrees = runSample.Longitude ?? default
                                    }
                                  };
                                }).ToList()
                        }
                      }).ToList()
              }).ToList()
      };
    }
  }
}
