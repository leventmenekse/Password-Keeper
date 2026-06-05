using PasswordKeeper.Core.Models;

namespace PasswordKeeper.Core.Vault;

public sealed class FileVaultStore : IVaultStore
{
    private readonly string _path;
    private readonly string _tempPath;
    private readonly string _backupPath;

    public FileVaultStore(string vaultPath)
    {
        _path = vaultPath;
        _tempPath = vaultPath + ".tmp";
        _backupPath = vaultPath + ".bak";
    }

    public string VaultPath => _path;

    public bool Exists => File.Exists(_path) || File.Exists(_backupPath);

    public VaultEnvelope Load()
    {
        if (File.Exists(_path))
        {
            try
            {
                return VaultSerializer.DeserializeEnvelope(File.ReadAllBytes(_path));
            }
            catch (Exception) when (File.Exists(_backupPath))
            {
                // Primary file is unreadable; fall through to backup
            }
        }

        if (File.Exists(_backupPath))
        {
            return VaultSerializer.DeserializeEnvelope(File.ReadAllBytes(_backupPath));
        }

        throw new FileNotFoundException("No vault file found.", _path);
    }

    public void Save(VaultEnvelope envelope)
    {
        string? dir = Path.GetDirectoryName(_path);
        if (!string.IsNullOrEmpty(dir)) Directory.CreateDirectory(dir);

        byte[] bytes = VaultSerializer.SerializeEnvelope(envelope);

        using (var fs = new FileStream(
            _tempPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.None,
            bufferSize: 4096,
            options: FileOptions.WriteThrough))
        {
            fs.Write(bytes, 0, bytes.Length);
            fs.Flush(flushToDisk: true);
        }

        if (File.Exists(_path))
        {
            File.Replace(_tempPath, _path, _backupPath);
        }
        else
        {
            File.Move(_tempPath, _path);
        }
    }
}
