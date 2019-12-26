using System.Collections.Generic;
using fitsharplib.Fit;

namespace fitsharp
{
    public class FitFile
    {
        public List<MesgDefinition> MessageDefinitions { get; set; } = new List<MesgDefinition>();
        public List<Mesg> Messages { get; set; } = new List<Mesg>();
    }
}