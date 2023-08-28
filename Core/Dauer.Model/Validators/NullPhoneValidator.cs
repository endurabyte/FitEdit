#nullable enable

namespace Dauer.Model.Validators;

public class NullPhoneValidator : IPhoneValidator
{
  public bool IsValid(string? phone) => true;
}