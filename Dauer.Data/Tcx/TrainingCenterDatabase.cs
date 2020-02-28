using System.Collections.Generic;

namespace Dauer.Data.Tcx
{
    public class TrainingCenterDatabase
    {
        public Author Author { get; set; }
        public List<Activity> Activities { get; set; } = new List<Activity>();
    }
}
