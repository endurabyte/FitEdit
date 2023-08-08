namespace Dauer.Model.Extensions;

public static class ListExtensions
{
  public static List<T> AppendRange<T>(this List<T> list, IEnumerable<T> range)
  {
    list.AddRange(range);
    return list;
  }

  public static List<T> Sorted<T>(this List<T> l, Comparison<T> comparison)
  {
    l.Sort(comparison);
    return l;
  }
}
