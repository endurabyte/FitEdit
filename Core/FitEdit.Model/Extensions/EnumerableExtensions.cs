namespace FitEdit.Model.Extensions;

public static class EnumerableExtensions
{
  public static Dictionary<TKey, TValue> ToDictionaryAllowDuplicateKeys<TKey, TValue, TEnum>(this IEnumerable<TEnum> e, Func<TEnum, TKey> getKey, Func<TEnum, TValue> getValue)
  {
    var dict = new Dictionary<TKey, TValue>();
    foreach (TEnum item in e)
    {
      dict[getKey(item)] = getValue(item);
    }
    return dict;
  }
  
  public static async Task<IEnumerable<T>> WhereAsync<T>(this IEnumerable<T> source, Func<T, Task<bool>> predicate)
  {
    var results = await Task.WhenAll(source.Select(async x => (x, await predicate(x))));
    return results.Where(x => x.Item2).Select(x => x.x);
  }

  public static IDictionary<TKey, TValue> AddRange<TKey, TValue>(this IDictionary<TKey, TValue> collection, IEnumerable<(TKey key, TValue value)> source)
  {
    foreach (var item in source)
    {
      collection[item.key] = item.value;
    }
    return collection;
  }
}