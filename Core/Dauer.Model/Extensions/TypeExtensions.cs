#nullable enable
using System.Reflection;

namespace Dauer.Model.Extensions;

public static class TypeExtensions
{
  private static readonly Dictionary<Type, Dictionary<int, string>> identifierCache_ = new();

  private static readonly HashSet<Type> intTypes_ = new(new[]
  {
    typeof(int),
    typeof(uint),
    typeof(short),
    typeof(ushort),
    typeof(byte),
    typeof(sbyte)
  });

  public static List<Type> DerivativesOf<T>() => typeof(T).Derivatives();
  public static List<Type> Derivatives(this Type t) => DerivativesOf(t, Assembly.GetAssembly(t));
  public static List<Type> DerivativesOf(Type t, Assembly? assembly) => assembly?
      .GetTypes()
      .Where(type => type != t && t.IsAssignableFrom(type)).ToList()
    ?? new List<Type>();

  public static bool IsIntlike(this Type type) => intTypes_.Contains(type);

  public static bool TryFindIdentifier(this Type type, int value, out string? identifier)
  {
    type.TryAddToCache();
    return identifierCache_[type].TryGetValue(value, out identifier);
  }

  /// <summary>
  /// Return all public static integer-like literals (int, short, byte, and their unsigned analogues) of the given type,
  /// or if the given type is an enum, return all enum entries.
  /// </summary>
  /// <param name="type"></param>
  /// <returns></returns>
  public static List<string> GetIdentifiers(this Type type)
  {
    type.TryAddToCache();
    return identifierCache_[type].Values.ToList();
  }

  private static void TryAddToCache(this Type type)
  {
    if (identifierCache_.ContainsKey(type))
    {
      return;
    }

    identifierCache_[type] = new();

    var enums = type.GetEnumEntries();
    foreach (var kvp in enums)
    {
      identifierCache_[type][kvp.Key] = kvp.Value;
    }

    var literals = type.GetLiterals();
    foreach (var kvp in literals)
    {
      identifierCache_[type][kvp.Key] = kvp.Value;
    }
  }

  /// <summary>
  /// Find all of the public static integer-like literals (int, short, byte, and their unsigned analogues) of the given type.
  /// Return a dictionary where the keys are the field integer value
  /// and the values are the field names.
  /// </summary>
  public static Dictionary<int, string> GetLiterals(this Type type)
  {
    var dict = new Dictionary<int, string>();

    var intLikes = type
      .GetFields(BindingFlags.Public | BindingFlags.Static)
      .Where(t => t.IsLiteral)
      .Where(t => t.FieldType.IsIntlike());

    // Not an enum, so search the public fields
    foreach (var field in intLikes)
    {
      object? value = field.GetValue(null);

      try
      {
        int i = Convert.ToInt32(value);
        dict[i] = field.Name;
      }
      catch (OverflowException)
      {
      }
    }

    return dict;
  }

  /// <summary>
  /// Find all of the enum entries of the given type.
  /// Return a dictionary where the keys are the enum backing value as integers
  /// and the values are the enum value names.
  /// </summary>
  public static Dictionary<int, string> GetEnumEntries(this Type type)
  {
    var dict = new Dictionary<int, string>();

    if (!type.IsEnum) { return dict; }

    Array values = Enum.GetValues(type);

    foreach (var value in values)
    {
      try
      {
        int i = Convert.ToInt32(value);
        dict[i] = $"{value}";
      }
      catch (OverflowException)
      {
      }
    }

    return dict;
  }
}
