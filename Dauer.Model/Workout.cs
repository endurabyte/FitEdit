using System.Collections.Generic;

namespace Dauer.Model
{
    public class Workout
    {
        /// <summary>
        /// Maps to FIT Sessions, TCX Activities, GPX Track
        /// </summary>
        public List<Sequence> Sequences { get; set; }
    }
}
