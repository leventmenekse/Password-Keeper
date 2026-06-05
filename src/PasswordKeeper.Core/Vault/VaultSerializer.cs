using System.Text;
using System.Text.Json;
using PasswordKeeper.Core.Models;

namespace PasswordKeeper.Core.Vault;

public static class VaultSerializer
{
    private static readonly JsonSerializerOptions VaultOptions = new()
    {
        WriteIndented = false,
    };

    private static readonly JsonSerializerOptions EnvelopeOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public static byte[] SerializeVault(Models.Vault vault)
        => Encoding.UTF8.GetBytes(JsonSerializer.Serialize(vault, VaultOptions));

    public static Models.Vault DeserializeVault(ReadOnlySpan<byte> plaintext)
        => JsonSerializer.Deserialize<Models.Vault>(plaintext, VaultOptions)
           ?? new Models.Vault();

    public static byte[] SerializeEnvelope(VaultEnvelope envelope)
        => JsonSerializer.SerializeToUtf8Bytes(envelope, EnvelopeOptions);

    public static VaultEnvelope DeserializeEnvelope(ReadOnlySpan<byte> bytes)
        => JsonSerializer.Deserialize<VaultEnvelope>(bytes, EnvelopeOptions)
           ?? throw new InvalidDataException("Vault envelope is empty or invalid.");
}
