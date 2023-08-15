using System.Text.RegularExpressions;

namespace Dauer.Ui;

public static partial class EmailValidator
{
  [GeneratedRegex("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")]
  private static partial Regex Regex();

  public static bool IsValid(string? email)
  {
    if (email == null) { return false; }
    return Regex().IsMatch(email);
  }
}