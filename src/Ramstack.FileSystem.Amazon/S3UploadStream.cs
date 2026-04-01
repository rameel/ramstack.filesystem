using System.Diagnostics.CodeAnalysis;

using Amazon.S3;
using Amazon.S3.Model;

using Ramstack.FileSystem.Amazon.Utilities;

namespace Ramstack.FileSystem.Amazon;

/// <summary>
/// Represents a stream for uploading data to Amazon S3 using multipart upload.
/// </summary>
internal sealed class S3UploadStream : Stream
{
    // https://docs.aws.amazon.com/AmazonS3/latest/userguide/qfacts.html
    private const long MinPartSize = 5L * 1024 * 1024;

    private readonly IAmazonS3 _client;
    private readonly string _bucketName;
    private readonly string _key;
    private readonly string _uploadId;
    private readonly FileStream _stream;
    private readonly List<PartETag> _partETags;

    private volatile int _disposed;

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
    /// Initializes a new instance of the <see cref="S3UploadStream"/> class.
    /// </summary>
    /// <param name="client">The Amazon S3 client used for uploading parts.</param>
    /// <param name="bucketName">The name of the S3 bucket where the data will be uploaded.</param>
    /// <param name="key">The key (path) in the S3 bucket where the data will be stored.</param>
    /// <param name="uploadId">The multipart upload session identifier.</param>
    public S3UploadStream(IAmazonS3 client, string bucketName, string key, string uploadId)
    {
        _client = client;
        _bucketName = bucketName;
        _key = key;
        _uploadId = uploadId;
        _partETags = [];

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
    public override int Read(byte[] buffer, int offset, int count)
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
            if (_stream.Length >= MinPartSize)
                UploadPart();
        }
        catch
        {
            Abort();
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
            if (_stream.Length >= MinPartSize)
                await UploadPartAsync(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            await AbortAsync(cancellationToken).ConfigureAwait(false);
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
        if (disposing)
        {
            using var scope = NullSynchronizationContext.CreateScope();
            DisposeAsync().AsTask().Wait();
        }

        base.Dispose(disposing);
    }

    /// <inheritdoc />
    public override async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            await UploadPartAsync(CancellationToken.None).ConfigureAwait(false);

            var request = new CompleteMultipartUploadRequest
            {
                BucketName = _bucketName,
                Key = _key,
                UploadId = _uploadId,
                PartETags = _partETags
            };

            await _client
                .CompleteMultipartUploadAsync(request)
                .ConfigureAwait(false);
        }
        catch
        {
            await AbortAsync(CancellationToken.None).ConfigureAwait(false);
            throw;
        }
        finally
        {
            try
            {
                await _stream.DisposeAsync().ConfigureAwait(false);
            }
            catch
            {
                // Ignore:
                // Errors when disposing the temporary buffer are not significant here, because:
                // 1) If the upload succeeded, the job is done; a cleanup error can be ignored.
                // 2) If the upload failed, preserving the original exception is more important for us.
            }
        }
    }

    /// <summary>
    /// Uploads the current buffer to Amazon S3 as a part of the multipart upload.
    /// This method blocks the calling thread and waits for the upload to complete.
    /// </summary>
    private void UploadPart()
    {
        using var scope = NullSynchronizationContext.CreateScope();
        UploadPartAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously uploads the current buffer to Amazon S3 as a part of the multipart upload.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation.
    /// </returns>
    private async ValueTask UploadPartAsync(CancellationToken cancellationToken)
    {
        _stream.Position = 0;

        // https://docs.aws.amazon.com/AmazonS3/latest/userguide/qfacts.html
        // The maximum allowed part size is 5 GiB.
        // -----------------------------------------------------------------------------------
        // We don't need to worry about S3's 5 GiB part limit because:
        // 1. All Write/WriteAsync methods are inherently limited by Array.MaxLength (~2 GiB).
        // 2. The upload starts as soon as the buffer reaches MinPartSize (5 MiB).
        // Even if a single write matches Array.MaxLength, the data is
        // uploaded immediately, staying within AWS limits.

        var request = new UploadPartRequest
        {
            BucketName = _bucketName,
            Key = _key,
            UploadId = _uploadId,
            PartNumber = _partETags.Count + 1,
            InputStream = _stream,
            PartSize = _stream.Length
        };

        var response = await _client
            .UploadPartAsync(request, cancellationToken)
            .ConfigureAwait(false);

        _partETags.Add(new PartETag(response));

        _stream.Position = 0;
        _stream.SetLength(0);
    }

    /// <summary>
    /// Aborts the multipart upload session.
    /// </summary>
    /// <remarks>
    /// This method sends a request to Amazon S3 to abort the multipart upload identified by the
    /// <see cref="_uploadId"/>. Once aborted, the upload cannot be resumed, and any uploaded parts
    /// will be deleted.
    /// </remarks>
    private void Abort()
    {
        using var scope = NullSynchronizationContext.CreateScope();
        AbortAsync(CancellationToken.None).AsTask().GetAwaiter().GetResult();
    }

    /// <summary>
    /// Asynchronously aborts the multipart upload session.
    /// </summary>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="ValueTask"/> that represents the asynchronous abort operation.
    /// </returns>
    /// <remarks>
    /// This method sends a request to Amazon S3 to abort the multipart upload identified by the
    /// <see cref="_uploadId"/>. Once aborted, the upload cannot be resumed, and any uploaded parts
    /// will be deleted.
    /// </remarks>
    private async ValueTask AbortAsync(CancellationToken cancellationToken)
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;

        try
        {
            await using (_stream.ConfigureAwait(false))
            {
                var request = new AbortMultipartUploadRequest
                {
                    BucketName = _bucketName,
                    Key = _key,
                    UploadId = _uploadId
                };

                await _client
                    .AbortMultipartUploadAsync(request, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch
        {
            // IGNORE:
            // Suppressing the exception during abort to preserve
            // the original exception that triggered the abort.
        }
    }

    /// <summary>
    /// Throws a <see cref="NotSupportedException"/>.
    /// </summary>
    [DoesNotReturn]
    private static void Error_NotSupported() =>
        throw new NotSupportedException();
}
