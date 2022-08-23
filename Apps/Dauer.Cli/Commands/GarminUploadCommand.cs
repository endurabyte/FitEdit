using Dauer.Adapters.Selenium;
using Dauer.Model;
using FundLog.Model.Extensions;
using OpenQA.Selenium;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("upload-garmin", Manual = "Upload to Garmin")]
public class GarminUploadCommand : ICommand
{
  private readonly GarminUploadStep upload_;

  [CommandParameter(0, Description = "Garmin FIT file")]
  public string File { get; set; }

  public GarminUploadCommand(GarminUploadStep upload)
  {
    upload_ = upload;
  }

  public async ValueTask ExecuteAsync(IConsole console)
  {
    upload_.File = File;

    try
    {
      if (!await upload_.Run().AnyContext())
      {
        Log.Error("Failed");
      }
    }
    finally
    {
      upload_.Close();
    }
  }
}