using System;

namespace Dauer.Data.Tcx
{
    public class Lap
    {
        public DateTime StartTime { get; set; }
        public double TotalTimeSeconds { get; set; }
        public double DistanceMeters { get; set; }
        public double MaximumSpeed { get; set; }
        public double Calories { get; set; }
        public double AverageHeartRateBpm { get; set; }
        public double MaximumHeartRateBpm { get; set; }
        public string Intensity { get; set; }
        public string TriggerMethod { get; set; }
        public Track Track { get; set; } = new Track();
        public LapExtensions Extensions { get; set; }
    }
}
