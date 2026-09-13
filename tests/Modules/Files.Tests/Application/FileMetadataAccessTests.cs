using Moq;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Files.Application.Access;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Files.Tests.Application;

public sealed class FileMetadataAccessTests
{
    private const string TenantId = "tenant-a";

    [Fact]
    public async Task GetAsync_rejects_a_request_without_a_trusted_subject_before_loading_metadata()
    {
        Mock<IFileInformationService> information = new();
        FileMetadataAccess access = new(information.Object, new TestRequestContext(TenantId, null));

        FileInformationReadDto? result = await access.GetAsync(Guid.NewGuid());

        result.Should().BeNull();
        information.Verify(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task ListAsync_replaces_a_client_owner_with_the_trusted_tenant()
    {
        Mock<IFileInformationService> information = new();
        information
            .Setup(value => value.GetListAsync(It.IsAny<FileInformationDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<FileInformationReadDto>>.Success([]));
        FileMetadataAccess access = new(information.Object, new TestRequestContext(TenantId, "user-a"));

        _ = await access.ListAsync(new FileInformationDescriptorDto { Owner = "tenant-other" });

        information.Verify(value => value.GetListAsync(
            It.Is<FileInformationDescriptorDto>(descriptor => descriptor.Owner == TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_replaces_a_client_owner_with_the_trusted_tenant()
    {
        Mock<IFileInformationService> information = new();
        information
            .Setup(value => value.AddAsync(It.IsAny<FileInformationWriteDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<FileInformationReadDto>.Success(new FileInformationReadDto { Owner = TenantId }));
        FileMetadataAccess access = new(information.Object, new TestRequestContext(TenantId, "user-a"));

        _ = await access.CreateAsync(new FileInformationWriteDto { Owner = "tenant-other", ContentType = "application/pdf" });

        information.Verify(value => value.AddAsync(
            It.Is<FileInformationWriteDto>(file => file.Owner == TenantId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task AuthorizeDownloadAsync_rejects_a_cross_tenant_record()
    {
        Guid fileId = Guid.NewGuid();
        Mock<IFileInformationService> information = new();
        information
            .Setup(value => value.GetAsync(fileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<FileInformationReadDto>.Success(new FileInformationReadDto { Id = fileId, Owner = "tenant-other" }));
        FileMetadataAccess access = new(information.Object, new TestRequestContext(TenantId, "user-a"));

        FileInformationReadDto? result = await access.AuthorizeDownloadAsync(fileId);

        result.Should().BeNull();
    }

    private sealed class TestRequestContext(string? tenantId, string? userId) : IRequestContext
    {
        public string Authority => "test";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => userId;
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => null;
        public string? AdditionalInfo => null;
        public System.Security.Claims.ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { TenantId = TenantId, UserId = UserId };
    }
}
