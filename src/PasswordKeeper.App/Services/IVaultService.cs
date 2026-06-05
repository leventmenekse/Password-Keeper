using PasswordKeeper.Core.Models;

namespace PasswordKeeper.App.Services;

public interface IVaultService
{
    bool VaultExists { get; }
    bool IsUnlocked { get; }
    Vault? CurrentVault { get; }

    /// <summary>Create a brand-new vault with the given master password.</summary>
    Task CreateAsync(System.Security.SecureString masterPassword);

    /// <summary>Unlock an existing vault. Returns false on wrong password.</summary>
    Task<bool> UnlockAsync(System.Security.SecureString masterPassword);

    /// <summary>Persist the current in-memory vault to disk.</summary>
    Task SaveAsync();

    /// <summary>Zero the session key and drop the in-memory vault.</summary>
    void Lock();
}
