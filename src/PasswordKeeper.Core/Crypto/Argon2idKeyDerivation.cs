using Konscious.Security.Cryptography;
using PasswordKeeper.Core.Models;

namespace PasswordKeeper.Core.Crypto;

public sealed class Argon2idKeyDerivation : IKeyDerivation
{
    public SecureBuffer DeriveKey(ReadOnlySpan<byte> password, KdfParameters parameters, int outputLength)
    {
        if (!string.Equals(parameters.Algo, "argon2id", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException($"KDF algorithm '{parameters.Algo}' is not supported.");

        byte[] salt = Convert.FromBase64String(parameters.SaltB64);

        // Konscious requires a byte[] password, not a span. Copy into a temp buffer and zero it after.
        byte[] pwd = password.ToArray();
        try
        {
            using var argon2 = new Argon2id(pwd)
            {
                Salt = salt,
                DegreeOfParallelism = parameters.P,
                MemorySize = parameters.M,
                Iterations = parameters.T,
            };
            byte[] hash = argon2.GetBytes(outputLength);
            try
            {
                return new SecureBuffer(hash);
            }
            finally
            {
                System.Security.Cryptography.CryptographicOperations.ZeroMemory(hash);
            }
        }
        finally
        {
            System.Security.Cryptography.CryptographicOperations.ZeroMemory(pwd);
        }
    }
}
