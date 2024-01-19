#nullable enable

namespace FitEdit.Model.Validators;

public class NullPhoneValidator : IPhoneValidator
{
  public bool IsValid(string? phone) => true;
}