#nullable enable

namespace Dauer.Model;

public static class CookieMapper
{
  public static System.Net.Cookie MapSystemCookie(this Cookie c) => new System.Net.Cookie
  {
    Name = c.Name,
    Value = c.Value,
    Domain = c.Domain,
    Path = c.Path,
    HttpOnly = c.HttpOnly,
    Secure = c.IsSecure,
    Expires = c.Expires,
  };

  public static Cookie MapModel(this System.Net.Cookie c) => new()
  {
    Name = c.Name,
    Value = c.Value,
    Domain = c.Domain,
    Path = c.Path,
    HttpOnly = c.HttpOnly,
    IsSecure = c.Secure,
    Expires = c.Expires,
  };
}
