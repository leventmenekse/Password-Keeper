namespace PasswordKeeper.Core.Models;

public sealed class Vault
{
    public List<VaultEntry> Entries { get; set; } = new();
    public List<Category> Categories { get; set; } = new();
}
