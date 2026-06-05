using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using PasswordKeeper.Core.Models;

namespace PasswordKeeper.Core.Crypto;

public sealed class AesGcmVaultCipher : IVaultCipher
{
    private const int NonceSize = 12;
    private const int TagSize = 16;
    private const int KeySize = 32;

    public VaultEnvelope Encrypt(ReadOnlySpan<byte> plaintext, SecureBuffer key, KdfParameters kdfParams)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes for AES-256-GCM.", nameof(key));

        Span<byte> nonce = stackalloc byte[NonceSize];
        RandomNumberGenerator.Fill(nonce);

        byte[] ciphertext = new byte[plaintext.Length];
        Span<byte> tag = stackalloc byte[TagSize];

        var envelope = new VaultEnvelope
        {
            Version = 1,
            Kdf = kdfParams,
            NonceB64 = Convert.ToBase64String(nonce),
        };

        byte[] aad = BuildAssociatedData(envelope);

        using var aes = new AesGcm(key.AsReadOnlySpan(), TagSize);
        aes.Encrypt(nonce, plaintext, ciphertext, tag, aad);

        envelope.CiphertextB64 = Convert.ToBase64String(ciphertext);
        envelope.TagB64 = Convert.ToBase64String(tag);
        return envelope;
    }

    public byte[] Decrypt(VaultEnvelope envelope, SecureBuffer key)
    {
        if (key.Length != KeySize)
            throw new ArgumentException($"Key must be {KeySize} bytes for AES-256-GCM.", nameof(key));

        byte[] nonce = Convert.FromBase64String(envelope.NonceB64);
        byte[] ciphertext = Convert.FromBase64String(envelope.CiphertextB64);
        byte[] tag = Convert.FromBase64String(envelope.TagB64);
        byte[] aad = BuildAssociatedData(envelope);

        byte[] plaintext = new byte[ciphertext.Length];
        using var aes = new AesGcm(key.AsReadOnlySpan(), TagSize);
        aes.Decrypt(nonce, ciphertext, tag, plaintext, aad);
        return plaintext;
    }

    // AAD = canonical JSON of {version, kdf, nonceB64}. Any tampering with header fields fails decryption.
    private static byte[] BuildAssociatedData(VaultEnvelope envelope)
    {
        var header = new
        {
            envelope.Version,
            Kdf = new
            {
                envelope.Kdf.Algo,
                envelope.Kdf.M,
                envelope.Kdf.T,
                envelope.Kdf.P,
                envelope.Kdf.SaltB64,
            },
            envelope.NonceB64,
        };
        string json = JsonSerializer.Serialize(header);
        return Encoding.UTF8.GetBytes(json);
    }
}
