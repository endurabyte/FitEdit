namespace Dauer.Api.Data;

public static class UserMapper
{
  public static Model.User MapModel(this User user) => new()
  {
    Id = user.Id,
    Name = user.Name,
    Email = user.Email,
    StripeId = user.StripeId,
    CognitoId = user.CognitoId,
  };

  public static User MapEntity(this Model.User user) => new()
  {
    Id = user.Id,
    Name = user.Name,
    Email = user.Email,
    StripeId = user.StripeId,
    CognitoId = user.CognitoId,
  };
}
