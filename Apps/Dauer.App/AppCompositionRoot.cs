using Dauer.Infrastructure;
using Dauer.Model;
using Dauer.Model.Workouts;
using Dauer.Services;
using McMaster.Extensions.CommandLineUtils;

namespace Dauer.App
{
  public class AppCompositionRoot
  {
    private readonly ICompositionRoot root_;

    public AppCompositionRoot(ICompositionRoot root)
    {
      root_ = root;
    }

    public CommandLineApplication App
    {
      get
      {
        var service = root_.Get<IFitService>();

        var app = new CommandLineApplication()
        {
          Name = "dauer",
          Description = "Edit FIT files"
        };

        app.HelpOption();
        app.OnExecute(() =>
        {
          Log.Info("Specify a command");
          app.ShowHelp();
          return 1;
        });

        app.Command("copy", config =>
          {
            config.Description = "Copy files";
            CommandArgument src = config.Argument("source", "Source .fit file").IsRequired();
            CommandArgument dest = config.Argument("dest", "Destination .fit file").IsRequired();

            config.OnExecute(() => service.Copy(src.Value, dest.Value));
          });

        app.Command("show", config =>
        {
          config.Description = "Show file contents";
          CommandArgument src = config.Argument("source", "Source .fit file").IsRequired();
          CommandOption<bool> verboseOpt = config.Option<bool>("-v|--verbose", "(optional) Show verbose output", CommandOptionType.NoValue);

          config.OnExecute(() =>
          {
            service.Print(src.Value, verboseOpt.ParsedValue);
          });
        });

        app.Command("dump", config =>
        {
          config.Description = "Show detailed file contents";
          CommandArgument src = config.Argument("source", "Source .fit file").IsRequired();

          config.OnExecute(() => service.PrintAll(src.Value));
        });

        app.Command("speeds", config =>
        {
          config.Description = "Recalculate lap speeds";
          CommandArgument src = config.Argument("source", "Source .fit file").IsRequired();
          CommandArgument dest = config.Argument("dest", "Destination .fit file").IsRequired();
          CommandOption<string> speedsOpt = config.Option<string>("-l|--lap <LAPS>", "Lap speeds", CommandOptionType.MultipleValue);
          CommandOption<string> unitOpt = config.Option<string>("-u|--units <UNIT>", "Lap speed units", CommandOptionType.SingleOrNoValue);

          config.OnExecute(() =>
          {
            List<Speed> speeds = speedsOpt.ParsedValues
              .Select(speed => new Speed(double.Parse(speed), unitOpt.ParsedValue))
              .ToList();

            service.SetLapSpeeds(src.Value, dest.Value, speeds);
          });
        });

        return app;
      }
    }
  }
}