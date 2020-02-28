using System;

namespace Dauer.Data.Tcx
{
    public class Trackpoint
    {
        public DateTime Time { get; set; }
        public double DistanceMeters { get; set; }
        public double HeartRateBpm { get; set; }
        public TrackpointExtensions Extensions { get; set; }
    }
}
