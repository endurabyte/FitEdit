using Dauer.Model.Extensions;

namespace Dauer.Model;

public static class Resilently
{
  public static async Task<bool> RetryAsync
  (
    Func<Task<bool>> action,
    RetryConfig config = default
  )
  {
    if (config == default)
    {
      string desc = config.Description; 
      config = new RetryConfig
      {
        Description = desc
      };
    }

    DateTime start = DateTime.UtcNow;
    int tries = 0;

    while (tries++ < config.RetryLimit
      && DateTime.UtcNow - start < config.Duration 
      && !config.CancellationToken.IsCancellationRequested)
    {
      if (await DoRetry(action, config).AnyContext())
      {
        if (tries > 1)
        {
          Log(config, start, tries);
        }

        return true;
      }
    }

    return false;
  }

  private static void Log(RetryConfig config, DateTime start, int tries)
  {
    string description = $"{(string.IsNullOrWhiteSpace(config.Description) ? "" : $"of \"{config.Description}\" ")}";
    string maxTries = $"{(config.RetryLimit == int.MaxValue ? "inf" : $"{config.RetryLimit}")}";

    Model.Log.Debug($"Retry {description}succeeded in {(DateTime.UtcNow - start).TotalSeconds:##.#}s after {tries} of {maxTries} tries");
  }

  private static async Task<bool> DoRetry
  (
    Func<Task<bool>> action,
    RetryConfig config
  )
  {
    try
    {
      if (await action().AnyContext())
      {
        return true;
      }

      config.Callback?.Invoke();

      await Task.Delay(config.Interval).AnyContext();
    }
    catch (Exception)
    {
    }

    return false;
  }
}
