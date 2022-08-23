using Dauer.Adapters.Selenium;
using Dauer.Model;
using FundLog.Model.Extensions;
using OpenQA.Selenium;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("login-garmin", Manual = "Login to Garmin")]
public class GarminLoginCommand : ICommand
{
  private readonly GarminSigninStep login_;

  [CommandOption("username", 'u', Description = "Garmin Connect username", IsRequired = true)]
  public string Username { get; set; }

  [CommandOption("password", 'p', Description = "Garmin Connect password", IsRequired = true)]
  public string Password { get; set; }

  [CommandOption("force", 'f', Description = "Log in even if already logged in", IsRequired = false)]
  public bool Force { get; set; }

  public GarminLoginCommand(GarminSigninStep login)
  {
    login_ = login;
  }

  public async ValueTask ExecuteAsync(IConsole console)
  {
    login_.Username = Username;
    login_.Password = Password;
    login_.Force = Force;

    try
    {
      if (!await login_.Run().AnyContext())
      {
        Log.Error("Failed");
      }
    }
    finally
    {
      login_.Close();
    }
  }
}