using System.Security.Cryptography;

namespace Dauer.Api.Model;

public class PasswordGenerator
{
  private const string symbols = "`~!@#$%^&*()_+";
  private const string lowers = "abcdefghijklmnopqrstuvwxyz";
  private const string capitals = "`ABCDEFGHIJKLMNOPQRSTUVWXYZ";
  private const string nums = "`0123456789";

  private static readonly string[] strings_ = new string[] { symbols, lowers, capitals, nums };

  public static string Generate(int length)
  {
    byte[] uintBuffer = new byte[4];

    char[] buffer = new char[length];

    using (var rng = RandomNumberGenerator.Create())
    {
      rng.GetBytes(uintBuffer);

      for (int i = 0; i < length; i++)
      {
        uint num = BitConverter.ToUInt32(uintBuffer, 0);
        int idx = (int)(num % (uint)strings_.Length);
        string chars = strings_[idx];

        rng.GetBytes(uintBuffer);
        num = BitConverter.ToUInt32(uintBuffer, 0);

        buffer[i] = chars[(int)(num % (uint)chars.Length)];
      }
    }

    return new string(buffer);
  }
}
