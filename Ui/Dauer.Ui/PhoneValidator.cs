using System.Text.RegularExpressions;

namespace Dauer.Ui;

public static partial class PhoneValidator
{
  [GeneratedRegex(@"^\+?[1-9]\d{1,14}$")]
  private static partial Regex Regex();

  public static bool IsValid(string? phone)
  {
    if (phone == null) { return false; }
    return Regex().IsMatch(phone);
  }
}
