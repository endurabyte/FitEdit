#nullable enable
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using Dauer.Model;

namespace Dauer.Services;

public interface ICryptoService
{
  string? Decrypt(string salt, string? data);
  string? Encrypt(string salt, string? data);
}

public class NullCryptoService : ICryptoService
{
  public string? Decrypt(string salt, string? data) => data;
  public string? Encrypt(string salt, string? data) => data;
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

  public string? Encrypt(string salt, string? data)
  {
    if (data is null) { return null; }
    var algorithm = GetAlgorithm(salt);

    using ICryptoTransform encryptor = algorithm.CreateEncryptor();
    using var ms = new MemoryStream();
    using var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write);
    using var sw = new StreamWriter(cs);
    sw.Write(data);
    sw.Flush();
    cs.FlushFinalBlock();
    return Convert.ToBase64String(ms.ToArray());
  }

  public string? Decrypt(string salt, string? data)
  {
    if (data is null) { return null; }

    var algorithm = GetAlgorithm(salt);

    using ICryptoTransform decryptor = algorithm.CreateDecryptor();
    using var ms = new MemoryStream(Convert.FromBase64String(data));
    using var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read);
    using var sr = new StreamReader(cs);
    return sr.ReadToEnd();
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
