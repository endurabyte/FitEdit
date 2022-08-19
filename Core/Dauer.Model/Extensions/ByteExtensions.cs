namespace FundLog.Model.Extensions;

public static class ByteExtensions
{
  /// <summary>
  /// Replace instances of a byte sequence with a new byte sequence in the given input.
  /// Equivalent of string.Replace
  /// </summary>
  public static IEnumerable<byte> Replace(this IEnumerable<byte> input, IEnumerable<byte> from, IEnumerable<byte> to)
  {
    var iter = from.GetEnumerator();
    iter.MoveNext();
    int match = 0;
    foreach (var data in input)
    {
      if (data == iter.Current)
      {
        match++;
        if (iter.MoveNext()) { continue; }
        foreach (byte d in to) { yield return d; }
        match = 0;
        iter.Reset();
        iter.MoveNext();
        continue;
      }
      if (0 != match)
      {
        foreach (byte d in from.Take(match)) { yield return d; }
        match = 0;
        iter.Reset();
        iter.MoveNext();
      }
      yield return data;
    }
    if (0 != match)
    {
      foreach (byte d in from.Take(match)) { yield return d; }
    }
  }
}