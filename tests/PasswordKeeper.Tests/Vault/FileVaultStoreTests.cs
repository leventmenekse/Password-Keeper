using PasswordKeeper.Core.Models;
using PasswordKeeper.Core.Vault;

namespace PasswordKeeper.Tests.Vault;

public class FileVaultStoreTests : IDisposable
{
    private readonly string _dir;

    public FileVaultStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "pwk-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
    }

    private string VaultPath => Path.Combine(_dir, "vault.json");

    private static VaultEnvelope SampleEnvelope(string ciphertext = "abc")
        => new()
        {
            Version = 1,
            Kdf = new KdfParameters { SaltB64 = Convert.ToBase64String(new byte[16]) },
            NonceB64 = Convert.ToBase64String(new byte[12]),
            CiphertextB64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(ciphertext)),
            TagB64 = Convert.ToBase64String(new byte[16]),
        };

    [Fact]
    public void Save_then_load_round_trips()
    {
        var store = new FileVaultStore(VaultPath);
        var env = SampleEnvelope("payload-1");

        store.Save(env);
        var loaded = store.Load();

        Assert.Equal(env.CiphertextB64, loaded.CiphertextB64);
        Assert.Equal(env.NonceB64, loaded.NonceB64);
        Assert.Equal(env.TagB64, loaded.TagB64);
    }

    [Fact]
    public void Second_save_creates_backup_with_previous_content()
    {
        var store = new FileVaultStore(VaultPath);
        store.Save(SampleEnvelope("first"));
        store.Save(SampleEnvelope("second"));

        Assert.True(File.Exists(VaultPath + ".bak"));
        var current = store.Load();
        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("second")), current.CiphertextB64);
    }

    [Fact]
    public void Load_falls_back_to_backup_if_primary_corrupt()
    {
        var store = new FileVaultStore(VaultPath);
        store.Save(SampleEnvelope("first"));
        store.Save(SampleEnvelope("second"));

        // Corrupt the primary
        File.WriteAllText(VaultPath, "{ not valid json");

        var loaded = store.Load();
        Assert.Equal(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("first")), loaded.CiphertextB64);
    }

    [Fact]
    public void Exists_is_false_for_fresh_directory()
    {
        var store = new FileVaultStore(VaultPath);
        Assert.False(store.Exists);
    }

    [Fact]
    public void Exists_is_true_after_save()
    {
        var store = new FileVaultStore(VaultPath);
        store.Save(SampleEnvelope());
        Assert.True(store.Exists);
    }
}
