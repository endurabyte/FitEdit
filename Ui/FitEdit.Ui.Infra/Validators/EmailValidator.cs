using System.Text.RegularExpressions;
using FitEdit.Model.Validators;

namespace FitEdit.Ui.Infra.Validators;

public partial class EmailValidator : IEmailValidator
{
  [GeneratedRegex("^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\\.[a-zA-Z]{2,}$")]
  private static partial Regex Regex();

  public bool IsValid(string? email)
  {
    if (email == null) { return false; }
    return Regex().IsMatch(email);
  }
}