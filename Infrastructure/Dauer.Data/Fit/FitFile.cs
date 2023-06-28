using Dynastream.Fit;

namespace Dauer.Data.Fit
{
  public class FitFile
  {
    public List<MesgDefinition> MessageDefinitions { get; set; } = new();
    public List<Mesg> Messages { get; set; } = new();
    public List<EventArgs> Events { get; set; } = new();

    public List<SessionMesg> Sessions { get; set; } = new();
    public List<LapMesg> Laps { get; set; } = new();
    public List<RecordMesg> Records { get; set; } = new();

    public FitFile() { }

    public FitFile(FitFile other)
    {
      MessageDefinitions = other.MessageDefinitions.Select(x => new MesgDefinition(x)).ToList();
      Messages = other.Messages.Select(MessageFactory.Create).ToList();
      Events = other.Events.Select(x => x switch
      {
        _ when x is MesgEventArgs mea => (EventArgs)new MesgEventArgs(mea.mesg),
        _ when x is MesgDefinitionEventArgs mea => new MesgDefinitionEventArgs(mea.mesgDef),
        _ => null,
      }).Where(x => x is not null).ToList();

      Sessions = other.Sessions.Select(x => new SessionMesg(x)).ToList();
      Laps = other.Laps.Select(x => new LapMesg(x)).ToList();
      Records = other.Records.Select(x => new RecordMesg(x)).ToList();
    }
  }
}