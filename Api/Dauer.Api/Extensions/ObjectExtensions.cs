using System.Text.Json;

namespace Dauer.Api.Extensions;

public static class ObjectExtensions
{
  public static string Json(this object o) => JsonSerializer.Serialize(o);
}

