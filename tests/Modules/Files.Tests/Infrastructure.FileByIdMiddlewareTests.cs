using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Minio;
using Minio.DataModel.Args;
using Vitarish.Files.Infrastructure.Services;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Entities.Server.Models;
using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;

namespace Vitarish.Files.Tests;

public sealed class Infrastructure_FileByIdMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_NonFilesPath_CallsNext()
    {
        // Arrange
        bool nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = CreateMiddleware(next);

        DefaultHttpContext context = new();
        context.Request.Method = "GET";
        context.Request.Path = "/api/some-other-path";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_PostMethod_Returns405()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);

        DefaultHttpContext context = new();
        context.Request.Method = "POST";
        context.Request.Path = "/files/" + Guid.NewGuid();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status405MethodNotAllowed);
    }

    [Fact]
    public async Task InvokeAsync_InvalidGuid_Returns400()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);

        DefaultHttpContext context = new();
        context.Request.Method = "GET";
        context.Request.Path = "/files/not-a-guid";

        SetupServices(context, fileInfo: null);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
    }

    [Fact]
    public async Task InvokeAsync_FileNotFound_Returns404()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);

        Guid fileId = Guid.NewGuid();
        DefaultHttpContext context = new();
        context.Request.Method = "GET";
        context.Request.Path = $"/files/{fileId}";

        SetupServices(context, fileInfo: null);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task InvokeAsync_FilesPathWithNoId_CallsNext()
    {
        // Arrange
        bool nextCalled = false;
        RequestDelegate next = _ => { nextCalled = true; return Task.CompletedTask; };
        var middleware = CreateMiddleware(next);

        DefaultHttpContext context = new();
        context.Request.Method = "GET";
        context.Request.Path = "/files";

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = nextCalled.Should().BeTrue();
    }

    [Fact]
    public async Task InvokeAsync_HeadMethod_IsAllowed()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);

        Guid fileId = Guid.NewGuid();
        DefaultHttpContext context = new();
        context.Request.Method = "HEAD";
        context.Request.Path = $"/files/{fileId}";

        SetupServices(context, fileInfo: null);

        // Act
        await middleware.InvokeAsync(context);

        // Assert - should reach 404 (file not found) not 405 (method not allowed)
        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status404NotFound);
    }

    [Fact]
    public async Task InvokeAsync_PutMethod_Returns405()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);

        DefaultHttpContext context = new();
        context.Request.Method = "PUT";
        context.Request.Path = "/files/" + Guid.NewGuid();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status405MethodNotAllowed);
    }

    [Fact]
    public async Task InvokeAsync_DeleteMethod_Returns405()
    {
        // Arrange
        RequestDelegate next = _ => Task.CompletedTask;
        var middleware = CreateMiddleware(next);

        DefaultHttpContext context = new();
        context.Request.Method = "DELETE";
        context.Request.Path = "/files/" + Guid.NewGuid();

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        _ = context.Response.StatusCode.Should().Be(StatusCodes.Status405MethodNotAllowed);
    }

    private static FileByIdMiddleware CreateMiddleware(RequestDelegate next)
    {
        var minioClient = new Mock<IMinioClient>();
        var httpClientFactory = new Mock<IHttpClientFactory>();
        var logger = NullLogger<FileByIdMiddleware>.Instance;
        return new FileByIdMiddleware(next, minioClient.Object, httpClientFactory.Object);
    }

    private static void SetupServices(DefaultHttpContext context, FileInformationDto? fileInfo)
    {
        var tenantAccessor = new TestTenantAccessor("root");
        var fileInfoService = new InMemoryFileInformationService();
        if (fileInfo is not null)
        {
            fileInfoService.Seed("test-file", fileInfo);
        }

        var filesOptions = Options.Create(new FilesOptions
        {
            Prefix = "files",
            BaseUrl = "https://files.example",
            UploadPath = "uploads"
        });

        var minioOptions = Options.Create(new MinioOptions
        {
            Endpoint = "minio.example",
            AccessKey = "key",
            SecretKey = "secret",
            BucketName = "bucket"
        });

        var serviceScopeFactory = new Mock<IServiceScopeFactory>();
        var serviceScope = new Mock<IServiceScope>();
        var scopedServiceProvider = new Mock<IServiceProvider>();

        scopedServiceProvider.Setup(sp => sp.GetService(typeof(IFileInformationService)))
            .Returns(fileInfoService);
        serviceScope.Setup(s => s.ServiceProvider).Returns(scopedServiceProvider.Object);
        serviceScopeFactory.Setup(f => f.CreateScope()).Returns(serviceScope.Object);

        var serviceProvider = new Mock<IServiceProvider>();
        serviceProvider.Setup(sp => sp.GetService(typeof(IMultiTenantContextAccessor<TenantInfo>)))
            .Returns(tenantAccessor);
        serviceProvider.Setup(sp => sp.GetService(typeof(IServiceScopeFactory)))
            .Returns(serviceScopeFactory.Object);
        serviceProvider.Setup(sp => sp.GetService(typeof(IOptionsSnapshot<FilesOptions>)))
            .Returns(new OptionsWrapper<FilesOptions>(filesOptions.Value));
        serviceProvider.Setup(sp => sp.GetService(typeof(IOptionsSnapshot<MinioOptions>)))
            .Returns(new OptionsWrapper<MinioOptions>(minioOptions.Value));

        context.RequestServices = serviceProvider.Object;
    }

    private sealed class OptionsWrapper<T> : IOptionsSnapshot<T> where T : class
    {
        public OptionsWrapper(T value) => Value = value;
        public T Value { get; }
        public T Get(string? name) => Value;
    }
}
