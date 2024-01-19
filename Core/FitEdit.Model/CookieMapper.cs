#nullable enable

using FitEdit.Model.Extensions;

namespace FitEdit.Model;

public static class CookieMapper
{
  public static System.Net.Cookie MapSystemCookie(this Cookie c) => new()
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
  
  /// <summary>
  /// Domain:
  /// null: return all cookies.
  /// empty string: return  no cookies.
  /// nonempty: return cookies only for the given domain.
  /// </summary>
  public static System.Net.CookieContainer MapCookieContainer(this Dictionary<string, Cookie> cookies, string? domain = null)
  { 
    var cookieContainer = new System.Net.CookieContainer();
    if (cookies == null) { return cookieContainer; }

    IEnumerable<Cookie> domainOnly = cookies.Values
      .Where(c => domain == null || c.Domain == domain)
      .ToList();

    foreach (var cookie in domainOnly)
    {
      cookieContainer.Add(cookie.MapSystemCookie());
    }

    return cookieContainer;
  }

  public static Dictionary<string, Cookie> MapModel(this System.Net.CookieContainer cookies) => cookies
    .GetAllCookies()
    .Select(c => c.MapModel())
    .ToDictionaryAllowDuplicateKeys(c => c.Name, c => c);
}
