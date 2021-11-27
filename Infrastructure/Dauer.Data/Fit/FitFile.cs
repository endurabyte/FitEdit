using Dynastream.Fit;
using System.Collections.Generic;
using System.Linq;

namespace Dauer.Data.Fit
{
  public class FitFile
  {
    public List<MesgDefinition> MessageDefinitions { get; set; } = new List<MesgDefinition>();
    public List<Mesg> Messages { get; set; } = new List<Mesg>();

    public List<LapMesg> Laps => Get<LapMesg>();
    public List<RecordMesg> Records => Get<RecordMesg>();
    public List<SessionMesg> Sessions => Get<SessionMesg>();

    public List<T> Get<T>() where T : Mesg => Messages
      .Where(message => message.Num == MessageFactory.MesgNums[typeof(T)])
      .Select(message => message as T)
      .ToList();
  }
}