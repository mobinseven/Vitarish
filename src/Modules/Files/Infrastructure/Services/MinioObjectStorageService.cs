using Amazon.S3;
using Amazon.S3.Model;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Options;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Interfaces;

namespace Vitarish.Files.Infrastructure.Services;

public sealed class AwsS3ObjectStorageService(
    IAmazonS3 client,
    IOptionsMonitor<MinioOptions> minioOptions) : IObjectStorageService
{
    private MinioOptions options => minioOptions.CurrentValue;

    public async Task EnsureBucketAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await client.GetBucketAclAsync(new GetBucketAclRequest
            {
                BucketName = options.BucketName
            }, cancellationToken);
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            PutBucketRequest request = new()
            {
                BucketName = options.BucketName
            };
            _ = await client.PutBucketAsync(request, cancellationToken);
        }
    }

    public async Task RenameAsync(
        string objectKey,
        string newObjectKey,
        CancellationToken cancellationToken =default)
    {
        CopyObjectRequest request = new()
        {
            SourceBucket = options.BucketName,
            SourceKey = objectKey,
            DestinationBucket = options.BucketName,
            DestinationKey = newObjectKey
        };
        _ = await client.CopyObjectAsync(request, cancellationToken);
        await RemoveAsync(objectKey, cancellationToken);
    }

    public async Task UploadAsync(
        string objectKey,
        string contentType,
        Stream data,
        long size,
        CancellationToken cancellationToken = default)
    {
        if (data.CanSeek)
        {
            _ = data.Seek(0, SeekOrigin.Begin);
        }

        TransferUtilityUploadRequest request = new()
        {
            BucketName = options.BucketName,
            Key = objectKey,
            InputStream = data,
            ContentType = contentType,
            AutoCloseStream = false
        };

        TransferUtility transferUtility = new(client);
        await transferUtility.UploadAsync(request, cancellationToken);
    }

    public async Task<bool> ExistsAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        try
        {
            _ = await GetMetadataAsync(objectKey, cancellationToken);
            return true;
        }
        catch (AmazonS3Exception ex) when (
            ex.StatusCode == System.Net.HttpStatusCode.NotFound ||
            string.Equals(ex.ErrorCode, "NotFound", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(ex.ErrorCode, "NoSuchKey", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }
    }

    public async Task<ObjectMetadata> GetMetadataAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        GetObjectMetadataResponse metadata = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
        {
            BucketName = options.BucketName,
            Key = objectKey
        }, cancellationToken);
        return new ObjectMetadata(metadata.ContentLength, metadata.Headers.ContentType);
    }

    public async Task<Stream> DownloadAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        MemoryStream buffer = new();
        using GetObjectResponse response = await client.GetObjectAsync(new GetObjectRequest
        {
            BucketName = options.BucketName,
            Key = objectKey
        }, cancellationToken);
        await response.ResponseStream.CopyToAsync(buffer, cancellationToken);
        _ = buffer.Seek(0, SeekOrigin.Begin);
        return buffer;
    }
    public async Task StreamToAsync
        (
            string objectKey,
            Stream destination,
            long? offset = null,
            long? length = null,
            CancellationToken cancellationToken = default
        )
    {
        GetObjectRequest request = new()
        {
            BucketName = options.BucketName,
            Key = objectKey
        };

        if (offset is not null || length is not null)
        {
            ObjectMetadata metadata = await GetMetadataAsync(objectKey, cancellationToken);
            long resolvedOffset = Math.Max(0, offset ?? 0);
            if (resolvedOffset >= metadata.Size)
            {
                return;
            }

            long resolvedLength = length is null
                ? metadata.Size - resolvedOffset
                : Math.Max(0, Math.Min(length.Value, metadata.Size - resolvedOffset));
            if (resolvedLength <= 0)
            {
                return;
            }

            long resolvedEnd = resolvedOffset + resolvedLength - 1;
            request.ByteRange = new ByteRange(resolvedOffset, resolvedEnd);
        }

        using GetObjectResponse response = await client.GetObjectAsync(request, cancellationToken);
        await response.ResponseStream.CopyToAsync(destination, cancellationToken);
    }

    public async Task RemoveAsync(string objectKey, CancellationToken cancellationToken = default)
    {
        _ = await client.DeleteObjectAsync(new DeleteObjectRequest
        {
            BucketName = options.BucketName,
            Key = objectKey
        }, cancellationToken);
    }

    public async Task<ObjectListResult> ListAsync(
        string? prefix = null,
        string? continuationToken = null,
        int? maxKeys = null,
        CancellationToken cancellationToken = default)
    {
        ListObjectsV2Request request = new()
        {
            BucketName = options.BucketName,
            Prefix = prefix,
            ContinuationToken = continuationToken,
            MaxKeys = maxKeys
        };
        ListObjectsV2Response response = await client.ListObjectsV2Async(request, cancellationToken);
        IReadOnlyList<ObjectListItem> items = response.S3Objects
            .Select(o => new ObjectListItem(
                o.Key,
                o.Size ?? 0,
                o.LastModified.HasValue ? new DateTimeOffset(o.LastModified.Value) : DateTimeOffset.MinValue,
                o.ETag))
            .ToList();
        return new ObjectListResult(items, response.NextContinuationToken, response.IsTruncated ?? false);
    }

    public Task<string> GetPresignedUrlAsync(
        string objectKey,
        TimeSpan expiresIn,
        string httpMethod = "GET",
        CancellationToken cancellationToken = default)
    {
        HttpVerb verb = httpMethod.ToUpperInvariant() switch
        {
            "PUT" => HttpVerb.PUT,
            "DELETE" => HttpVerb.DELETE,
            "HEAD" => HttpVerb.HEAD,
            _ => HttpVerb.GET
        };
        GetPreSignedUrlRequest request = new()
        {
            BucketName = options.BucketName,
            Key = objectKey,
            Expires = DateTime.UtcNow.Add(expiresIn),
            Verb = verb
        };
        return Task.FromResult(client.GetPreSignedURL(request));
    }
}
