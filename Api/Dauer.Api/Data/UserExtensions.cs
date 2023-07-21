namespace Dauer.Api.Data;

public static class UserExtensions
{
  public static void Merge(this User a, User? b)
  {
    a.Id ??= b?.Id;
    a.Name ??= b?.Name;
    a.Email ??= b?.Email;
    a.StripeId ??= b?.StripeId;
    a.CognitoId ??= b?.CognitoId;
  }
}