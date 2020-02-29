using System.Collections.Generic;

namespace Dauer.Model
{
    public class Sequence
    {
        public System.DateTime When { get; set; }

        /// <summary>
        /// Maps to FIT Lap, TCX Lap, TCX Track
        /// Recursive because...
        ///     in FIT, a Session has Lap(s), which are both sequences
        ///     in TCX, a Lap has a Track, which are both sequences.
        /// </summary>
        public List<Sequence> Sequences { get; set; }

        public List<Sample> Samples { get; set; }
    }
}
