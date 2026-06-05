using System.Runtime.InteropServices;
using System.Security;
using System.Security.Cryptography;
using PasswordKeeper.Core.Crypto;
using PasswordKeeper.Core.Models;
using PasswordKeeper.Core.Vault;

namespace PasswordKeeper.App.Services;

public sealed class VaultService : IVaultService, IDisposable
{
    private readonly IKeyDerivation _kdf;
    private readonly IVaultCipher _cipher;
    private readonly IVaultStore _store;

    private SecureBuffer? _key;
    private KdfParameters? _kdfParams;
    private Vault? _vault;

    public VaultService(IKeyDerivation kdf, IVaultCipher cipher, IVaultStore store)
    {
        _kdf = kdf;
        _cipher = cipher;
        _store = store;
    }

    public bool VaultExists => _store.Exists;
    public bool IsUnlocked => _vault is not null && _key is not null;
    public Vault? CurrentVault => _vault;

    public Task CreateAsync(SecureString masterPassword)
    {
        return Task.Run(() =>
        {
            if (_store.Exists)
                throw new InvalidOperationException("A vault already exists at this location.");

            byte[] salt = new byte[16];
            RandomNumberGenerator.Fill(salt);
            var kdfParams = new KdfParameters
            {
                SaltB64 = Convert.ToBase64String(salt),
            };

            var pwBytes = SecureStringToUtf8Bytes(masterPassword);
            try
            {
                var key = _kdf.DeriveKey(pwBytes, kdfParams, 32);
                var vault = new Vault();
                var envelope = _cipher.Encrypt(VaultSerializer.SerializeVault(vault), key, kdfParams);
                _store.Save(envelope);

                _key?.Dispose();
                _key = key;
                _kdfParams = kdfParams;
                _vault = vault;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pwBytes);
            }
        });
    }

    public Task<bool> UnlockAsync(SecureString masterPassword)
    {
        return Task.Run(() =>
        {
            if (!_store.Exists)
                throw new InvalidOperationException("No vault to unlock.");

            var envelope = _store.Load();
            var pwBytes = SecureStringToUtf8Bytes(masterPassword);
            SecureBuffer? candidateKey = null;
            try
            {
                candidateKey = _kdf.DeriveKey(pwBytes, envelope.Kdf, 32);
                byte[] plaintext;
                try
                {
                    plaintext = _cipher.Decrypt(envelope, candidateKey);
                }
                catch (CryptographicException)
                {
                    candidateKey.Dispose();
                    return false;
                }

                var vault = VaultSerializer.DeserializeVault(plaintext);
                CryptographicOperations.ZeroMemory(plaintext);

                _key?.Dispose();
                _key = candidateKey;
                _kdfParams = envelope.Kdf;
                _vault = vault;
                return true;
            }
            finally
            {
                CryptographicOperations.ZeroMemory(pwBytes);
            }
        });
    }

    public Task SaveAsync()
    {
        return Task.Run(() =>
        {
            if (_vault is null || _key is null || _kdfParams is null)
                throw new InvalidOperationException("Vault is locked.");

            // Reuse stored KDF params (and therefore salt) so users don't need to re-derive on every save.
            byte[] plaintext = VaultSerializer.SerializeVault(_vault);
            try
            {
                var envelope = _cipher.Encrypt(plaintext, _key, _kdfParams);
                _store.Save(envelope);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plaintext);
            }
        });
    }

    public void Lock()
    {
        _key?.Dispose();
        _key = null;
        _vault = null;
        _kdfParams = null;
    }

    public void Dispose() => Lock();

    private static byte[] SecureStringToUtf8Bytes(SecureString s)
    {
        if (s is null || s.Length == 0) return Array.Empty<byte>();
        IntPtr bstr = Marshal.SecureStringToBSTR(s);
        try
        {
            int len = Marshal.ReadInt32(bstr, -4) / 2; // BSTR length is UTF-16 chars
            char[] chars = new char[len];
            Marshal.Copy(bstr, chars, 0, len);
            try
            {
                byte[] bytes = System.Text.Encoding.UTF8.GetBytes(chars);
                Array.Clear(chars);
                return bytes;
            }
            catch
            {
                Array.Clear(chars);
                throw;
            }
        }
        finally
        {
            Marshal.ZeroFreeBSTR(bstr);
        }
    }
}
