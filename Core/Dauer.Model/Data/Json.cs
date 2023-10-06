﻿#nullable enable
using System.Text.Json;

namespace Dauer.Model.Data;

public static class Json
{
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

  public static string ToJson(this object? obj) => JsonSerializer.Serialize(obj);
}