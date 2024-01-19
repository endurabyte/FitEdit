#nullable enable

namespace FitEdit.Model.Validators;

public interface IEmailValidator
{
  bool IsValid(string? email);
}