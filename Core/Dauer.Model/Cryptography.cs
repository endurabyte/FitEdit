#nullable enable
namespace Dauer.Model;

public static class Cryptography
{
  /// <summary>
  /// Rotate the string according to a secret permutation. 
  /// This protects the password or salt in case either is hijacked;
  /// the attacker must also discover the mangle pattern.
  /// </summary>
  public static string Mangle(string s)
  {
    if (s.Length == 1) { return s; }

    int len = s.Length;

    int[] permutation = Enumerable
      .Range(1, len)
      .Select(Fibonacci.Get)
      .Select(fib => (int)Math.Abs(fib % len))
      .ToArray();

    var chars = s.ToList();

    foreach (int i in Enumerable.Range(0, len))
    {
      (chars[permutation[i]], chars[i]) = (chars[i], chars[permutation[i]]);
    }

    return new string(chars.ToArray());
  }
}