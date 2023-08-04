using Dynastream.Fit;

namespace Dauer.Data.Fit
{
  public class FitFile
  {
    /// <summary>
    /// Contains all messages and message definitons.
    /// </summary>
    public List<EventArgs> Events { get; set; } = new();

    /// <summary>
    /// Key: global mesg_num. Value: MesgDefinintion
    /// </summary>
    public Dictionary<int, MesgDefinition> MessageDefinitions { get; set; } = new();

    /// <summary>
    /// Key: global mesg_num. Value: All messages with the mesg_num
    /// </summary>
    public Dictionary<int, List<Mesg>> MessagesByDefinition { get; set; } = new();

    public IReadOnlyList<Mesg> Messages => MessagesByDefinition.SelectMany(kvp => kvp.Value).ToList();

    /// <summary>
    /// Contains all Messages that are a SessionMesg
    /// </summary>
    public List<SessionMesg> Sessions { get; set; } = new();

    /// <summary>
    /// Contains all Messages that are a LapMesg
    /// </summary>
    public List<LapMesg> Laps { get; set; } = new();

    /// <summary>
    /// Contains all Messages that are a RecordMesg
    /// </summary>
    public List<RecordMesg> Records { get; set; } = new();

    public FitFile() { }

    public FitFile(FitFile other)
    {
      Events = other.Events.Select(x => x switch
      {
        _ when x is MesgEventArgs mea => (EventArgs)new MesgEventArgs(mea.mesg),
        _ when x is MesgDefinitionEventArgs mea => new MesgDefinitionEventArgs(mea.mesgDef),
        _ when x is DeveloperFieldDescriptionEventArgs dfdea => new DeveloperFieldDescriptionEventArgs(dfdea.Description),
        _ when x is MesgBroadcastEventArgs mbea => new MesgBroadcastEventArgs(mbea.mesgs.ToList()),
        _ when x is IncomingMesgEventArgs imea => new IncomingMesgEventArgs(imea.mesg),
        _ => null,
      }).Where(x => x is not null).ToList();

      MessageDefinitions = other.MessageDefinitions.ToDictionary(kvp => kvp.Key, kvp => new MesgDefinition(kvp.Value));
      MessagesByDefinition = other.MessagesByDefinition.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Select(MessageFactory.Create).ToList());

      Sessions = other.Sessions.Select(x => new SessionMesg(x)).ToList();
      Laps = other.Laps.Select(x => new LapMesg(x)).ToList();
      Records = other.Records.Select(x => new RecordMesg(x)).ToList();
    }
  }
}