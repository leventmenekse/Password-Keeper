using PasswordKeeper.Core.Models;

namespace PasswordKeeper.Core.Vault;

public interface IVaultStore
{
    bool Exists { get; }
    string VaultPath { get; }
    VaultEnvelope Load();
    void Save(VaultEnvelope envelope);
}
