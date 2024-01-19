using FitEdit.Data.Tcx;
using FitEdit.Data.Tcx.Entities;
using System;
using System.Collections.Generic;
using System.IO;

namespace FitEdit.UnitTests.Shared
{
    public static class TcxFixtures
    {
        public static string GetGpsWorkout()
        {
            const string source = @"..\..\..\..\data\devices\forerunner-945\sports\running\"
                + @"generic\2019-12-18\35min-easy-4x20s-strides\garmin-connect\activity.tcx";

            return File.ReadAllText(source);
        }

        public static string GetTreadmillWorkout()
        {
            const string source = @"..\..\..\..\data\devices\forerunner-945\sports\running\"
                + @"treadmill\2019-12-19\90-minutes-easy\garmin-connect\activity.tcx";

            return File.ReadAllText(source);
        }

        public static TrainingCenterDatabase GetTrainingCenterDatabase()
        {
            return new TrainingCenterDatabase
            {
                Activities = new List<Activity>
                {
                    new Activity
                    {
                        Sport = "Running",
                        Id = "2020-02-06T01:10:39.000Z",
                        Laps = new List<Lap>
                        {
                            new Lap
                            {
                                StartTime = DateTime.Parse("2020-02-06T01:10:39.000Z"),
                                TotalTimeSeconds = 322.0,
                                DistanceMeters = 1000.0,
                                MaximumSpeed = 3.759999990463257,
                                Calories = 54,
                                AverageHeartRateBpm = 136,
                                MaximumHeartRateBpm = 149,
                                Intensity = "Active",
                                TriggerMethod = "Manual",

                                Track = new Track
                                {
                                    Trackpoints = new List<Trackpoint>
                                    {
                                        new Trackpoint
                                        {
                                            Time = DateTime.Parse("2020-02-06T01:10:39.000Z"),

                                            Position = new Position
                                            {
                                                LatitudeDegrees = 35.79396564513445,
                                                LongitudeDegrees = -83.98982640355825,
                                            },
                                            AltitudeMeters = 275.79998779296875,

                                            DistanceMeters = 0.0,
                                            HeartRateBpm = 72,
                                            Extensions = new TrackpointExtensions
                                            {
                                                Speed = 0.3919999897480011,
                                                RunCadence = 44
                                            }
                                        }
                                    }
                                },

                                Extensions = new LapExtensions
                                {
                                    AvgSpeed = 3.4514179326061356,
                                    AvgRunCadence = 91,
                                    MaxRunCadence = 94
                                }
                            }
                        },

                        Creator = new Creator
                        {
                            Type = "Device_t",
                            Name = "Forerunner 945",
                            UnitId = "3998947172",
                            ProductID = "3113",
                            VersionMajor = "4",
                            VersionMinor = "0",
                            BuildMajor = "0",
                            BuildMinor = "0"
                        }
                    }
                },

                Author = new Author
                {
                    Type = "Application_t",
                    Name = "FitEdit",
                    BuildVersionMajor = "0",
                    BuildVersionMinor = "0",
                    BuildBuildMajor = "0",
                    BuildBuildMinor = "0",
                    LangID = "en",
                    PartNumber = "006-D2449-00"
                }
            };
        }
    }
}