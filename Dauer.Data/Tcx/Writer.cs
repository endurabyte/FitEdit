using Dauer.Data.Extensions;
using System.Xml.Linq;

namespace Dauer.Data.Tcx
{
    public static class Writer
    {
        public static string Write(this TrainingCenterDatabase db)
        {
            XNamespace ns = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
            XNamespace ns2 = "http://www.garmin.com/xmlschemas/UserProfile/v2";
            XNamespace ns3 = "http://www.garmin.com/xmlschemas/ActivityExtension/v2";
            XNamespace ns4 = "http://www.garmin.com/xmlschemas/ProfileExtension/v1";
            XNamespace ns5 = "http://www.garmin.com/xmlschemas/ActivityGoals/v1";
            XNamespace xsi = "http://www.w3.org/2001/XMLSchema-instance";
            XNamespace schemaLocation = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd";

            var root = new XElement(ns + "TrainingCenterDatabase",
                new XAttribute(XNamespace.Xmlns + "ns2", ns2),
                new XAttribute(XNamespace.Xmlns + "ns3", ns3),
                new XAttribute(XNamespace.Xmlns + "ns4", ns4),
                new XAttribute(XNamespace.Xmlns + "ns5", ns5),
                new XAttribute(XNamespace.Xmlns + "xsi", xsi),
                new XAttribute(xsi + "schemaLocation", schemaLocation));

            var activities = new XElement(ns + "Activities");

            foreach (var activity in db.Activities)
            {
                var activityElem = new XElement(ns + "Activity",
                    new XAttribute("Sport", activity.Sport)
                );

                activityElem.Add(new XElement(ns + "Id", activity.Id));

                foreach (var lap in activity.Laps)
                {
                    var lapElem = new XElement(ns + "Lap",
                        new XAttribute("StartTime", lap.StartTime.ToTcx())
                    );

                    lapElem.Add(new XElement(ns + "TotalTimeSeconds", lap.TotalTimeSeconds));
                    lapElem.Add(new XElement(ns + "DistanceMeters", lap.DistanceMeters));
                    lapElem.Add(new XElement(ns + "MaximumSpeed", lap.MaximumSpeed));
                    lapElem.Add(new XElement(ns + "Calories", lap.Calories));
                    lapElem.Add(new XElement(ns + "AverageHeartRateBpm", 
                        new XElement(ns + "Value", lap.AverageHeartRateBpm))
                    );
                    lapElem.Add(new XElement(ns + "MaximumHeartRateBpm", 
                        new XElement(ns + "Value", lap.MaximumHeartRateBpm))
                    );
                    lapElem.Add(new XElement(ns + "Intensity", lap.Intensity));
                    lapElem.Add(new XElement(ns + "TriggerMethod", lap.TriggerMethod));

                    var trackElem = new XElement(ns + "Track");

                    foreach (var trackpoint in lap.Track.Trackpoints)
                    {
                        var tpElem = new XElement(ns + "Trackpoint",
                            new XElement(ns + "Time", trackpoint.Time.ToTcx()),
                            new XElement(ns + "DistanceMeters", trackpoint.DistanceMeters),
                            new XElement(ns + "HeartRateBpm", new XElement(ns + "Value", trackpoint.HeartRateBpm)),
                            new XElement(ns + "Extensions",
                                new XElement(ns3 + "TPX",
                                    new XElement(ns3 + "Speed", trackpoint.Extensions.Speed),
                                    new XElement(ns3 + "RunCadence", trackpoint.Extensions.RunCadence)
                                )
                            )
                        );

                        // For GPS workouts
                        if (trackpoint.Position != default)
                        {
                            trackElem.Add(new XElement(ns + "Position",
                                new XElement(ns + "LatitudeDegrees", trackpoint.Position.LatitudeDegrees),
                                new XElement(ns + "LongitudeDegrees", trackpoint.Position.LongitudeDegrees)
                            ));
                        }

                        // For GPS workouts
                        if (trackpoint.AltitudeMeters != default)
                        {
                            trackElem.Add(new XElement(ns + "AltitudeMeters", trackpoint.AltitudeMeters));
                        }

                        trackElem.Add(tpElem);
                    }

                    lapElem.Add(trackElem);

                    lapElem.Add(new XElement(ns + "Extensions",
                        new XElement(ns3 + "LX",
                            new XElement(ns3 + "AvgSpeed", lap.Extensions.AvgSpeed),
                            new XElement(ns3 + "AvgRunCadence", lap.Extensions.AvgRunCadence),
                            new XElement(ns3 + "MaxRunCadence", lap.Extensions.MaxRunCadence)
                        )
                    ));

                    activityElem.Add(lapElem);

                    activityElem.Add(new XElement(ns + "Creator",
                        new XAttribute(xsi + "type", activity.Creator.Type),
                        new XElement(ns + "Name", activity.Creator.Name),
                        new XElement(ns + "UnitId", activity.Creator.UnitId),
                        new XElement(ns + "ProductID", activity.Creator.ProductID),
                        new XElement(ns + "Version",
                            new XElement(ns + "VersionMajor", activity.Creator.VersionMajor),
                            new XElement(ns + "VersionMinor", activity.Creator.VersionMinor),
                            new XElement(ns + "BuildMajor", activity.Creator.BuildMajor),
                            new XElement(ns + "BuildMinor", activity.Creator.BuildMinor)
                        )
                    ));
                }

                activities.Add(activityElem);
            }

            root.Add(activities);

            root.Add(new XElement(ns + "Author",
                new XAttribute(xsi + "type", db.Author.Type),
                new XElement(ns + "Name", db.Author.Name),
                new XElement(ns + "Build",
                    new XElement(ns + "Version",
                        new XElement(ns + "VersionMajor", db.Author.BuildVersionMajor),
                        new XElement(ns + "VersionMinor", db.Author.BuildVersionMinor),
                        new XElement(ns + "BuildMajor", db.Author.BuildBuildMajor),
                        new XElement(ns + "BuildMinor", db.Author.BuildBuildMinor)
                    )
                ),
                new XElement(ns + "LangID", db.Author.LangID),
                new XElement(ns + "PartNumber", db.Author.PartNumber)
            ));

            return root.ToString();
        }
    }
}
