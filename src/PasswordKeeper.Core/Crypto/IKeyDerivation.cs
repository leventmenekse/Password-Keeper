using PasswordKeeper.Core.Models;

namespace PasswordKeeper.Core.Crypto;

public interface IKeyDerivation
{
    SecureBuffer DeriveKey(ReadOnlySpan<byte> password, KdfParameters parameters, int outputLength);
}
