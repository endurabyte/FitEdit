using FitEdit.Ui.Infra.Validators;
using Supabase.Gotrue;

namespace FitEdit.Ui.Infra.Supabase;

public static class SessionMapper
{
  public static Session? Map(this FitEdit.Model.Authorization? auth)
  {
    if (auth == null) { return null; }

    return new Session
    {
      AccessToken = auth.AccessToken,
      RefreshToken = auth.RefreshToken,
      User = new User 
      { 
        Email = new EmailValidator().IsValid(auth.Username) ? auth.Username : null,
        Phone = new PhoneValidator().IsValid(auth.Username) ? auth.Username : null 
      },
      TokenType = "bearer",
      CreatedAt = auth.Created.UtcDateTime,
      ExpiresIn = (int)(auth.Expiry.UtcDateTime - auth.Created.UtcDateTime).TotalSeconds,
    };
  }

  public static FitEdit.Model.Authorization? Map(this Session? session)
  {
    if (session == null) { return null; }

    return new FitEdit.Model.Authorization
    {
      Id = "FitEdit.Api",
      AccessToken = session.AccessToken,
      RefreshToken = session.RefreshToken,
      Username = !string.IsNullOrWhiteSpace(session.User?.Email) 
        ? session.User?.Email 
        : session.User?.Phone,
      Sub = session.User?.Id,
      Created = session.CreatedAt,
      Expiry = session.ExpiresAt(),
    };
  }
}
