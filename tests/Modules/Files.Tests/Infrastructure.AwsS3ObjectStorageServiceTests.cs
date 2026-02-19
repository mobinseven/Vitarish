using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using System.Net;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Services;

namespace Files.Tests;

public sealed class Infrastructure_AwsS3ObjectStorageServiceTests
{
    [Fact]
    public async Task EnsureBucketAsync_WhenBucketDoesNotExist_CreatesBucket()
    {
        var s3Mock = new Mock<IAmazonS3>();
        _ = s3Mock
            .Setup(x => x.GetBucketAclAsync(It.IsAny<GetBucketAclRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("missing") { StatusCode = HttpStatusCode.NotFound });
        _ = s3Mock
            .Setup(x => x.PutBucketAsync(It.IsAny<PutBucketRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PutBucketResponse());

        AwsS3ObjectStorageService sut = CreateSut(s3Mock.Object);
        await sut.EnsureBucketAsync();

        s3Mock.Verify(x => x.PutBucketAsync(
            It.Is<PutBucketRequest>(r => r.BucketName == "bucket"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExistsAsync_WhenObjectIsMissing_ReturnsFalse()
    {
        var s3Mock = new Mock<IAmazonS3>();
        _ = s3Mock
            .Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("missing") { StatusCode = HttpStatusCode.NotFound, ErrorCode = "NoSuchKey" });

        AwsS3ObjectStorageService sut = CreateSut(s3Mock.Object);
        bool exists = await sut.ExistsAsync("missing.txt");

        _ = exists.Should().BeFalse();
    }

    private static AwsS3ObjectStorageService CreateSut(IAmazonS3 client)
    {
        var minioOptionsMock = new Mock<IOptionsMonitor<MinioOptions>>();
        _ = minioOptionsMock.SetupGet(x => x.CurrentValue).Returns(new MinioOptions
        {
            BucketName = "bucket"
        });
        return new AwsS3ObjectStorageService(client, minioOptionsMock.Object);
    }
}
