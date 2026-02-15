using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Files.Domain.Models;
using Vitarish.Files.Domain.Options;
using Vitarish.Files.Infrastructure.Api;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Vitarish.Entities.Server.Models;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Infrastructure.Interfaces;

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
        _ = response.Content.Headers.ContentLength.Should().Be(1000);
        var content = await response.Content.ReadAsStringAsync();
        _ = content.Should().BeEmpty();
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

        long? capturedOffset = null;
        long? capturedLength = null;
        using var host = CreateHost(fileInfo, (offset, length) =>
        {
            capturedOffset = offset;
            capturedLength = length;
        });
        var client = host.GetTestClient();

        // Act
        var request = new HttpRequestMessage(HttpMethod.Get, $"/files/{fileId}");
        request.Headers.Add("Range", "bytes=5-11");
        var response = await client.SendAsync(request);

        // Assert
        _ = capturedOffset.Should().Be(5);
        _ = capturedLength.Should().Be(7);
        _ = response.StatusCode.Should().Be(System.Net.HttpStatusCode.PartialContent);
    }

    private static IHost CreateHost(FileInformationDto? fileInfo = null, Action<long?, long?>? captureRangeParams = null)
    {
        var storageServiceMock = new Mock<IObjectStorageService>();
        
        storageServiceMock
            .Setup(s => s.GetMetadataAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ObjectMetadata(1000, "application/octet-stream"));

        storageServiceMock
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
                webBuilder
                    .UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddSingleton(storageServiceMock.Object);

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
