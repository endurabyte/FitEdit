#nullable enable
namespace FitEdit.Model.Extensions;

public static class HashSetExtensions
{
  public static HashSet<T> AddRange<T>(this HashSet<T> hs, IEnumerable<T>? range) 
  {
    if (range is null) { return hs; }

    foreach (T t in range)
    {
      hs.Add(t);
    }
    return hs;
  }
}