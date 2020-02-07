using System;

namespace BlazorApp.Shared.Dto.Fitness
{
    public class Lap
    {
        public DateTime StartTime { get; set; }
        public double TotalTimeSEconds { get; set; }
        public double DistanceMeters { get; set; }
        public double MaximumSpeed { get; set; }
        public double Calories { get; set; }
        public double AverageHeartRateBmp { get; set; }
        public double MaximumHeartRateBmp { get; set; }
        public double Intensity { get; set; }
        public double TriggerMethod { get; set; }
        public Track Track { get; set; } = new Track();
        public LapExtensions Extensions { get; set; }
    }
}
