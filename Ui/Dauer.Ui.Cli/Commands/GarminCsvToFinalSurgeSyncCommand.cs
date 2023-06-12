using Dauer.Adapters.Selenium;
using Dauer.Model;
using Dauer.Model.Extensions;
using OpenQA.Selenium;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("garmin-csv-to-finalsurge", Manual = "Load a Garmin Connect CSV file and sync it to FinalSurge")]
public class GarminCsvToFinalSurgeSyncCommand : ICommand
{
  private readonly FinalSurgeBulkEditStep edit_;

  [CommandOption("file", 'f', Description = "Garmin Connect CSV file", IsRequired = true)]
  public string File { get; set; }

  public GarminCsvToFinalSurgeSyncCommand(FinalSurgeBulkEditStep edit)
  {
    edit_ = edit;
  }

  public async ValueTask ExecuteAsync(IConsole console)
  {
    string[] lines = System.IO.File.ReadAllText(File).Split('\n');
    string[][] rows = lines
      .Where(line => !string.IsNullOrWhiteSpace(line))
      .Select(line => line.Split(',')).ToArray();

    List<DateTime> dates = new();
    List<string> workoutNames = new();

    foreach (var row in rows.Skip(1)) // Skip header
    {
      if (row.Length < 4)
      {
        continue;
      }

      if (!DateTimeFactory.TryParseSafe($"{row[1]}", out DateTime dt, "yyyy-MM-dd HH:mm:ss"))
      {
        Log.Error($"Bad date {row[1]}");
        continue;
      }

      dates.Add(dt);
      workoutNames.Add(row[3].Trim('\"')); // Dequote
    }

    edit_.Dates = dates;
    edit_.WorkoutNames = workoutNames;
    edit_.Descriptions = Enumerable.Repeat("", workoutNames.Count).ToList();

    try
    {
      if (!await edit_.Run().AnyContext())
      {
        Log.Error($"{nameof(GarminCsvToFinalSurgeSyncCommand)} Failed");
      }
    }
    finally
    {
      edit_.Close();
    }
  }
}
