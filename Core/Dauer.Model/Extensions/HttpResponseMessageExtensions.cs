#nullable enable
using System.Net;

namespace Dauer.Model.Extensions;

public static class HttpResponseMessageExtensions
{
  public static bool RequireHttpOk(this HttpResponseMessage resp, string errorMessage)
  {
    if (!resp.IsSuccessStatusCode)
    {
      Log.Error(errorMessage);
      return false;
    }
    return true;
  }

  public static bool RequireHttpOk200(this HttpResponseMessage resp, string error)
  {
    if (!resp.IsSuccessStatusCode && !resp.StatusCode.Equals(HttpStatusCode.OK))
    {
      Log.Error(error);
      return false;
    }
    return true;
  }

}
