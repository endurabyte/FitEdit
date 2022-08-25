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
      var desc = config.Description;
      config = new RetryConfig();
      config.Description = desc;
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
          string description = $"{(string.IsNullOrWhiteSpace(config.Description) ? "" : $"of \"{config.Description}\" ") }";
          Log.Debug($"Retry {description}succeeded in {(DateTime.UtcNow - start).TotalSeconds:##.#}s after {tries} of {config.RetryLimit} tries");
        }

        return true;
      }
    }

    return false;
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
