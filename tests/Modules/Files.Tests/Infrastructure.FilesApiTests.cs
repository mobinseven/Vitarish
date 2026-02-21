using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Infrastructure.Api;

namespace Files.Tests;

public sealed class Infrastructure_FilesApiTests
{
    [Fact]
    public async Task GetFileById_NonExistentFile_Returns404()
    {
        // Arrange
        using IHost host = CreateHost();
        HttpClient client = host.GetTestClient();
        Guid fileId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await client.GetAsync($"/files/{fileId}");

        // Assert
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFileById_InvalidGuid_ReturnsNotFound()
    {
        // Arrange
        using IHost host = CreateHost();
        HttpClient client = host.GetTestClient();

        // Act
        HttpResponseMessage response = await client.GetAsync("/files/not-a-guid");

        // Assert
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFileById_ExistingFile_Returns200()
    {
        // Arrange
        Guid fileId = Guid.NewGuid();
        FileInformationReadDto fileInfo = new()
        {
            Id = fileId,
            ContentType = "text/plain",
            Metadata = new FileMetadata { Extension = "txt" }
        };

        using IHost host = CreateHost(fileInfo);
        HttpClient client = host.GetTestClient();

        // Act
        HttpResponseMessage response = await client.GetAsync($"/files/{fileId}");

        // Assert
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
    }

    [Fact]
    public async Task HeadRequest_ExistingFile_Returns200WithoutBody()
    {
        // Arrange
        Guid fileId = Guid.NewGuid();
        FileInformationReadDto fileInfo = new()
        {
            Id = fileId,
            ContentType = "text/plain",
            Metadata = new FileMetadata { Extension = "txt" }
        };

        using IHost host = CreateHost(fileInfo);
        HttpClient client = host.GetTestClient();

        // Act
        HttpRequestMessage request = new(HttpMethod.Head, $"/files/{fileId}");
        HttpResponseMessage response = await client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.OK);
        _ = response.Content.Headers.ContentLength.Should().Be(1000);
        var content = await response.Content.ReadAsStringAsync();
        _ = content.Should().BeEmpty();
    }

    [Fact]
    public async Task HeadRequest_NonExistentFile_Returns404()
    {
        // Arrange
        using IHost host = CreateHost();
        HttpClient client = host.GetTestClient();
        Guid fileId = Guid.NewGuid();

        // Act
        HttpRequestMessage request = new(HttpMethod.Head, $"/files/{fileId}");
        HttpResponseMessage response = await client.SendAsync(request);

        // Assert
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task GetFileById_WithRangeHeader_ForwardsRangeRequest()
    {
        // Arrange
        Guid fileId = Guid.NewGuid();
        FileInformationReadDto fileInfo = new()
        {
            Id = fileId,
            ContentType = "video/mp4",
            Metadata = new FileMetadata { Extension = "mp4" }
        };

        long? capturedOffset = null;
        long? capturedLength = null;
        using IHost host = CreateHost(fileInfo, (offset, length) =>
        {
            capturedOffset = offset;
            capturedLength = length;
        });
        HttpClient client = host.GetTestClient();

        // Act
        HttpRequestMessage request = new(HttpMethod.Get, $"/files/{fileId}");
        request.Headers.Add("Range", "bytes=5-11");
        HttpResponseMessage response = await client.SendAsync(request);

        // Assert
        _ = capturedOffset.Should().Be(5);
        _ = capturedLength.Should().Be(7);
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.PartialContent);
    }

    private static IHost CreateHost(FileInformationReadDto? fileInfo = null, Action<long?, long?>? captureRangeParams = null)
    {
        Mock<IObjectStorageService> storageServiceMock = new();

        _ = storageServiceMock
            .Setup(s => s.GetMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ObjectMetadata(1000, "application/octet-stream"));

        _ = storageServiceMock
            .Setup(s => s.StreamToAsync(
                It.IsAny<string>(),
                It.IsAny<Stream>(),
                It.IsAny<long?>(),
                It.IsAny<long?>(),
                It.IsAny<CancellationToken>()))
            .Callback<string, Stream, long?, long?, CancellationToken>((key, stream, offset, length, ct) =>
            {
                captureRangeParams?.Invoke(offset, length);
            })
            .Returns(Task.CompletedTask);

        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                _ = webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        _ = services.AddSingleton(storageServiceMock.Object);

                        TestTenantAccessor tenantAccessor = new("root");
                        _ = services.AddSingleton<IMultiTenantContextAccessor<TenantInfo>>(tenantAccessor);
                        _ = services.AddSingleton<IMultiTenantContextSetter>(tenantAccessor);

                        InMemoryFileInformationService fileInfoService = new();
                        if (fileInfo is not null)
                        {
                            fileInfoService.Seed("test-file", fileInfo);
                        }
                        _ = services.AddScoped<IFileInformationService>(_ => fileInfoService);

                        _ = services.AddSingleton<IServiceScopeFactory>(sp => sp.GetRequiredService<IServiceScopeFactory>());

                        _ = services.AddSingleton<IOptionsSnapshot<FilesOptions>>(new OptionsWrapper<FilesOptions>(
                            new FilesOptions
                            {
                                Prefix = "files",
                                BaseUrl = "https://files.example",
                                UploadPath = "uploads"
                            }));

                        _ = services.AddRouting();
                    })
                    .Configure(app =>
                    {
                        _ = app.UseRouting();
                        _ = app.UseEndpoints(endpoints =>
                        {
                            _ = endpoints.MapFilesApi();
                        });
                    });
            })
            .Start();
    }

    private sealed class OptionsWrapper<T>(T value) : IOptionsSnapshot<T> where T : class
    {
        public T Value { get; } = value;
        public T Get(string? name) => Value;
    }
}
