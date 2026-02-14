using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Moq.Protected;
using Minio;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Api;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Vitarish.Entities.Server.Models;
using Vitarish.Files.Application.Abstractions;

namespace Vitarish.Files.Tests;

public sealed class Infrastructure_FilesApiTests
{
    [Fact]
    public async Task GetFileById_NonExistentFile_Returns404()
    {
        // Arrange
        using var host = CreateHost();
        var client = host.GetTestClient();
        var fileId = Guid.NewGuid();

        // Act
        var response = await client.GetAsync($"/files/{fileId}");

        // Assert
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFileById_InvalidGuid_ReturnsNotFound()
    {
        // Arrange
        using var host = CreateHost();
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/files/not-a-guid");

        // Assert
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFileById_ExistingFile_Returns200()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var fileInfo = new FileInformationDto
        {
            Id = fileId,
            ContentType = "text/plain",
            Metadata = new FileMetadata { Extension = "txt" }
        };

        using var host = CreateHost(fileInfo);
        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync($"/files/{fileId}");

        // Assert
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task HeadRequest_ExistingFile_Returns200WithoutBody()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var fileInfo = new FileInformationDto
        {
            Id = fileId,
            ContentType = "text/plain",
            Metadata = new FileMetadata { Extension = "txt" }
        };

        using var host = CreateHost(fileInfo);
        var client = host.GetTestClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Head, $"/files/{fileId}");
        var response = await client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        _ = response.Content.Headers.ContentLength.Should().Be(0);
    }

    [Fact]
    public async Task HeadRequest_NonExistentFile_Returns404()
    {
        // Arrange
        using var host = CreateHost();
        var client = host.GetTestClient();
        var fileId = Guid.NewGuid();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Head, $"/files/{fileId}");
        var response = await client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFileById_WithRangeHeader_ForwardsRangeRequest()
    {
        // Arrange
        var fileId = Guid.NewGuid();
        var fileInfo = new FileInformationDto
        {
            Id = fileId,
            ContentType = "video/mp4",
            Metadata = new FileMetadata { Extension = "mp4" }
        };

        HttpRequestMessage? capturedRequest = null;
        using var host = CreateHost(fileInfo, req => capturedRequest = req);
        var client = host.GetTestClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/files/{fileId}");
        request.Headers.Add("Range", "bytes=5-11");
        var response = await client.SendAsync(request);

        // Assert
        _ = capturedRequest.Should().NotBeNull();
        _ = capturedRequest!.Headers.Contains("Range").Should().BeTrue();
        var rangeHeader = capturedRequest.Headers.GetValues("Range").FirstOrDefault();
        _ = rangeHeader.Should().Be("bytes=5-11");
    }

    private static IHost CreateHost(FileInformationDto? fileInfo = null, Action<HttpRequestMessage>? captureRequest = null)
    {
        var minioClientMock = new Mock<IMinioClient>();
        minioClientMock
            .Setup(m => m.PresignedGetObjectAsync(It.IsAny<Minio.DataModel.Args.PresignedGetObjectArgs>()))
            .ReturnsAsync("http://minio.test/bucket/file");

        var httpClientFactoryMock = new Mock<IHttpClientFactory>();
        var httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync((HttpRequestMessage req, CancellationToken ct) =>
            {
                captureRequest?.Invoke(req);
                return new HttpResponseMessage
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new ByteArrayContent(Array.Empty<byte>())
                };
            });

        var httpClient = new HttpClient(httpMessageHandlerMock.Object);
        httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(minioClientMock.Object);
                        services.AddSingleton(httpClientFactoryMock.Object);

                        var tenantAccessor = new TestTenantAccessor("root");
                        services.AddSingleton<IMultiTenantContextAccessor<TenantInfo>>(tenantAccessor);
                        services.AddSingleton<IMultiTenantContextSetter>(tenantAccessor);

                        var fileInfoService = new InMemoryFileInformationService();
                        if (fileInfo is not null)
                        {
                            fileInfoService.Seed("test-file", fileInfo);
                        }
                        services.AddScoped<IFileInformationService>(_ => fileInfoService);

                        services.AddSingleton<IServiceScopeFactory>(sp => sp.GetRequiredService<IServiceScopeFactory>());

                        services.AddSingleton<IOptionsSnapshot<FilesOptions>>(new OptionsWrapper<FilesOptions>(
                            new FilesOptions
                            {
                                Prefix = "files",
                                BaseUrl = "https://files.example",
                                UploadPath = "uploads"
                            }));

                        services.AddSingleton<IOptionsSnapshot<MinioOptions>>(new OptionsWrapper<MinioOptions>(
                            new MinioOptions
                            {
                                Endpoint = "minio.example",
                                AccessKey = "key",
                                SecretKey = "secret",
                                BucketName = "bucket"
                            }));

                        services.AddRouting();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapFilesApi();
                        });
                    });
            })
            .Start();
    }

    private sealed class OptionsWrapper<T> : IOptionsSnapshot<T> where T : class
    {
        public OptionsWrapper(T value) => Value = value;
        public T Value { get; }
        public T Get(string? name) => Value;
    }
}
