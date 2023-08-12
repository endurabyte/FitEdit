using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Dauer.Model;

namespace Dauer.Ui.Supabase;

public class AuthorizationFactory
{
  public static Authorization Create(string? accessToken, string? refreshToken)
  {
    var token = new JwtSecurityTokenHandler().ReadJwtToken(accessToken);
    var identity = new ClaimsPrincipal(new ClaimsIdentity(token.Claims));

    string? username = identity.Claims.FirstOrDefault(x => x.Type == "email")?.Value;
    string? sub = identity.Claims.FirstOrDefault(x => x.Type == "sub")?.Value;
    _ = long.TryParse(identity.Claims.FirstOrDefault(x => x.Type == "exp")?.Value, out long exp);
    _ = long.TryParse(identity.Claims.FirstOrDefault(x => x.Type == "iat")?.Value, out long iat);
    var issuedAt = DateTimeOffset.FromUnixTimeSeconds(iat);
    var expiry = DateTimeOffset.FromUnixTimeSeconds(exp);

    return new Authorization
    {
      Username = username,
      IdentityToken = "",
      Id = "Dauer.Api",
      Created = issuedAt,
      Expiry = expiry,
      AccessToken = accessToken,
      RefreshToken = refreshToken,
    };
  }
}
