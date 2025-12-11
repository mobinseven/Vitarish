using Microsoft.Extensions.Configuration;

namespace Vitarish.Files.Tests;

/// <summary>
/// Unit tests for the <see cref="MinioOptions"/> class.
/// </summary>
public sealed class MinioOptionsTests
{
    [Fact]
    public void MinioOptions_WithFullConnectionString_ParsesAllProperties()
    {
        // Arrange
        var connectionString = "Endpoint=minio.example.com;Port=9000;UseSsl=true;AccessKey=myAccessKey;SecretKey=mySecretKey;BucketName=myBucket;Region=us-east-1;ObjectPrefix=uploads";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.Endpoint.Should().Be("minio.example.com");
        options.Port.Should().Be(9000);
        options.UseSsl.Should().BeTrue();
        options.AccessKey.Should().Be("myAccessKey");
        options.SecretKey.Should().Be("mySecretKey");
        options.BucketName.Should().Be("myBucket");
        options.Region.Should().Be("us-east-1");
        options.ObjectPrefix.Should().Be("uploads");
        options.HttpRequestTimeoutSeconds.Should().Be(100);
    }

    [Fact]
    public void MinioOptions_WithMinimalConnectionString_ParsesRequiredProperties()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;AccessKey=minioadmin;SecretKey=minioadmin;BucketName=test";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.Endpoint.Should().Be("localhost");
        options.AccessKey.Should().Be("minioadmin");
        options.SecretKey.Should().Be("minioadmin");
        options.BucketName.Should().Be("test");
    }

    [Fact]
    public void MinioOptions_DefaultPort_Is443()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;AccessKey=admin;SecretKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.Port.Should().Be(443);
    }

    [Fact]
    public void MinioOptions_DefaultUseSsl_IsTrue()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;AccessKey=admin;SecretKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.UseSsl.Should().BeTrue();
    }

    [Fact]
    public void MinioOptions_DefaultHttpRequestTimeoutSeconds_Is100()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;AccessKey=admin;SecretKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.HttpRequestTimeoutSeconds.Should().Be(100);
    }

    [Fact]
    public void MinioOptions_WhenUseSslFalse_ParsesCorrectly()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;Port=9000;UseSsl=false;AccessKey=admin;SecretKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.UseSsl.Should().BeFalse();
    }

    [Fact]
    public void MinioOptions_WhenCustomTimeoutProvided_ParsesCorrectly()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;AccessKey=admin;SecretKey=admin;BucketName=bucket;HttpRequestTimeoutSeconds=45";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.HttpRequestTimeoutSeconds.Should().Be(45);
    }

    [Fact]
    public void MinioOptions_WithoutOptionalProperties_HasNullValues()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;AccessKey=admin;SecretKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.Region.Should().BeNull();
        options.ObjectPrefix.Should().BeNull();
    }

    [Fact]
    public void MinioOptions_WithCustomPort_ParsesCorrectly()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;Port=9001;AccessKey=admin;SecretKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.Port.Should().Be(9001);
    }

    [Fact]
    public void MinioOptions_WithInvalidPort_KeepsDefaultPort()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;Port=invalid;AccessKey=admin;SecretKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.Port.Should().Be(443);
    }

    [Fact]
    public void MinioOptions_WhenConnectionStringMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var act = () => new MinioOptions(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Connection string 'Minio' not found.");
    }

    [Fact]
    public void MinioOptions_WithCustomName_UsesCorrectConnectionString()
    {
        // Arrange
        var connectionString = "Endpoint=custom.example.com;AccessKey=admin;SecretKey=admin;BucketName=bucket";
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:CustomMinio"] = connectionString
            })
            .Build();

        // Act
        var options = new MinioOptions(configuration, "CustomMinio");

        // Assert
        options.Endpoint.Should().Be("custom.example.com");
    }

    [Fact]
    public void MinioOptions_WhenCustomNameMissing_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>())
            .Build();

        // Act
        var act = () => new MinioOptions(configuration, "NonExistent");

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Connection string 'NonExistent' not found.");
    }

    [Fact]
    public void MinioOptions_WithEmptyConnectionString_ThrowsInvalidOperationException()
    {
        // Arrange
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Minio"] = ""
            })
            .Build();

        // Act
        var act = () => new MinioOptions(configuration);

        // Assert
        act.Should().Throw<InvalidOperationException>()
           .WithMessage("Connection string 'Minio' not found.");
    }

    [Theory]
    [InlineData("localhost")]
    [InlineData("minio.example.com")]
    [InlineData("192.168.1.100")]
    [InlineData("minio-service.namespace.svc.cluster.local")]
    public void MinioOptions_AcceptsVariousEndpointFormats(string endpoint)
    {
        // Arrange
        var connectionString = $"Endpoint={endpoint};AccessKey=admin;SecretKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.Endpoint.Should().Be(endpoint);
    }

    [Theory]
    [InlineData(80)]
    [InlineData(443)]
    [InlineData(9000)]
    [InlineData(9001)]
    [InlineData(3000)]
    public void MinioOptions_AcceptsVariousPorts(int port)
    {
        // Arrange
        var connectionString = $"Endpoint=localhost;Port={port};AccessKey=admin;SecretKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.Port.Should().Be(port);
    }

    [Theory]
    [InlineData("us-east-1")]
    [InlineData("eu-west-1")]
    [InlineData("ap-southeast-1")]
    [InlineData("custom-region")]
    public void MinioOptions_AcceptsVariousRegions(string region)
    {
        // Arrange
        var connectionString = $"Endpoint=localhost;AccessKey=admin;SecretKey=admin;BucketName=bucket;Region={region}";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.Region.Should().Be(region);
    }

    [Theory]
    [InlineData("uploads")]
    [InlineData("files/images")]
    [InlineData("tenant-123/documents")]
    public void MinioOptions_AcceptsVariousObjectPrefixes(string prefix)
    {
        // Arrange
        var connectionString = $"Endpoint=localhost;AccessKey=admin;SecretKey=admin;BucketName=bucket;ObjectPrefix={prefix}";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.ObjectPrefix.Should().Be(prefix);
    }

    [Fact]
    public void MinioOptions_WithMissingEndpoint_ReturnsEmptyString()
    {
        // Arrange
        var connectionString = "AccessKey=admin;SecretKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.Endpoint.Should().BeEmpty();
    }

    [Fact]
    public void MinioOptions_WithMissingAccessKey_ReturnsEmptyString()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;SecretKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.AccessKey.Should().BeEmpty();
    }

    [Fact]
    public void MinioOptions_WithMissingSecretKey_ReturnsEmptyString()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;AccessKey=admin;BucketName=bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.SecretKey.Should().BeEmpty();
    }

    [Fact]
    public void MinioOptions_WithMissingBucketName_ReturnsEmptyString()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;AccessKey=admin;SecretKey=admin";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.BucketName.Should().BeEmpty();
    }

    [Fact]
    public void MinioOptions_CaseInsensitivePropertyNames()
    {
        // Arrange
        var connectionString = "endpoint=localhost;port=9000;usessl=false;accesskey=admin;secretkey=admin;bucketname=bucket;region=us-east-1;objectprefix=prefix";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.Endpoint.Should().Be("localhost");
        options.Port.Should().Be(9000);
        options.UseSsl.Should().BeFalse();
        options.AccessKey.Should().Be("admin");
        options.SecretKey.Should().Be("admin");
        options.BucketName.Should().Be("bucket");
        options.Region.Should().Be("us-east-1");
        options.ObjectPrefix.Should().Be("prefix");
    }

    [Fact]
    public void MinioOptions_WithSpecialCharactersInValues_ParsesCorrectly()
    {
        // Arrange
        var connectionString = "Endpoint=localhost;AccessKey=admin+key;SecretKey=secret/key=123;BucketName=my-bucket";
        var configuration = CreateConfiguration(connectionString);

        // Act
        var options = new MinioOptions(configuration);

        // Assert
        options.AccessKey.Should().Be("admin+key");
        options.SecretKey.Should().Be("secret/key=123");
        options.BucketName.Should().Be("my-bucket");
    }

    private static IConfiguration CreateConfiguration(string connectionString)
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Minio"] = connectionString
            })
            .Build();
    }
}
