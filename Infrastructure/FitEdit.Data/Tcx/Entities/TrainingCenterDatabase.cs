using System.Collections.Generic;

namespace FitEdit.Data.Tcx.Entities
{
    public class TrainingCenterDatabase
    {
        public Author Author { get; set; }
        public List<Activity> Activities { get; set; } = new List<Activity>();
    }
}
