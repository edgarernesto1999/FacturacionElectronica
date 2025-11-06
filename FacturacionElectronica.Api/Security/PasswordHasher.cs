using System.Security.Cryptography;
using System.Text;

/// Formato almacenado:  PBKDF2$<iteraciones>$<saltBase64>$<hashBase64>

namespace FacturacionElectronica.Api.Security
{
  public static class PasswordHasher
  {
    private const int SaltSize = 16;            // 128-bit
    private const int KeySize = 32;            // 256-bit
    private const int Iterations = 100_000;     // recomendado

    public static string Hash(string password)
    {
      using var rng = RandomNumberGenerator.Create();
      var salt = new byte[SaltSize];
      rng.GetBytes(salt);

      var pbkdf2 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
      var key = pbkdf2.GetBytes(KeySize);

      return $"PBKDF2${Iterations}${Convert.ToBase64String(salt)}${Convert.ToBase64String(key)}";
    }

    public static bool Verify(string password, string stored)
    {
      try
      {
        var parts = stored.Split('$');
        if (parts.Length != 4 || parts[0] != "PBKDF2") return false;

        var iterations = int.Parse(parts[1]);
        var salt = Convert.FromBase64String(parts[2]);
        var key = Convert.FromBase64String(parts[3]);

        var pbkdf2 = new Rfc2898DeriveBytes(password, salt, iterations, HashAlgorithmName.SHA256);
        var computed = pbkdf2.GetBytes(key.Length);

        return CryptographicOperations.FixedTimeEquals(computed, key);
      }
      catch { return false; }
    }
  }
}
