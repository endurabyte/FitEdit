namespace Dauer.Model.Factories;

public static class StringFactory
{
  private static readonly Random random_ = new();
  const string Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

  public static string Random(int length) => new string(Enumerable.Repeat(Alphabet, length)
    .Select(s => s[random_.Next(s.Length)]).ToArray());
}