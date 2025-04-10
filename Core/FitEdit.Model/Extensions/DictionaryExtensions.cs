﻿namespace FitEdit.Model.Extensions;

public static class DictionaryExtensions
{
  public static Dictionary<TValue, TKey> Reverse<TKey, TValue>(this Dictionary<TKey, TValue> dict) 
    where TKey : notnull
    where TValue : notnull
      => dict.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);
}