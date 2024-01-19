#nullable enable

namespace FitEdit.Model.Validators;

public class NullEmailValidator : IEmailValidator
{
  public bool IsValid(string? email) => true;
}
