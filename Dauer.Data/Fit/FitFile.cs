using Dynastream.Fit;
using System.Collections.Generic;

namespace Dauer.Data.Fit
{
  public class FitFile
  {
    public List<MesgDefinition> MessageDefinitions { get; set; } = new List<MesgDefinition>();
    public List<Mesg> Messages { get; set; } = new List<Mesg>();
  }
}