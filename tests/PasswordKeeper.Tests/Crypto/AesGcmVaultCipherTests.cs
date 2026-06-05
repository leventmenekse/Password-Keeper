using System.Security.Cryptography;
using System.Text;
using PasswordKeeper.Core.Crypto;
using PasswordKeeper.Core.Models;

namespace PasswordKeeper.Tests.Crypto;

public class AesGcmVaultCipherTests
{
    private static KdfParameters DummyKdf() => new()
    {
        Algo = "argon2id", M = 1024, T = 1, P = 1,
        SaltB64 = Convert.ToBase64String(new byte[16]),
    };

    private static SecureBuffer RandomKey()
    {
        byte[] k = new byte[32];
        RandomNumberGenerator.Fill(k);
        return new SecureBuffer(k);
    }

    [Fact]
    public void Encrypt_then_decrypt_round_trips()
    {
        var cipher = new AesGcmVaultCipher();
        using var key = RandomKey();
        byte[] plaintext = Encoding.UTF8.GetBytes("hello, vault");

        var env = cipher.Encrypt(plaintext, key, DummyKdf());
        byte[] decrypted = cipher.Decrypt(env, key);

        Assert.Equal(plaintext, decrypted);
    }

    [Fact]
    public void Wrong_key_throws_cryptographic_exception()
    {
        var cipher = new AesGcmVaultCipher();
        using var key = RandomKey();
        using var wrongKey = RandomKey();

        var env = cipher.Encrypt(Encoding.UTF8.GetBytes("secret"), key, DummyKdf());
        Assert.Throws<AuthenticationTagMismatchException>(() => cipher.Decrypt(env, wrongKey));
    }

    [Fact]
    public void Tampered_ciphertext_throws()
    {
        var cipher = new AesGcmVaultCipher();
        using var key = RandomKey();
        var env = cipher.Encrypt(Encoding.UTF8.GetBytes("secret"), key, DummyKdf());

        byte[] ct = Convert.FromBase64String(env.CiphertextB64);
        ct[0] ^= 0xFF;
        env.CiphertextB64 = Convert.ToBase64String(ct);

        Assert.Throws<AuthenticationTagMismatchException>(() => cipher.Decrypt(env, key));
    }

    [Fact]
    public void Tampered_tag_throws()
    {
        var cipher = new AesGcmVaultCipher();
        using var key = RandomKey();
        var env = cipher.Encrypt(Encoding.UTF8.GetBytes("secret"), key, DummyKdf());

        byte[] tag = Convert.FromBase64String(env.TagB64);
        tag[0] ^= 0xFF;
        env.TagB64 = Convert.ToBase64String(tag);

        Assert.Throws<AuthenticationTagMismatchException>(() => cipher.Decrypt(env, key));
    }

    [Fact]
    public void Tampered_aad_throws()
    {
        var cipher = new AesGcmVaultCipher();
        using var key = RandomKey();
        var env = cipher.Encrypt(Encoding.UTF8.GetBytes("secret"), key, DummyKdf());

        // Change a KDF parameter -- this is AAD, so AEAD must reject it
        env.Kdf.T = env.Kdf.T + 1;

        Assert.Throws<AuthenticationTagMismatchException>(() => cipher.Decrypt(env, key));
    }

    [Fact]
    public void Each_encrypt_uses_unique_nonce()
    {
        var cipher = new AesGcmVaultCipher();
        using var key = RandomKey();
        var seen = new HashSet<string>();

        for (int i = 0; i < 200; i++)
        {
            var env = cipher.Encrypt(Encoding.UTF8.GetBytes("x"), key, DummyKdf());
            Assert.True(seen.Add(env.NonceB64), "Nonce reused — fatal for AES-GCM");
        }
    }
}
