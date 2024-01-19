using System.Collections.Generic;

namespace FitEdit.Data.Tcx.Entities
{
    public class Activity
    {
        public string Id { get; set; }
        public string Sport { get; set; }
        public List<Lap> Laps { get; set; } = new List<Lap>();
        public Creator Creator { get; set; }
    }
}
