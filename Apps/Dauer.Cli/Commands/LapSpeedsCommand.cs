using Dauer.Model.Workouts;
using Dauer.Services;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("laps", Manual = "Recalculate lap speeds")]
public class LapSpeedsCommand : ICommand
{
  private readonly IFitService service_;

  [CommandParameter(0, Description = "Lap speeds, chronological")]
  public List<string> LapSpeeds { get; set; }

  [CommandOption("units", 'u', IsRequired = false, Description = "Lap speed units")]
  public string Units { get; set; }

  [CommandOption("source", 's', IsRequired = true, Description = "Source .fit file")]
  public string Source { get; set; }

  [CommandOption("destination", 'd', IsRequired = false, Description = "Destination .fit file")]
  public string Destination { get; set; }

  /// <summary>
  /// Return <c>{filename}-edited.{extension}</c> e.g. myactivity.fit => myactivity-edited.fit
  /// </summary>
  private string AutoDestination_ => Destination ?? $"{Path.GetFileNameWithoutExtension(Source)}-edited{Path.GetExtension(Source)}";

  public LapSpeedsCommand(IFitService service)
  {
    service_ = service;
  }

  public ValueTask ExecuteAsync(IConsole console)
  {
    List<Speed> speeds = LapSpeeds
    .Select(speed => new Speed(double.Parse(speed), Units))
    .ToList();

    service_.SetLapSpeeds(Source, AutoDestination_, speeds);

    return ValueTask.CompletedTask;
  }
}