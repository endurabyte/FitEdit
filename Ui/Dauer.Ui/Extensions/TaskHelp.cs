namespace Dauer.Ui.Extensions;

public static class TaskHelp
{
  /// <summary>
  /// Give other jobs a chance to run on single-threaded platforms like WASM
  /// </summary>
  public static async Task MaybeYield()
  {
    if (!OperatingSystem.IsBrowser()) { return; }

    await Task.Delay(1);
  }
}
