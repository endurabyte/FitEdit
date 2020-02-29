using System.Collections.Generic;

namespace Dauer.Data.Tcx.Entities
{
    public class Track
    {
        public List<Trackpoint> Trackpoints { get; set; } = new List<Trackpoint>();
    }
}
