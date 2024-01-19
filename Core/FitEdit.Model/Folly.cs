namespace FitEdit.Model;

/// <summary>
/// faux Polly. 
/// </summary>
public static class Folly
{
  /// <summary>
  /// Keep calling the given callback while it returns true.
  /// and while the given duration has not elapsed 
  /// and while the given count (if given) has not been reached.
  /// </summary>
  public static async Task RepeatAsync(Func<bool> callback, TimeSpan duration, int count = 0)
  {
    var start = DateTime.UtcNow;
    int i = 0;

    while (DateTime.UtcNow - start < duration)
    {
      if (count > 0 && i == count) { break; }
      if (callback()) { break; }

      await Task.Delay(1000);
      i++;
    }
  }

}
