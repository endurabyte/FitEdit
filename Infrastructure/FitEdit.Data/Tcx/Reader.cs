using System;
using System.Linq;
using System.Xml.Linq;
using FitEdit.Data.Extensions;
using FitEdit.Data.Tcx.Entities;

namespace FitEdit.Data.Tcx
{
    public static class Reader
    {
        public static TrainingCenterDatabase Read(string xml)
        {
            var db = new TrainingCenterDatabase();
            var doc = XDocument.Parse(xml);
            var activities = doc.Root.GetElems("Activities");
            Console.WriteLine($"Found {activities.Count()} activitie(s)");

            foreach (var elem in activities)
            {
                db.Activities.Add(ParseActivity(elem));
            }

            db.Author = ParseAuthor(doc.Root.GetElems("Author").First());

            return db;
        }

        private static Author ParseAuthor(XElement elem)
        {
            return new Author
            {
                Type = elem.GetAttributeValue<string>("type"),
                Name = elem.GetValue<string>("Name"),
                BuildVersionMajor = elem.GetValue<string>("VersionMajor"),
                BuildVersionMinor = elem.GetValue<string>("VersionMinor"),
                BuildBuildMajor = elem.GetValue<string>("BuildMajor"),
                BuildBuildMinor = elem.GetValue<string>("BuildMinor"),
                LangID = elem.GetValue<string>("LangID"),
                PartNumber = elem.GetValue<string>("PartNumber")
            };
        }

        private static Activity ParseActivity(XElement elem)
        {
            var activity = new Activity();
            var laps = elem.GetElems("Lap");
            Console.WriteLine($"Found {laps.Count()} lap(s)");

            foreach (var lap in laps)
            {
                activity.Laps.Add(ParseLap(lap));
            }

            activity.Sport = elem.GetAttributeValue<string>("Activity", "Sport");
            activity.Id = elem.GetValue("Id");
            activity.Creator = ParseCreator(elem.GetElems("Creator").First());

            return activity;
        }

        private static Creator ParseCreator(XElement elem)
        {
            return new Creator
            {
                Type = elem.GetAttributeValue<string>("type"),
                Name = elem.GetValue<string>("Name"),
                UnitId = elem.GetValue<string>("UnitId"),
                ProductID = elem.GetValue<string>("ProductID"),
                VersionMajor = elem.GetValue<string>("VersionMajor"),
                VersionMinor = elem.GetValue<string>("VersionMinor"),
                BuildMajor = elem.GetValue<string>("BuildMajor"),
                BuildMinor = elem.GetValue<string>("BuildMinor")
            };
        }

        private static Lap ParseLap(XElement elem)
        {
            var lap = new Lap();

            var trackpoints = elem.GetElems("Trackpoint");
            Console.WriteLine($"Found {trackpoints.Count()} trackpoint(s)");

            foreach (var trackpoint in trackpoints)
            {
                lap.Track.Trackpoints.Add(ParseTrackpoint(trackpoint));
            }

            lap.StartTime = elem.GetAttributeValue<DateTime>("StartTime");
            lap.TotalTimeSeconds = elem.GetValue<double>("TotalTimeSeconds");
            lap.DistanceMeters = elem.GetValue<double>("DistanceMeters");
            lap.MaximumSpeed = elem.GetValue<double>("MaximumSpeed");
            lap.Calories = elem.GetValue<double>("Calories");
            lap.AverageHeartRateBpm = elem.GetValue<double>("AverageHeartRateBpm");
            lap.MaximumHeartRateBpm = elem.GetValue<double>("MaximumHeartRateBpm");
            lap.Intensity = elem.GetValue<string>("Intensity");
            lap.TriggerMethod = elem.GetValue<string>("TriggerMethod");

            lap.Extensions = new LapExtensions
            {
                AvgSpeed = elem.GetValue<double>("AvgSpeed"),
                AvgRunCadence = elem.GetValue<double>("AvgRunCadence"),
                MaxRunCadence = elem.GetValue<double>("MaxRunCadence"),
            };

            return lap;
        }

        private static Trackpoint ParseTrackpoint(XElement elem)
        {
            var tp = new Trackpoint() { Extensions = new TrackpointExtensions() };

            tp.Time = elem.GetValue<DateTime>("Time");
            tp.DistanceMeters = elem.GetValue<double>("DistanceMeters");
            tp.HeartRateBpm = elem.GetValue<double>("HeartRateBpm");
            tp.Extensions.Speed = elem.GetValue<double>("Speed");
            tp.Extensions.RunCadence = elem.GetValue<double>("RunCadence");
            tp.AltitudeMeters = elem.GetValue<double?>("AltitudeMeters");
            tp.Position = ParsePosition(elem.GetElems("Position").FirstOrDefault());

            return tp;
        }

        private static Position ParsePosition(XElement position)
        {
            return position == default
                ? default
                : new Position
                {
                    LatitudeDegrees = position.GetValue<double>("LatitudeDegrees"),
                    LongitudeDegrees = position.GetValue<double>("LongitudeDegrees")
                };
        }
    }
}
