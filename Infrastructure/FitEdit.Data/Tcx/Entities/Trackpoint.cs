using System;

namespace FitEdit.Data.Tcx.Entities
{
    public class Trackpoint
    {
        public DateTime Time { get; set; }
        /// <summary>
        /// Only for GPS activities but can still be null
        /// </summary>
        public Position Position { get; set; }
        /// <summary>
        /// Only for GPS activities but can still be null
        /// </summary>
        public double? AltitudeMeters { get; set; }
        public double DistanceMeters { get; set; }
        public double HeartRateBpm { get; set; }
        public TrackpointExtensions Extensions { get; set; }
    }
}
