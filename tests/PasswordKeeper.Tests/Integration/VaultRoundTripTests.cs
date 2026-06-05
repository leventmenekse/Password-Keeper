using System.Text;
using PasswordKeeper.Core.Crypto;
using PasswordKeeper.Core.Models;
using PasswordKeeper.Core.Vault;

namespace PasswordKeeper.Tests.Integration;

// End-to-end: encrypt vault, write to disk, read back, decrypt, verify entries.
public class VaultRoundTripTests : IDisposable
{
    private readonly string _dir;

    public VaultRoundTripTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "pwk-e2e-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    [Fact]
    public void Full_round_trip_preserves_entries()
    {
        var kdf = new Argon2idKeyDerivation();
        var cipher = new AesGcmVaultCipher();
        var store = new FileVaultStore(Path.Combine(_dir, "vault.json"));

        byte[] salt = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(salt);
        var kdfParams = new KdfParameters
        {
            M = 1024, T = 1, P = 1,
            SaltB64 = Convert.ToBase64String(salt),
        };

        byte[] pwd = Encoding.UTF8.GetBytes("correct horse battery staple");
        using var key = kdf.DeriveKey(pwd, kdfParams, 32);

        var vault = new Core.Models.Vault();
        var cat = new Category { Name = "Email" };
        vault.Categories.Add(cat);
        vault.Entries.Add(new VaultEntry
        {
            Title = "Gmail",
            Username = "me@example.com",
            Password = "hunter2",
            Url = "https://mail.google.com",
            Notes = "test entry",
            CategoryId = cat.Id,
        });

        var envelope = cipher.Encrypt(VaultSerializer.SerializeVault(vault), key, kdfParams);
        store.Save(envelope);

        var loadedEnvelope = store.Load();
        byte[] plaintext = cipher.Decrypt(loadedEnvelope, key);
        var loaded = VaultSerializer.DeserializeVault(plaintext);

        Assert.Single(loaded.Entries);
        Assert.Equal("Gmail", loaded.Entries[0].Title);
        Assert.Equal("hunter2", loaded.Entries[0].Password);
        Assert.Single(loaded.Categories);
        Assert.Equal("Email", loaded.Categories[0].Name);
    }

    [Fact]
    public void Custom_fields_round_trip_including_secrets()
    {
        var kdf = new Argon2idKeyDerivation();
        var cipher = new AesGcmVaultCipher();
        var store = new FileVaultStore(Path.Combine(_dir, "vault.json"));

        byte[] salt = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(salt);
        var kdfParams = new KdfParameters
        {
            M = 1024, T = 1, P = 1,
            SaltB64 = Convert.ToBase64String(salt),
        };

        using var key = kdf.DeriveKey(Encoding.UTF8.GetBytes("pw"), kdfParams, 32);

        var vault = new Core.Models.Vault();
        vault.Entries.Add(new VaultEntry
        {
            Title = "prod ssh",
            CustomFields =
            {
                new CustomField { Label = "host", Value = "10.0.0.5" },
                new CustomField { Label = "port", Value = "22" },
                new CustomField { Label = "key path", Value = "~/.ssh/id_ed25519" },
                new CustomField { Label = "key passphrase", Value = "s3cret!", IsSecret = true },
            },
        });

        var envelope = cipher.Encrypt(VaultSerializer.SerializeVault(vault), key, kdfParams);
        store.Save(envelope);

        var loaded = VaultSerializer.DeserializeVault(cipher.Decrypt(store.Load(), key));
        var entry = Assert.Single(loaded.Entries);
        Assert.Equal(4, entry.CustomFields.Count);
        var passphrase = entry.CustomFields.Single(f => f.Label == "key passphrase");
        Assert.True(passphrase.IsSecret);
        Assert.Equal("s3cret!", passphrase.Value);
    }

    [Fact]
    public void Wrong_password_after_persist_fails_decryption()
    {
        var kdf = new Argon2idKeyDerivation();
        var cipher = new AesGcmVaultCipher();
        var store = new FileVaultStore(Path.Combine(_dir, "vault.json"));

        byte[] salt = new byte[16];
        System.Security.Cryptography.RandomNumberGenerator.Fill(salt);
        var kdfParams = new KdfParameters
        {
            M = 1024, T = 1, P = 1,
            SaltB64 = Convert.ToBase64String(salt),
        };

        using (var k1 = kdf.DeriveKey(Encoding.UTF8.GetBytes("right"), kdfParams, 32))
        {
            var env = cipher.Encrypt(Encoding.UTF8.GetBytes("payload"), k1, kdfParams);
            store.Save(env);
        }

        var loaded = store.Load();
        using var k2 = kdf.DeriveKey(Encoding.UTF8.GetBytes("wrong"), kdfParams, 32);
        Assert.Throws<System.Security.Cryptography.AuthenticationTagMismatchException>(
            () => cipher.Decrypt(loaded, k2));
    }
}
