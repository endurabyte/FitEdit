using System;

namespace BlazorApp.Shared.Dto.Fitness
{
    public class Trackpoint
    {
        public DateTime Time { get; set; }
        public double DistanceMeters { get; set; }
        public double HeartRateBpm { get; set; }
        public TrackpointExtensions Extensions { get; set; }
    }
}
