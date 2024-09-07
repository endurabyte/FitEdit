#nullable enable
using System.Text.Json;

namespace FitEdit.Model.Data;

public static class Json
{
  public static async Task<T?> MapFromJson<T>(this HttpContent content) => (await content.ReadAsStringAsync()).MapFromJson<T>();

  public static T? MapFromJson<T>(this string json)
  {
    try
    {
      return JsonSerializer.Deserialize<T>(json);
    }
    catch (Exception)
    {
      return default;
    }
  }

  public static string ToPrettyJson(this object? obj) => obj.ToJson(new JsonSerializerOptions { WriteIndented = true });
  public static string ToJson(this object? obj, JsonSerializerOptions? options = null) => JsonSerializer.Serialize(obj, options);
}