namespace Dauer.Api.Model;

public class User
{
  public string? Id { get; set; }
  public string? Name { get; set; }
  public string? Email { get; set; }

  public string? CognitoId { get; set; }
  public string? StripeId { get; set; }
}
