using System.Diagnostics.CodeAnalysis;

namespace Ramstack.FileSystem.Google;

/// <summary>
/// Represents a temporary write-only stream for Google Cloud Storage operations, redirecting all write operations to a temporary file.
/// Upon disposing or closing the stream, the data is transferred to the Azure Blob storage.
/// </summary>
internal sealed class GcsWriteStream : Stream
{
    private readonly GoogleFileSystem _fs;
    private readonly string _objectName;
    private readonly FileStream _stream = CreateTempFileStream();
    private bool _disposed;

    /// <inheritdoc />
    public override bool CanRead => false;

    /// <inheritdoc />
    public override bool CanSeek => false;

    /// <inheritdoc />
    public override bool CanWrite => true;

    /// <inheritdoc />
    public override long Length
    {
        get
        {
            Error_NotSupported();
            return 0;
        }
    }

    /// <inheritdoc />
    public override long Position
    {
        get
        {
            Error_NotSupported();
            return 0;
        }
        // ReSharper disable once ValueParameterNotUsed
        set => Error_NotSupported();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="GcsWriteStream"/> class.
    /// </summary>
    /// <param name="fs">The <see cref="GoogleFileSystem"/> instance.</param>
    /// <param name="objectName">The name of the object.</param>
    public GcsWriteStream(GoogleFileSystem fs, string objectName)
    {
        _fs = fs;
        _objectName = objectName;
    }

    /// <inheritdoc />
    public override int Read(byte[] array, int offset, int count)
    {
        Error_NotSupported();
        return 0;
    }

    /// <inheritdoc />
    public override int Read(Span<byte> buffer)
    {
        Error_NotSupported();
        return 0;
    }

    /// <inheritdoc />
    public override void Write(byte[] buffer, int offset, int count) =>
        Write(buffer.AsSpan(offset, count));

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer)
    {
        try
        {
            _stream.Write(buffer);
        }
        catch
        {
            _disposed = true;
            throw;
        }
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        try
        {
            return _stream.WriteAsync(buffer, cancellationToken);
        }
        catch
        {
            _disposed = true;
            throw;
        }
    }

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin)
    {
        Error_NotSupported();
        return 0;
    }

    /// <inheritdoc />
    public override void SetLength(long value) =>
        Error_NotSupported();

    /// <inheritdoc />
    public override void Flush() =>
        _stream.Flush();

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _stream.FlushAsync(cancellationToken);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _stream.Position = 0;

                if (_stream.Length != 0)
                {
                    var destination = new global::Google.Apis.Storage.v1.Data.Object { Bucket = _fs.BucketName, Name = _objectName };
                    _fs.StorageClient.UploadObject(destination, _stream);
                }
            }
            finally
            {
                _disposed = true;
                _stream.Close();

                base.Dispose(disposing);
            }
        }
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            try
            {
                _stream.Position = 0;

                if (_stream.Length != 0)
                {
                    var destination = new global::Google.Apis.Storage.v1.Data.Object { Bucket = _fs.BucketName, Name = _objectName };

                    await _fs.StorageClient
                        .UploadObjectAsync(destination, _stream)
                        .ConfigureAwait(false);
                }
            }
            finally
            {
                _disposed = true;

                await _stream.DisposeAsync().ConfigureAwait(false);
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    private static FileStream CreateTempFileStream()
    {
        const int BufferSize = 4096;
        const FileOptions Options = FileOptions.DeleteOnClose | FileOptions.Asynchronous;

        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, BufferSize, Options);
    }

    [DoesNotReturn]
    private static void Error_NotSupported() =>
        throw new NotSupportedException();
}
