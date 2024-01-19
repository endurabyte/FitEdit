using System.Collections.Generic;

namespace FitEdit.Data.Tcx.Entities
{
    public class Track
    {
        public List<Trackpoint> Trackpoints { get; set; } = new List<Trackpoint>();
    }
}
