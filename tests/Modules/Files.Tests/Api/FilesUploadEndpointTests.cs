using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Threading.Channels;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Metadata;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Files.Api;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Contracts.Errors;
using Vitarish.Files.Contracts.Responses;
using Vitarish.Files.Domain.Models;
using Vitarish.Requests.Application.Abstractions;

namespace Files.Tests.Api;

public sealed class FilesUploadEndpointTests : IAsyncDisposable
{
    private static readonly byte[] PngHeader = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00, 0x00, 0x0D];

    private readonly Mock<IFilesService> _files = new();
    private readonly Mock<IFileTransferAudit> _audit = new();
    private readonly WebApplication _app;

    public FilesUploadEndpointTests()
    {
        WebApplicationBuilder builder = WebApplication.CreateBuilder();
        _ = builder.Services.AddRouting();
        _ = builder.Services.AddSingleton(_files.Object);
        _ = builder.Services.AddSingleton(_audit.Object);
        _ = builder.Services.AddSingleton(Mock.Of<IFileTransferAuthorization>(value => value.CanUploadAsync(It.IsAny<CancellationToken>()) == Task.FromResult(true)));
        _ = builder.Services.AddSingleton(Mock.Of<IFileMetadataAccess>());
        _ = builder.Services.AddSingleton(Mock.Of<IFileIntegrityService>());
        _ = builder.Services.AddSingleton(Mock.Of<ISecurityAuditWriter>());
        _ = builder.Services.AddSingleton(Mock.Of<IRequestContext>());
        _app = builder.Build();
        _ = _app.MapFilesApi();
    }

    [Fact]
    public void Upload_endpoint_raises_the_request_and_multipart_limits_to_the_policy_ceiling()
    {
        Endpoint upload = EndpointNamed("FilesUploadFile");

        upload.Metadata.GetMetadata<IRequestSizeLimitMetadata>()!.MaxRequestBodySize
            .Should().Be(FileTransferPolicy.MaximumMultipartRequestBytes);
        upload.Metadata.GetMetadata<IFormOptionsMetadata>()!.MultipartBodyLengthLimit
            .Should().Be(FileTransferPolicy.MaximumMultipartRequestBytes);
    }

    [Fact]
    public void Published_upload_form_fields_match_the_contract_the_handler_and_module_client_use()
    {
        // Multipart field names are matched case-insensitively.
        IEnumerable<string> formFields = typeof(FileUploadForm).GetProperties().Select(property => property.Name.ToLowerInvariant());
        IEnumerable<string> contractFields = typeof(Vitarish.Files.Contracts.Requests.FileUploadRequest).GetProperties()
            .Select(property => property.Name)
            .Append(Vitarish.Files.Contracts.Requests.FileUploadRequest.ContentFieldName)
            .Select(field => field.ToLowerInvariant());

        formFields.Should().BeEquivalentTo(contractFields);
    }

    [Fact]
    public async Task Upload_policy_publishes_the_ceiling_and_every_accepted_format()
    {
        (int status, JsonElement body) = await InvokeAsync(EndpointNamed("FilesGetUploadPolicy"), new DefaultHttpContext());

        status.Should().Be(StatusCodes.Status200OK);
        FileUploadPolicyResponse policy = body.Deserialize<FileUploadPolicyResponse>(JsonSerializerOptions.Web)!;
        policy.MaximumContentBytes.Should().Be(FileTransferPolicy.MaximumContentBytes);
        policy.Formats.Select(format => format.MediaType)
            .Should().Equal(FileTransferPolicy.UploadFormats.Select(format => format.MediaType));
    }

    [Fact]
    public async Task Upload_accepts_an_allowed_format_and_streams_it_to_the_files_service()
    {
        _ = _files.Setup(value => value.SaveFile(It.IsAny<ChannelReader<byte[]>>(), It.IsAny<FileInformationReadDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FileInformationReadDto { Id = Guid.NewGuid(), ContentType = "image/png" });

        (int status, _) = await InvokeAsync(EndpointNamed("FilesUploadFile"), await MultipartContextAsync(PngHeader, "image/png", "scan.png"));

        status.Should().Be(StatusCodes.Status200OK);
        _files.Verify(value => value.SaveFile(
            It.IsAny<ChannelReader<byte[]>>(),
            It.Is<FileInformationReadDto>(file => file.ContentType == "image/png"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("text/plain", "notes.txt")]
    [InlineData("image/png", "scan.jpg")]
    public async Task Upload_rejects_a_type_outside_the_allowlist_with_415_before_storage(string contentType, string fileName)
    {
        (int status, JsonElement body) = await InvokeAsync(EndpointNamed("FilesUploadFile"), await MultipartContextAsync(PngHeader, contentType, fileName));

        AssertRejected(status, body, StatusCodes.Status415UnsupportedMediaType, FilesErrorCode.UnsupportedContentType, SecurityAuditReasonCatalog.ContentTypeNotAllowed, fileName);
    }

    [Fact]
    public async Task Upload_rejects_a_renamed_executable_whose_content_does_not_match_its_claimed_format()
    {
        byte[] executable = [.. "MZ"u8, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00];

        (int status, JsonElement body) = await InvokeAsync(EndpointNamed("FilesUploadFile"), await MultipartContextAsync(executable, "image/png", "scan.png"));

        AssertRejected(status, body, StatusCodes.Status415UnsupportedMediaType, FilesErrorCode.UnsupportedContentType, SecurityAuditReasonCatalog.ContentTypeNotAllowed, "scan.png");
    }

    [Fact]
    public async Task Upload_rejects_a_declared_body_above_the_ceiling_with_413_before_reading_it()
    {
        HttpContext context = await MultipartContextAsync(PngHeader, "image/png", "scan.png");
        context.Request.ContentLength = FileTransferPolicy.MaximumMultipartRequestBytes + 1;

        (int status, JsonElement body) = await InvokeAsync(EndpointNamed("FilesUploadFile"), context);

        AssertRejected(status, body, StatusCodes.Status413PayloadTooLarge, FilesErrorCode.ContentTooLarge, SecurityAuditReasonCatalog.ContentTooLarge, null);
    }

    [Fact]
    public async Task Upload_rejects_a_streamed_body_that_exceeds_the_multipart_limit_with_413()
    {
        HttpContext context = await MultipartContextAsync(new byte[2048], "image/png", "scan.png");
        context.Request.ContentLength = null;
        context.Features.Set<Microsoft.AspNetCore.Http.Features.IFormFeature>(new Microsoft.AspNetCore.Http.Features.FormFeature(
            context.Request,
            new Microsoft.AspNetCore.Http.Features.FormOptions { MultipartBodyLengthLimit = 1024 }));

        (int status, JsonElement body) = await InvokeAsync(EndpointNamed("FilesUploadFile"), context);

        AssertRejected(status, body, StatusCodes.Status413PayloadTooLarge, FilesErrorCode.ContentTooLarge, SecurityAuditReasonCatalog.ContentTooLarge, null);
    }

    public async ValueTask DisposeAsync() => await _app.DisposeAsync();

    private void AssertRejected(int status, JsonElement body, int expectedStatus, FilesErrorCode expectedCode, string expectedReason, string? expectedFileName)
    {
        status.Should().Be(expectedStatus);
        body.Deserialize<FilesError>(JsonSerializerOptions.Web)!.Code.Should().Be(expectedCode);
        _audit.Verify(value => value.RecordImportAsync(null, expectedFileName, SecurityAuditOutcome.Denied, expectedReason, It.IsAny<CancellationToken>()), Times.Once);
        _files.Verify(value => value.SaveFile(It.IsAny<ChannelReader<byte[]>>(), It.IsAny<FileInformationReadDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private Endpoint EndpointNamed(string operationId) =>
        ((IEndpointRouteBuilder)_app).DataSources
            .SelectMany(source => source.Endpoints)
            .Single(endpoint => endpoint.Metadata.GetMetadata<IEndpointNameMetadata>()?.EndpointName == operationId);

    private async Task<(int Status, JsonElement Body)> InvokeAsync(Endpoint endpoint, HttpContext context)
    {
        await using AsyncServiceScope scope = _app.Services.CreateAsyncScope();
        context.RequestServices = scope.ServiceProvider;
        using MemoryStream response = new();
        context.Response.Body = response;

        await endpoint.RequestDelegate!(context);

        response.Position = 0;
        JsonElement body = response.Length == 0 ? default : await JsonSerializer.DeserializeAsync<JsonElement>(response);
        return (context.Response.StatusCode, body);
    }

    private static async Task<HttpContext> MultipartContextAsync(byte[] content, string contentType, string fileName)
    {
        using MultipartFormDataContent form = new();
        ByteArrayContent file = new(content);
        file.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        form.Add(file, nameof(FileUploadForm.Content), fileName);
        form.Add(new StringContent(fileName), nameof(FileUploadForm.FileName));
        form.Add(new StringContent(contentType), nameof(FileUploadForm.ContentType));
        form.Add(new StringContent(content.Length.ToString(System.Globalization.CultureInfo.InvariantCulture)), nameof(FileUploadForm.Size));
        form.Add(new StringContent(Convert.ToHexStringLower(SHA256.HashData(content))), nameof(FileUploadForm.Checksum));

        MemoryStream body = new();
        await form.CopyToAsync(body);
        body.Position = 0;
        DefaultHttpContext context = new();
        context.Request.Method = HttpMethods.Post;
        context.Request.ContentType = form.Headers.ContentType!.ToString();
        context.Request.ContentLength = body.Length;
        context.Request.Body = body;
        return context;
    }
}
