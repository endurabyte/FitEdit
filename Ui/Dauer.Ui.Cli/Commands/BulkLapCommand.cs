using Dauer.Model;
using Dauer.Model.Extensions;
using Dauer.Model.Workouts;
using Dauer.Services;
using System.IO.Compression;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("bulk", Manual = "Interactively edit multiple FIT files in a directory")]
public class BulkLapCommand : ICommand
{
  private string defaultDirectory_ = @"C:\Users\doug\Downloads";
  private string defaultUnit_ = "mi/h";

  private ConsoleColor GoodColor_ => ConsoleColor.Green;
  private ConsoleColor WarnColor_ => ConsoleColor.Yellow;
  private ConsoleColor ErrorColor_ => ConsoleColor.Red;
  private ConsoleColor DetailColor_ => ConsoleColor.Magenta;
  private ConsoleColor DefaultColor_ => Console.ForegroundColor;

  private readonly IFitService service_;

  public BulkLapCommand(IFitService service)
  {
    service_ = service;
  }

  public async ValueTask ExecuteAsync(IConsole console)
  {
    await BulkLapSpeeds();
  }

  /// <summary>
  /// Interactively edit multiple FIT files in a directory.
  /// </summary>
  public async Task BulkLapSpeeds()
  {
    Console.WriteLine("Interactively editing multiple FIT files in a directory.");

    string directory = GetDirectory();
    string units = GetUnits();

    string tmpDir = @$"{directory}\dauer\{DateTime.Now:yyyy-MM-dd HHmmss}";
    string originalsDir = @$"{tmpDir}\originals\";
    string editsDir = @$"{tmpDir}\edits\";

    Directory.CreateDirectory(originalsDir);
    Directory.CreateDirectory(editsDir);

    ExtractZipFiles(directory, originalsDir, ".fit");
    CopyFitFiles(directory, originalsDir, ".fit");

    List<string> originals = Directory.EnumerateFiles(originalsDir, "*.fit").ToList();

    await ShowUser(originals);

    // Edit each found lap file
    foreach (var fitFile in originals)
    {
      string dest = @$"{editsDir}\{Path.GetFileName(fitFile)}";

      while (!await TrySetLapSpeeds(fitFile, units, dest))
      {
      }
    }
  }

  /// <summary>
  /// Ask the user for a directory
  /// </summary>
  /// <returns></returns>
  private string GetDirectory()
  {
    Console.Write("Directory? (Default: ");
    defaultDirectory_.Write(DetailColor_);
    Console.WriteLine(")");

    string directory = Console.ReadLine().Trim().Trim('"'); // escape quotes

    return string.IsNullOrWhiteSpace(directory)
      ? defaultDirectory_
      : directory;
  }

  private string GetUnits()
  {
    Console.Write("Units? (Default: ");
    defaultUnit_.Write(DetailColor_);
    Console.WriteLine(")");

    string units = Console.ReadLine().Trim();
    if (string.IsNullOrWhiteSpace(units))
    {
      units = defaultUnit_;
    }

    return units;
  }

  /// <summary>
  /// Tell the user what we found.
  /// </summary>
  private async Task ShowUser(List<string> files)
  {
    Console.WriteLine($"Found {files.Count} files");

    foreach (var file in files)
    {
      $"  {Path.GetFileName(file)}".Write(DetailColor_);
      $" ({await service_.OneLineAsync(file)})".WriteLine(DefaultColor_);
    }
  }

  /// <summary>
  /// Copy files with a matching file extension from the given source directory to the given destination directory
  /// </summary>
  private static void CopyFitFiles(string sourceDir, string destDir, string extension)
  {
    var sourceFiles = Directory.EnumerateFiles(sourceDir, extension);

    foreach (var file in sourceFiles)
    {
      File.Copy(file, @$"{destDir}\{Path.GetFileName(file)}");
    }
  }

  /// <summary>
  /// Find all zip files in the given source directory. Extract all files in these with a matching file extension to the given destination directory.
  /// </summary>
  private static void ExtractZipFiles(string sourceDir, string destDir, string extension)
  {
    var zips = Directory.EnumerateFiles(sourceDir, "*.zip")
      .Where(file => ZipFile.OpenRead(file).Entries.Any(entry => entry.Name.EndsWith(extension)));

    foreach (string zip in zips)
    {
      foreach (var entry in ZipFile.OpenRead(zip).Entries)
      {
        File.WriteAllBytes(@$"{destDir}\{entry.Name}", entry.Open().ReadAllBytes());
      }
    }
  }

  private async Task<bool> TrySetLapSpeeds(string fitFile, string units, string dest)
  {
    try
    {
      Console.Write($"\nLap Speeds? (");
      Path.GetFileName(fitFile).Write(DetailColor_);
      Console.WriteLine($")");

      string command = Console.ReadLine().Trim();

      if (command == "!")
      {
        $"Skipping {dest}.".WriteLine(WarnColor_);
        return true;
      }

      List<string> laps = command.Split(' ', ',')
        .Where(s => !string.IsNullOrWhiteSpace(s))
        .ToList();

      List<Speed> speeds = laps
        .Select(speed => new Speed(double.Parse(speed), units))
        .ToList();

      await service_.SetLapSpeedsAsync(fitFile, dest, speeds);

      $"Wrote {dest}.".WriteLine(GoodColor_);
      return true;
    }
    catch (Exception e)
    {
      $"Error: {e.Message}.".WriteLine(ErrorColor_);
      $"Please try again. Type ! to skip.".WriteLine(DefaultColor_);
      return false;
    }
  }
}
