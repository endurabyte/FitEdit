using Dauer.Adapters.Selenium;
using Dauer.Model;
using Dauer.Model.Extensions;
using OpenQA.Selenium;
using Typin;
using Typin.Attributes;
using Typin.Console;

namespace Dauer.Cli.Commands;

[Command("login-finalsurge", Manual = "Login to Final Surge")]
public class FinalSurgeLoginCommand : ICommand
{
  private readonly FinalSurgeLoginStep login_;

  [CommandOption("username", 'u', Description = "Final Surge username", IsRequired = true)]
  public string Username { get; set; }

  [CommandOption("password", 'p', Description = "Final Surge password", IsRequired = true)]
  public string Password { get; set; }

  [CommandOption("force", 'f', Description = "Log in even if already logged in", IsRequired = false)]
  public bool Force { get; set; }

  public FinalSurgeLoginCommand(FinalSurgeLoginStep login)
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
        Log.Error($"{nameof(FinalSurgeLoginCommand)} Failed");
      }
    }
    finally
    {
      login_.Close();
    }
  }
}
