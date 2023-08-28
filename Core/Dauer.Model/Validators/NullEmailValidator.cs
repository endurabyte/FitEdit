#nullable enable

namespace Dauer.Model.Validators;

public class NullEmailValidator : IEmailValidator
{
  public bool IsValid(string? email) => true;
}
