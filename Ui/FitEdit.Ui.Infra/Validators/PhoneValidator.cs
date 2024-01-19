using System.Text.RegularExpressions;
using FitEdit.Model.Validators;

namespace FitEdit.Ui.Infra.Validators;

public partial class PhoneValidator : IPhoneValidator
{
  [GeneratedRegex(@"^\+?[1-9]\d{1,14}$")]
  private static partial Regex Regex();

  public bool IsValid(string? phone)
  {
    if (phone == null) { return false; }
    return Regex().IsMatch(phone);
  }
}
