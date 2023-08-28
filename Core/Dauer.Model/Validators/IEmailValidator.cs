#nullable enable

namespace Dauer.Model.Validators;

public interface IEmailValidator
{
  bool IsValid(string? email);
}