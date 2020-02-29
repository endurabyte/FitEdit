using Dauer.Data.Extensions;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;

namespace Dauer.Data.Tcx
{
    public static class Writer
    {
        private static readonly XNamespace Ns = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2";
        private static readonly XNamespace Ns2 = "http://www.garmin.com/xmlschemas/UserProfile/v2";
        private static readonly XNamespace Ns3 = "http://www.garmin.com/xmlschemas/ActivityExtension/v2";
        private static readonly XNamespace Ns4 = "http://www.garmin.com/xmlschemas/ProfileExtension/v1";
        private static readonly XNamespace Ns5 = "http://www.garmin.com/xmlschemas/ActivityGoals/v1";
        private static readonly XNamespace Xsi = "http://www.w3.org/2001/XMLSchema-instance";
        private static readonly XNamespace SchemaLocation = "http://www.garmin.com/xmlschemas/TrainingCenterDatabase/v2 http://www.garmin.com/xmlschemas/TrainingCenterDatabasev2.xsd";

        private static XName InNs(this string name, XNamespace ns = null)
        {
            return (ns ?? Ns) + name;
        }

        public static string Write(this TrainingCenterDatabase db) => db.ToTcx().ToString();

        private static XElement ToTcx(this TrainingCenterDatabase db) => 
            new XElement("TrainingCenterDatabase".InNs(),
                 new XAttribute("ns2".InNs(XNamespace.Xmlns), Ns2),
                 new XAttribute("ns3".InNs(XNamespace.Xmlns), Ns3),
                 new XAttribute("ns4".InNs(XNamespace.Xmlns), Ns4),
                 new XAttribute("ns5".InNs(XNamespace.Xmlns), Ns5),
                 new XAttribute("xsi".InNs(XNamespace.Xmlns), Xsi),
                 new XAttribute("schemaLocation".InNs(Xsi), SchemaLocation),
                 db.Activities.ToTcx(),
                 db.Author.ToTcx());

        private static XElement ToTcx(this List<Activity> activities) =>
            new XElement("Activities".InNs(), activities.Select(ToTcx));

        private static XElement ToTcx(this Activity activity) => new XElement("Activity".InNs(),
                new XAttribute("Sport", activity.Sport),
                new XElement("Id".InNs(), activity.Id),
                activity.Laps.Select(ToTcx),
                activity.Creator.ToTcx()
            );

        private static XElement ToTcx(this Lap lap) => new XElement("Lap".InNs(),
                new XAttribute("StartTime", lap.StartTime.ToTcx()),
                new XElement("TotalTimeSeconds".InNs(), lap.TotalTimeSeconds),
                new XElement("DistanceMeters".InNs(), lap.DistanceMeters),
                new XElement("MaximumSpeed".InNs(), lap.MaximumSpeed),
                new XElement("Calories".InNs(), lap.Calories),
                new XElement("AverageHeartRateBpm".InNs(),
                    new XElement("Value".InNs(), lap.AverageHeartRateBpm)
                ),
                new XElement("MaximumHeartRateBpm".InNs(),
                    new XElement("Value".InNs(), lap.MaximumHeartRateBpm)
                ),
                new XElement("Intensity".InNs(), lap.Intensity),
                new XElement("TriggerMethod".InNs(), lap.TriggerMethod),
                lap.Track.ToTcx(),
                lap.Extensions.ToTcx()
            );

        private static XElement ToTcx(this LapExtensions lapExtensions) => new XElement("Extensions".InNs(),
                new XElement("LX".InNs(Ns3),
                    new XElement("AvgSpeed".InNs(Ns3), lapExtensions.AvgSpeed),
                    new XElement("AvgRunCadence".InNs(Ns3), lapExtensions.AvgRunCadence),
                    new XElement("MaxRunCadence".InNs(Ns3), lapExtensions.MaxRunCadence)
                )
            );

        private static XElement ToTcx(this Track track) => 
            new XElement("Track".InNs(), track.Trackpoints.Select(trackpoint =>
            {
                var tpElem = new XElement("Trackpoint".InNs(),
                    new XElement("Time".InNs(), trackpoint.Time.ToTcx()),
                    new XElement("DistanceMeters".InNs(), trackpoint.DistanceMeters),
                    new XElement("HeartRateBpm".InNs(), new XElement("Value".InNs(), trackpoint.HeartRateBpm)),
                    new XElement("Extensions".InNs(),
                        new XElement("TPX".InNs(Ns3),
                            new XElement("Speed".InNs(Ns3), trackpoint.Extensions.Speed),
                            new XElement("RunCadence".InNs(Ns3), trackpoint.Extensions.RunCadence)
                        )
                    )
                );

                // For GPS workouts
                if (trackpoint.Position != default)
                {
                    tpElem.Add(new XElement("Position".InNs(),
                        new XElement("LatitudeDegrees".InNs(), trackpoint.Position.LatitudeDegrees),
                        new XElement("LongitudeDegrees".InNs(), trackpoint.Position.LongitudeDegrees)
                    ));
                }

                // For GPS workouts
                if (trackpoint.AltitudeMeters != default)
                {
                    tpElem.Add(new XElement("AltitudeMeters".InNs(), trackpoint.AltitudeMeters));
                }

                return tpElem;
            }));

        private static XElement ToTcx(this Creator creator) => new XElement("Creator".InNs(),
                new XAttribute("type".InNs(Xsi), creator.Type),
                new XElement("Name".InNs(), creator.Name),
                new XElement("UnitId".InNs(), creator.UnitId),
                new XElement("ProductID".InNs(), creator.ProductID),
                new XElement("Version".InNs(),
                    new XElement("VersionMajor".InNs(), creator.VersionMajor),
                    new XElement("VersionMinor".InNs(), creator.VersionMinor),
                    new XElement("BuildMajor".InNs(), creator.BuildMajor),
                    new XElement("BuildMinor".InNs(), creator.BuildMinor)
                )
            );

        private static XElement ToTcx(this Author author) => new XElement("Author".InNs(),
                new XAttribute("type".InNs(Xsi), author.Type),
                new XElement("Name".InNs(), author.Name),
                new XElement("Build".InNs(),
                    new XElement("Version".InNs(),
                        new XElement("VersionMajor".InNs(), author.BuildVersionMajor),
                        new XElement("VersionMinor".InNs(), author.BuildVersionMinor),
                        new XElement("BuildMajor".InNs(), author.BuildBuildMajor),
                        new XElement("BuildMinor".InNs(), author.BuildBuildMinor)
                    )
                ),
                new XElement("LangID".InNs(), author.LangID),
                new XElement("PartNumber".InNs(), author.PartNumber)
            );
    }
}
