using System.Security.Cryptography;
using System.Text;
using PasswordKeeper.Core.Crypto;
using PasswordKeeper.Core.Models;

namespace PasswordKeeper.Tests.Crypto;

public class Argon2idKeyDerivationTests
{
    // Use tiny parameters so unit tests run fast. Real app uses 128 MiB / t=3 / p=4.
    private static KdfParameters FastParams(byte[]? salt = null) => new()
    {
        Algo = "argon2id",
        M = 1024,
        T = 1,
        P = 1,
        SaltB64 = Convert.ToBase64String(salt ?? new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }),
    };

    [Fact]
    public void Same_password_and_salt_yields_same_key()
    {
        var kdf = new Argon2idKeyDerivation();
        var p = FastParams();
        byte[] pwd = Encoding.UTF8.GetBytes("correct horse battery staple");

        using var k1 = kdf.DeriveKey(pwd, p, 32);
        using var k2 = kdf.DeriveKey(pwd, p, 32);

        Assert.Equal(k1.AsReadOnlySpan().ToArray(), k2.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void Different_salt_yields_different_key()
    {
        var kdf = new Argon2idKeyDerivation();
        byte[] saltA = new byte[16]; RandomNumberGenerator.Fill(saltA);
        byte[] saltB = new byte[16]; RandomNumberGenerator.Fill(saltB);
        byte[] pwd = Encoding.UTF8.GetBytes("same-password");

        using var k1 = kdf.DeriveKey(pwd, FastParams(saltA), 32);
        using var k2 = kdf.DeriveKey(pwd, FastParams(saltB), 32);

        Assert.NotEqual(k1.AsReadOnlySpan().ToArray(), k2.AsReadOnlySpan().ToArray());
    }

    [Fact]
    public void SecureBuffer_zeroes_on_dispose()
    {
        var kdf = new Argon2idKeyDerivation();
        var key = kdf.DeriveKey(Encoding.UTF8.GetBytes("pw"), FastParams(), 32);
        Assert.False(key.IsZeroed);
        key.Dispose();
        Assert.True(key.IsZeroed);
    }

    [Fact]
    public void Throws_for_unknown_algo()
    {
        var kdf = new Argon2idKeyDerivation();
        var p = FastParams();
        p.Algo = "scrypt";
        Assert.Throws<NotSupportedException>(() => kdf.DeriveKey(new byte[] { 1 }, p, 32));
    }
}
