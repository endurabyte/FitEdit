namespace Dauer.Model.Extensions;

public static class ListExtensions
{
  public static List<T> Sorted<T>(this List<T> l, Comparison<T> comparison)
  {
    l.Sort(comparison);
    return l;
  }
}