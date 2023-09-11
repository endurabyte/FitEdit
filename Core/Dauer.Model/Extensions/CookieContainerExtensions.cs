#nullable enable
using System.Net;

namespace Dauer.Model.Extensions;

public static class CookieContainerExtensions
{
  /// <summary>
  /// Validates the cookie presence.
  /// </summary>
  /// <param name="container">The container.</param>
  /// <param name="cookieName">Name of the cookie.</param>
  /// <exception cref="Exception">Missing cookie {cookieName}</exception>
  public static bool ValidateCookiePresence(this CookieContainer container, string cookieName, string url)
  {
    var cookies = container.GetCookies(new Uri(url)).Cast<System.Net.Cookie>().ToList();
    System.Net.Cookie? cookie = cookies.Find(e => string.Equals(cookieName, e.Name, StringComparison.InvariantCultureIgnoreCase));

    if (cookie is null)
    {
      Log.Error($"Missing cookie {cookieName}");
      return false;
    }

    if (cookie.Expired)
    {
      Log.Error($"Expired cookie {cookieName}");
      return false;
    }

    return true;
  }
}
