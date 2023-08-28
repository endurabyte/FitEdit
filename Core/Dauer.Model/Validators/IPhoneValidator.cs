#nullable enable

namespace Dauer.Model.Validators;

public interface IPhoneValidator
{
  bool IsValid(string? phone);
}