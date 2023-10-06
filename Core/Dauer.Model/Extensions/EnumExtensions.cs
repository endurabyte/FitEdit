#nullable enable

namespace Dauer.Model.Extensions;

public static class EnumExtensions
  {
    /// <summary>
    /// Map an Enum to an Enum via its string representation
    /// 
    /// <para/>
    /// Example: Map&lt;Dto.Device&gt;(Models.DeviceName.Pps) => "Pps" => Dto.DeviceName.Pps
    /// </summary>
    public static T Map<T>(this Enum e) where T : struct, Enum => e.ToString().Map<T>();

    /// <summary>
    /// Map a string to an Enum
    /// 
    /// <para/>
    /// Example: <c>Map&lt;Dto.Device&gt;("Pps") => Dto.DeviceName.Pps </c>
    /// </summary>
    public static T Map<T>(this string s) where T : struct, Enum
      => Enum.TryParse(RequireString(s), ignoreCase: true, out T t) ? t : throw new ArgumentException("");

    public static T[] Values<T>() where T : Enum => Enum.GetValues(typeof(T))
      .Cast<T>()
      .ToArray();

    public static string Name(this Enum e) => $"{e.GetType()}.{e}";

    private static string RequireString(string s)
    {
      if (string.IsNullOrWhiteSpace(s))
      {
        throw new ArgumentException("String was null or whitespace");
      }
      return s;
    }
  }
