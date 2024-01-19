#nullable enable

namespace FitEdit.Model;

public static class Fibonacci
{
  private static readonly Dictionary<int, long> memo_ = new(100) 
  {
    { 0, 0 },
    { 1, 1 }
  };

  /// <summary>
  /// Return the nth element from the Fibonacci sequence with memoization
  /// </summary>
  public static long Get(int n)
  {
    // Check if value already memoized and return
    if (memo_.TryGetValue(n, out long existingValue))
    {
      return existingValue;
    }

    // Start loop from maximum memoized index
    int startIndex = 2;
    if (memo_.Count > 2)
    {
      startIndex = memo_.Count;
    }

    long f1 = memo_[startIndex - 2];
    long f2 = memo_[startIndex - 1];
    long f_result = 0;

    for (int i = startIndex; i <= n; i++)
    {
      f_result = f1 + f2;
      memo_[i] = f_result;

      f1 = f2;
      f2 = f_result;
    }

    return f_result;
  }
}