using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace PasswordKeeper.Core.Crypto;

public sealed class SecureBuffer : IDisposable
{
    private readonly byte[] _buffer;
    private GCHandle _handle;
    private bool _disposed;

    public SecureBuffer(int length)
    {
        _buffer = new byte[length];
        _handle = GCHandle.Alloc(_buffer, GCHandleType.Pinned);
    }

    public SecureBuffer(byte[] source) : this(source.Length)
    {
        source.AsSpan().CopyTo(_buffer);
    }

    public int Length => _buffer.Length;

    public Span<byte> AsSpan()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _buffer;
    }

    public ReadOnlySpan<byte> AsReadOnlySpan()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return _buffer;
    }

    public bool IsZeroed
    {
        get
        {
            for (int i = 0; i < _buffer.Length; i++)
            {
                if (_buffer[i] != 0) return false;
            }
            return true;
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        CryptographicOperations.ZeroMemory(_buffer);
        if (_handle.IsAllocated) _handle.Free();
        _disposed = true;
    }
}
