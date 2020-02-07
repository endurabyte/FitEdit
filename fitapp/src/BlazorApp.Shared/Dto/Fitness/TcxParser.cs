using BlazorApp.Shared.Extensions;
using System;
using System.Linq;
using System.Xml.Linq;

namespace BlazorApp.Shared.Dto.Fitness
{
    public static class Tcx
    {
        public static TrainingCenterDatabase Parse(string xml)
        {
            var db = new TrainingCenterDatabase();
            var doc = XDocument.Parse(xml);
            var activities = doc.Root.GetElems("Activities");
            Console.WriteLine($"Found {activities.Count()} activitie(s)");

            foreach (var elem in activities)
            {
                db.Activities.Add(ParseActivity(elem));
            }

            return db;
        }

        public static string ToString(this TrainingCenterDatabase activity)
        {
            return "";
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

            return activity;
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

            return lap;
        }

        private static Trackpoint ParseTrackpoint(XElement elem)
        {
            var timeString = elem.GetValue("Time");
            var speedString = elem.GetValue("Speed");
            var distanceString = elem.GetValue("DistanceMeters");
            var hrElem = elem.GetElems("HeartRateBpm").First();
            var hrString = hrElem.GetValue("Value");
            var cadenceString = elem.GetValue("RunCadence");

            var time = DateTime.Parse(timeString);
            var speed = Convert.ToDouble(speedString);
            var distance = Convert.ToDouble(distanceString);
            var hr = Convert.ToDouble(hrString);
            var cadence = Convert.ToDouble(cadenceString);

            return new Trackpoint
            {
                Time = time,
                DistanceMeters = distance,
                HeartRateBpm = hr,
                Extensions = new TrackpointExtensions
                {
                    Speed = speed,
                    RunCadence = cadence
                }
            };
        }
    }
}
