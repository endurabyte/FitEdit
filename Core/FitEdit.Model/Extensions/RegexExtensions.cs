#nullable enable
using System.Text.RegularExpressions;

namespace FitEdit.Model.Extensions;

public static class RegexExtensions
{
  /// <summary>
  /// Gets the value by pattern.
  /// </summary>
  /// <param name="data">The data.</param>
  /// <param name="expectedCountOfGroups">The expected count of groups.</param>
  /// <param name="groupPosition">The group position.</param>
  /// <returns>Value of particular match group.</returns>
  /// <exception cref="Exception">Could not match expected pattern {pattern}</exception>
  public static string? GetSingleValue(this Regex regex, string data, int expectedCountOfGroups, int groupPosition)
  {
    var match = regex.Match(data);
    return match.Success && match.Groups.Count == expectedCountOfGroups 
      ? match.Groups[groupPosition].Value 
      : null;
  }
}
