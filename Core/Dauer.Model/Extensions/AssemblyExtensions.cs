#nullable enable
using System.Reflection;

namespace Dauer.Model.Extensions;

public static class AssemblyExtensions
{
  private static readonly Dictionary<Assembly, Dictionary<string, Type>> typeCache_ = new();

  /// <summary>
  /// Tries to find a public identifier (literal field or enum value) with a given value in a specified type within an assembly.
  /// </summary>
  /// <param name="assembly">The assembly to search for the type.</param>
  /// <param name="typeName">The name of the type to look for the identifier in.</param>
  /// <param name="value">The value of the identifier to find.</param>
  /// <param name="identifier">When this method returns, contains the name of the identifier if found, or null if not.</param>
  /// <returns><c>true</c> if an identifier was found; otherwise, <c>false</c>.</returns>
  /// <example>
  /// <code>
  /// var assembly = Assembly.GetExecutingAssembly();
  /// string identifier;
  /// if (TryFindIdentifier(assembly, "SourceType", 3, out identifier))
  /// {
  ///     Console.WriteLine("Identifier found: " + identifier);  // Outputs: "Identifier found: BluetoothLowEnergy"
  /// }
  /// else
  /// {
  ///     Console.WriteLine("Identifier not found.");
  /// }
  /// </code>
  /// </example>
  public static bool TryFindIdentifier(this Assembly assembly, string typeName, int value, out string? identifier)
  {
    identifier = null;

    return assembly.TryFindType(typeName, out Type? type) 
      && type != null 
      && type.TryFindIdentifier(value, out identifier);
  }

  /// <summary>
  /// Find the type with the given name within the assembly.
  /// Add the assembly to the cache if is wasn't already.
  /// </summary>
  public static bool TryFindType(this Assembly assembly, string typeName, out Type? type)
  {
    TryAddToCache(assembly);
    return typeCache_[assembly].TryGetValue(typeName, out type);
  }

  private static void TryAddToCache(Assembly assembly)
  {
    if (typeCache_.ContainsKey(assembly))
    {
      return;
    }

    typeCache_[assembly] = new();
    foreach (var t in assembly.ExportedTypes)
    {
      typeCache_[assembly][t.Name] = t;
    }
  }

  public static bool TryGetLoadedAssembly(string assemblyFullName, out Assembly assembly)
  {
    var assemblies = AppDomain.CurrentDomain.GetAssemblies();
    assembly = assemblies.FirstOrDefault(a => a.FullName?.StartsWith(assemblyFullName) ?? false)!;
    return assembly != null;
  }
}