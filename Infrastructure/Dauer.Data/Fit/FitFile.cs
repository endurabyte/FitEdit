using Dynastream.Fit;
using Dauer.Model.Extensions;

namespace Dauer.Data.Fit
{
  public class FitFile
  {
    public List<MesgDefinition> MessageDefinitions { get; set; } = new List<MesgDefinition>();
    public List<Mesg> Messages { get; set; } = new List<Mesg>();

    public List<LapMesg> Laps => Get<LapMesg>();
    public List<RecordMesg> Records => Get<RecordMesg>().Sorted(sort_);
    public List<SessionMesg> Sessions => Get<SessionMesg>();

    private Comparison<RecordMesg> sort_ = (a, b) => a.GetTimestamp().CompareTo(b.GetTimestamp());

    public List<T> Get<T>() where T : Mesg => Messages
      .Where(message => message.Num == MessageFactory.MesgNums[typeof(T)])
      .Select(message => message as T)
      .ToList();
  }
}