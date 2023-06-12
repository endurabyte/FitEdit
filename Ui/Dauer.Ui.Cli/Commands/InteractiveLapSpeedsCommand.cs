using Dauer.Model.Workouts;
using Dauer.Services;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("ilap", Manual = "Recalculate lap speeds interactively")]
public class InteractiveLapSpeedsCommand : ICommand
{
  private readonly IFitService service_;
  private string defaultUnit_ = "mi/h";

  /// <summary>
  /// Return <c>{filename}-edited.{extension}</c> e.g. myactivity.fit => myactivity-edited.fit
  /// </summary>
  private static string DestinationFor(string source) => $"{Path.GetDirectoryName(source)}\\{Path.GetFileNameWithoutExtension(source)}-edited{Path.GetExtension(source)}";

  public InteractiveLapSpeedsCommand(IFitService service)
  {
    service_ = service;
  }

  public async ValueTask ExecuteAsync(IConsole console)
  {
    await LapSpeeds();
  }

  /// <summary>
  /// Interactively edit a single FIT file
  /// </summary>
  public async Task LapSpeeds()
  {
    Console.WriteLine("Interactively editing a single FIT file.");

    Console.WriteLine("Source file?");
    string source = Console.ReadLine().Trim();
    source = source.Trim('"'); // escape quotes

    Console.WriteLine("Destination file (optional)?");
    string dest = Console.ReadLine().Trim();

    Console.WriteLine("Lap Speeds?");
    string allLaps = Console.ReadLine().Trim();
    List<string> laps = allLaps.Split(' ', ',')
      .Where(s => !string.IsNullOrWhiteSpace(s))
      .ToList();

    Console.WriteLine($"Units? (Default: {defaultUnit_})");
    string units = Console.ReadLine().Trim();

    units = string.IsNullOrWhiteSpace(units) ? defaultUnit_ : units;
    dest = string.IsNullOrWhiteSpace(dest) ? DestinationFor(source) : dest;

    List<Speed> speeds = laps
    .Select(speed => new Speed(double.Parse(speed), units))
    .ToList();

    await service_.SetLapSpeedsAsync(source, dest, speeds);
  }
}