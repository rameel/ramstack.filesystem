using System.Diagnostics.CodeAnalysis;

namespace Ramstack.FileSystem.Google;

/// <summary>
/// Represents a temporary write-only stream that buffers data to a temporary file before uploading it to Google Cloud Storage.
/// Data is committed to the storage bucket when the stream is disposed or closed.
/// </summary>
internal sealed class GcsWriteStream : Stream
{
    private readonly GoogleFileSystem _fs;
    private readonly string _objectName;
    private readonly FileStream _stream;
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
        _stream = new FileStream(
            Path.Combine(
                Path.GetTempPath(),
                Path.GetRandomFileName()),
            FileMode.CreateNew,
            FileAccess.ReadWrite,
            FileShare.None,
            bufferSize: 4096,
            FileOptions.DeleteOnClose
                | FileOptions.Asynchronous);
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
            _stream.Close();
            throw;
        }
    }

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        WriteAsync(buffer.AsMemory(offset, count), cancellationToken).AsTask();

    /// <inheritdoc />
    public override async ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
    {
        try
        {
            await _stream.WriteAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            _disposed = true;
            _stream.Close();
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
    public override void Flush()
    {
    }

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        Task.CompletedTask;

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            try
            {
                _stream.Position = 0;

                var destination = new global::Google.Apis.Storage.v1.Data.Object { Bucket = _fs.BucketName, Name = _objectName };
                _fs.StorageClient.UploadObject(destination, _stream);
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

                var destination = new global::Google.Apis.Storage.v1.Data.Object { Bucket = _fs.BucketName, Name = _objectName };

                await _fs.StorageClient
                    .UploadObjectAsync(destination, _stream)
                    .ConfigureAwait(false);
            }
            finally
            {
                _disposed = true;

                await _stream.DisposeAsync().ConfigureAwait(false);
                await base.DisposeAsync().ConfigureAwait(false);
            }
        }
    }

    [DoesNotReturn]
    private static void Error_NotSupported() =>
        throw new NotSupportedException();
}
