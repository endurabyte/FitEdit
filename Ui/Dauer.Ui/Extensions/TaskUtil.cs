namespace Dauer.Ui.Extensions;

public static class TaskUtil
{
  /// <summary>
  /// Give other jobs a chance to run on single-threaded platforms like WASM.
  /// </summary>
  public static async Task MaybeYield()
  {
    if (!OperatingSystem.IsBrowser()) { return; }

    // Note that Thread.Yield and Task.Yield do not have the desired effect on WASM
    await Task.Delay(1);
  }
}
