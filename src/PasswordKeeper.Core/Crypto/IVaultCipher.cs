using PasswordKeeper.Core.Models;

namespace PasswordKeeper.Core.Crypto;

public interface IVaultCipher
{
    VaultEnvelope Encrypt(ReadOnlySpan<byte> plaintext, SecureBuffer key, KdfParameters kdfParams);
    byte[] Decrypt(VaultEnvelope envelope, SecureBuffer key);
}
