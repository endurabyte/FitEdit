using Supabase.Gotrue;

namespace Dauer.Ui.Supabase;

public static class SessionMapper
{
  public static Session? Map(this Dauer.Model.Authorization? auth)
  {
    if (auth == null) { return null; }

    return new Session
    {
      AccessToken = auth.AccessToken,
      RefreshToken = auth.RefreshToken,
      User = new User { Email = auth.Username },
      TokenType = "bearer",
      CreatedAt = auth.Created.UtcDateTime,
      ExpiresIn = (int)(auth.Expiry.UtcDateTime - auth.Created.UtcDateTime).TotalSeconds,
    };
  }

  public static Dauer.Model.Authorization? Map(this Session? session)
  {
    if (session == null) { return null; }

    return new Dauer.Model.Authorization
    {
      Id = "Dauer.Api",
      AccessToken = session.AccessToken,
      RefreshToken = session.RefreshToken,
      Username = session.User?.Email,
      Created = session.CreatedAt,
      Expiry = session.ExpiresAt(),
    };
  }
}
