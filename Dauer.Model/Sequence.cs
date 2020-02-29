using System.Collections.Generic;

namespace Dauer.Model
{
    public interface ISequence
    {
        /// <summary>
        /// Time of the earliest sample of the sequence
        /// </summary>
        System.DateTime When { get; set; }
    }

    public abstract class Sequence : ISequence
    {
        /// <summary>
        /// Time of the earliest sample of the sequence
        /// </summary>
        public System.DateTime When { get; set; }
    }

    /// <summary>
    /// A sequence that contains sequences.
    /// </summary>
    public class NodeSequence : Sequence
    {
        /// <summary>
        /// Maps to FIT Lap, TCX Lap, TCX Track
        /// Recursive because...
        ///     in FIT, a Session has Lap(s), which are both sequences
        ///     in TCX, a Lap has a Track, which are both sequences.
        /// </summary>
        public List<ISequence> Sequences { get; set; }
    }

    /// <summary>
    /// A sequence that contains data.
    /// Bottom of a sequence hierarchy, where data actually lives
    /// </summary>
    public class LeafSequence : Sequence
    {
        public List<ISample> Samples { get; set; }
    }
}
