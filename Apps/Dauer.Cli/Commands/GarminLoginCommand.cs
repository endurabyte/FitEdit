using Dauer.Adapters.Selenium;
using FundLog.Model.Extensions;
using OpenQA.Selenium;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("login-garmin", Manual = "Login to Garmin")]
public class GarminLoginCommand : ICommand
{
  private readonly GarminLoginStep login_;

  [CommandOption("username", 'u', Description = "Garmin Connect username", IsRequired = true)]
  public string Username { get; set; }

  [CommandOption("password", 'p', Description = "Garmin Connect password", IsRequired = true)]
  public string Password { get; set; }

  public GarminLoginCommand(GarminLoginStep login)
  {
    login_ = login;
  }

  public async ValueTask ExecuteAsync(IConsole console)
  {
    login_.Username = Username;
    login_.Password = Password;

    try
    {
      await login_.Run().AnyContext();
    }
    finally
    {
      login_.Close();
    }
  }
}
