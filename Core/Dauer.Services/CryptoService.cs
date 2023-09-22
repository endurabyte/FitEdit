#nullable enable
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Dauer.Model;
using Dauer.Model.Extensions;

namespace Dauer.Services;

public interface ICryptoService
{
  string? Decrypt(string? salt, string? data);
  string? Encrypt(string? salt, string? data);

  byte[]? Decrypt(string? salt, byte[] data);
  byte[]? Encrypt(string? salt, byte[] data);
}

public class NullCryptoService : ICryptoService
{
  public string? Decrypt(string? salt, string? data) => data;
  public string? Encrypt(string? salt, string? data) => data;
  public byte[]? Decrypt(string? salt, byte[] data) => data;
  public byte[]? Encrypt(string? salt, byte[] data) => data;
}

public class CryptoService : ICryptoService
{
  private static readonly ConcurrentDictionary<string, SymmetricAlgorithm> dict_ = new();
  private readonly string password_;
  private const int mangleIterations_ = 100;

  public CryptoService(string password)
  {
    password_ = password;

    foreach (var _ in Enumerable.Range(0, mangleIterations_))
    {
      password_ = Cryptography.Mangle(password_);
    }
  }

  public string? Encrypt(string? salt, string? data)
  {
    if (data is null || data == "null") { return null; }
    byte[] unencrypted = Encoding.UTF8.GetBytes(data);
    byte[]? encrypted = Encrypt(salt, unencrypted);
    if (encrypted == null) { return null; }

    return Convert.ToBase64String(encrypted);
  }

  public byte[]? Encrypt(string? salt, byte[] data)
  {
    if (salt is null) { return null; }
    if (data is null) { return null; }
    var algorithm = GetAlgorithm(salt);

    using ICryptoTransform encryptor = algorithm.CreateEncryptor();
    using var ms = new MemoryStream();
    using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
    using var sw = new BinaryWriter (cs);
    sw.Write(data);
    sw.Flush();
    cs.FlushFinalBlock();
    return ms.ToArray();
  }

  public string? Decrypt(string? salt, string? data)
  {
    if (salt is null) { return null; }
    if (data is null || data == "null") { return null; }

    byte[] encrypted = Convert.FromBase64String(data);
    byte[]? decrypted = Decrypt(salt, encrypted);
    if (decrypted is null) { return null; }

    return Encoding.UTF8.GetString(decrypted);
  }

  public byte[]? Decrypt(string? salt, byte[]? data)
  {
    if (salt is null) { return null; }
    if (data is null) { return null; }

    var algorithm = GetAlgorithm(salt);

    using ICryptoTransform decryptor = algorithm.CreateDecryptor();
    using var ms = new MemoryStream(data);
    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);

    try
    {
      return cs.ReadAllBytes();
    }
    catch (CryptographicException) // Not encrypted?
    {
      return data;
    }
  }

  private SymmetricAlgorithm GetAlgorithm(string salt)
  {
    salt = Cryptography.Mangle(salt);
    string config = $"{password_}^{salt}";

    if (dict_.TryGetValue(config, out SymmetricAlgorithm? alg))
    {
      return alg;
    }

    if (salt.Length < 8)
    {
      salt += new string('+', 8 - salt.Length);
    }

    var saltBytes = Encoding.ASCII.GetBytes(salt);
    var rfc2898DeriveBytes = new Rfc2898DeriveBytes(password_, saltBytes, 1000, HashAlgorithmName.SHA256);

    alg = Aes.Create();
    alg.Key = rfc2898DeriveBytes.GetBytes(alg.KeySize / 8);
    alg.IV = rfc2898DeriveBytes.GetBytes(alg.BlockSize / 8);
    alg.Padding = PaddingMode.PKCS7;

    dict_.TryAdd(config, alg);


    return alg;
  }
}
