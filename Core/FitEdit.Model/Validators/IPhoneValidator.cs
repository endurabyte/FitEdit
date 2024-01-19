#nullable enable

namespace FitEdit.Model.Validators;

public interface IPhoneValidator
{
  bool IsValid(string? phone);
}