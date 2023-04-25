using System.Reflection;

namespace Dauer.Fuse.Secure;

public static class Defuse
{
  /// <summary>
  /// Redirect attempts to load Dauer.Ui, Dauer.Models, etc to Dauer.Fuse.dll
  /// </summary>
  /// <param name="from">The name of the requested assembly, e.g. Dauer.Ui, Dauer.Models, etc</param>
  /// <param name="to">The absolute path to Dauer.Fuse.dll</param>
  public static Assembly? Redirect(string from, string to)
  {
    if (!from.StartsWith("Dauer")) return null;

    Console.WriteLine($"Fuse: Trying to redirect from {from} to {to}");
    try
    {
      var assem = Assembly.LoadFile(to);
      Console.WriteLine($"Fuse: Redirecting to assembly: {assem.FullName}");
      return assem;
    }
    catch (Exception e)
    {
      Console.WriteLine($"Fuse: Could not redirect: {e}");
      return null;
    }
  }
}
