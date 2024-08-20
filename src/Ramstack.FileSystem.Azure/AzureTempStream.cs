namespace Ramstack.FileSystem.Azure;

/// <summary>
/// Represents a temporary write-only stream for Azure Blob storage operations, redirecting all write operations to a temporary file.
/// Upon disposing or closing the stream, the data is transferred to the Azure Blob storage.
/// </summary>
internal sealed class AzureTempStream : Stream
{
    private readonly AzureFile _file;
    private readonly FileStream _fileStream = CreateTempFileStream();
    private bool _disposed;

    /// <inheritdoc />
    public override bool CanRead => _fileStream.CanRead;

    /// <inheritdoc />
    public override bool CanSeek => _fileStream.CanSeek;

    /// <inheritdoc />
    public override bool CanWrite => _fileStream.CanWrite;

    /// <inheritdoc />
    public override long Length => _fileStream.Length;

    /// <inheritdoc />
    public override bool CanTimeout => _fileStream.CanTimeout;

    /// <inheritdoc />
    public override long Position
    {
        get => _fileStream.Position;
        set => _fileStream.Position = value;
    }

    /// <inheritdoc />
    public override int ReadTimeout
    {
        get => _fileStream.ReadTimeout;
        set => _fileStream.ReadTimeout = value;
    }

    /// <inheritdoc />
    public override int WriteTimeout
    {
        get => _fileStream.WriteTimeout;
        set => _fileStream.WriteTimeout = value;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="AzureTempStream"/> class.
    /// </summary>
    /// <param name="file">The <see cref="AzureFile"/> instance.</param>
    public AzureTempStream(AzureFile file) =>
        _file = file;

    /// <inheritdoc />
    public override void CopyTo(Stream destination, int bufferSize) =>
        _fileStream.CopyTo(destination, bufferSize);

    /// <inheritdoc />
    public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken) =>
        _fileStream.CopyToAsync(destination, bufferSize, cancellationToken);

    /// <inheritdoc />
    public override IAsyncResult BeginRead(byte[] array, int offset, int numBytes, AsyncCallback? callback, object? state) =>
        _fileStream.BeginRead(array, offset, numBytes, callback, state);

    /// <inheritdoc />
    public override IAsyncResult BeginWrite(byte[] array, int offset, int numBytes, AsyncCallback? callback, object? state) =>
        _fileStream.BeginWrite(array, offset, numBytes, callback, state);

    /// <inheritdoc />
    public override int EndRead(IAsyncResult asyncResult) =>
        _fileStream.EndRead(asyncResult);

    /// <inheritdoc />
    public override void EndWrite(IAsyncResult asyncResult) =>
        _fileStream.EndWrite(asyncResult);

    /// <inheritdoc />
    public override int Read(byte[] array, int offset, int count) =>
        _fileStream.Read(array, offset, count);

    /// <inheritdoc />
    public override int Read(Span<byte> buffer) =>
        _fileStream.Read(buffer);

    /// <inheritdoc />
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _fileStream.ReadAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc />
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default) =>
        _fileStream.ReadAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public override int ReadByte() =>
        _fileStream.ReadByte();

    /// <inheritdoc />
    public override void Write(byte[] array, int offset, int count) =>
        _fileStream.Write(array, offset, count);

    /// <inheritdoc />
    public override void Write(ReadOnlySpan<byte> buffer) =>
        _fileStream.Write(buffer);

    /// <inheritdoc />
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken) =>
        _fileStream.WriteAsync(buffer, offset, count, cancellationToken);

    /// <inheritdoc />
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default) =>
        _fileStream.WriteAsync(buffer, cancellationToken);

    /// <inheritdoc />
    public override void WriteByte(byte value) =>
        _fileStream.WriteByte(value);

    /// <inheritdoc />
    public override long Seek(long offset, SeekOrigin origin) =>
        _fileStream.Seek(offset, origin);

    /// <inheritdoc />
    public override void SetLength(long value) =>
        _fileStream.SetLength(value);

    /// <inheritdoc />
    public override void Flush() =>
        _fileStream.Flush();

    /// <inheritdoc />
    public override Task FlushAsync(CancellationToken cancellationToken) =>
        _fileStream.FlushAsync(cancellationToken);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing && !_disposed)
            Task.Run(async () => await DisposeAsync()).GetAwaiter().GetResult();

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (!_disposed)
        {
            _disposed = true;
            _fileStream.Position = 0;
            await _file.WriteAsync(_fileStream, overwrite: true);
            await _fileStream.DisposeAsync();
        }

        await base.DisposeAsync();
    }

    private static FileStream CreateTempFileStream()
    {
        const int bufferSize = 4096;
        const FileOptions options = FileOptions.DeleteOnClose | FileOptions.Asynchronous;

        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        return new FileStream(path, FileMode.Create, FileAccess.ReadWrite, FileShare.Read, bufferSize, options);
    }
}
