using System.Collections.Generic;

namespace BlazorApp.Shared.Dto.Fitness
{
    public class Track
    {
        public List<Trackpoint> Trackpoints { get; set; } = new List<Trackpoint>();
    }
}
