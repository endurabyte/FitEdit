using System.Security.Cryptography;
using System.Text;

namespace FitEdit.Model.Extensions;

public static class ByteExtensions
{
  public static string Sha256(this object o) => Encoding.UTF8.GetBytes($"{o}").Sha256();
  public static string Sha256(this byte[] bytes) => BitConverter.ToString(SHA256.HashData(bytes)).Replace("-", string.Empty);

  /// <summary>
  /// Return the index of the nth occurrence of the given byte sequence from the given start index in the given byte sequence.
  /// Return -1 if no such occurence exists.
  /// 
  /// <para/>
  /// Example:
  /// 
  /// <code>
  /// 
  /// var container = new byte[]
  /// {
  ///     0x6b, 0x3f, 0xb5, 0x32, 0x6b, 0x3e, 0x66, 0x0e,
  ///     0x59, 0x04, 0xcf, 0xff, 0x02, 0x6b, 0x3f, 0x0b
  ///     //                            ^^^^^^^^^^ desired sequence
  /// };
  /// int foundIndex = FindNextOccurrence(container, new byte[] { 0x6b, 0x3f }, 2, 1);
  /// // foundIndex == 13
  /// </code>
  /// </summary>
  public static int FindNextOccurrence(this IList<byte> container, IList<byte> toFind, int startIndex = 0, int nth = 1)
  {
    // Ensure the start index is within range
    if (startIndex < 0 || startIndex >= container.Count - 1)
    {
      return -1;
    }

    int count = 0;
    for (int i = startIndex; i < container.Count - 1; i++)
    {
      bool found = true;

      for (int j = 0; j < toFind.Count; j++)
      {
        if (container[i + j] != toFind[j])
        {
          found = false;
          continue;
        }
      }

      if (found)
      {
        count++;

        if (nth == count)
        {
          return i;
        }
      }
    }

    return -1;
  }

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