using System.Collections.Generic;

namespace BlazorApp.Shared.Dto.Fitness
{
    public class Activity
    {
        public string Id { get; set; }
        public string Sport { get; set; }
        public List<Lap> Laps { get; set; } = new List<Lap>();
        public Creator Creator { get; set; }
    }
}
