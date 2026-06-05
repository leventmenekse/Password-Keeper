namespace PasswordKeeper.Core.Models;

public sealed class VaultEnvelope
{
    public int Version { get; set; } = 1;
    public KdfParameters Kdf { get; set; } = new();
    public string NonceB64 { get; set; } = string.Empty;
    public string CiphertextB64 { get; set; } = string.Empty;
    public string TagB64 { get; set; } = string.Empty;
}
