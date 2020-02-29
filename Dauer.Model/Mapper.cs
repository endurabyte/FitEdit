using Dauer.Data.Extensions;
using Dauer.Data.Fit;
using Dauer.Data.Tcx;
using System;
using System.Linq;

namespace Dauer.Model
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

                    return new Sequence
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

                                return new Sequence
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
                                            return (Sample)new GpsRunSample()
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

        public Workout Map(TrainingCenterDatabase db)
        {
            return new Workout
            {
                Sequences = db.Activities.Select(activity => new Sequence
                {
                    When = System.DateTime.Parse(activity.Id),
                    Sequences = activity.Laps.Select(lap => new Sequence
                    {
                        When = lap.StartTime,
                        Samples = lap.Track.Trackpoints.Select(trackpoint => (Sample)new GpsRunSample
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
                Activities = workout.Sequences.Select(activitySequence => new Data.Tcx.Activity
                {
                    Laps = activitySequence.Sequences.Select(lapSequence => new Lap 
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
