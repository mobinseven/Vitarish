using FluentAssertions;
using Moq;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Contracts.Routes;
using Vitarish.Files.Domain.Models;
using Vitarish.Parties.Application.Profiles;
using Vitarish.Parties.Infrastructure.Services;

namespace Vitarish.Parties.Tests.Infrastructure;

public sealed class FilesTenantImageFilesTests
{
    private const string ObjectKey = "tenant/logo";
    private static readonly Guid FileId = Guid.NewGuid();

    [Fact]
    public async Task Opens_a_verified_image_at_the_address_it_made()
    {
        MemoryStream stored = new([1, 2, 3]);
        Mock<IObjectStorageService> storage = new();
        storage.Setup(item => item.DownloadAsync(ObjectKey, It.IsAny<CancellationToken>())).ReturnsAsync(stored);
        FilesTenantImageFiles images = Create(Image("image/png"), Verified(), storage.Object);

        TenantImageContent? logo = await images.OpenAsync(images.ContentUrl(FileId));

        logo.Should().BeEquivalentTo(new TenantImageContent(stored, "image/png", 3));
    }

    [Theory]
    [InlineData("/somewhere/else.png")]
    [InlineData("https://example.test/logo.png")]
    public async Task Opens_nothing_at_an_address_it_did_not_make(string contentUrl)
    {
        Mock<IFileInformationService> files = new(MockBehavior.Strict);
        FilesTenantImageFiles images = new(files.Object, Mock.Of<IFileIntegrityService>(MockBehavior.Strict), Mock.Of<IObjectStorageService>(MockBehavior.Strict));

        (await images.OpenAsync(contentUrl)).Should().BeNull();
        (await images.OpenAsync($"{FilesApiRoutes.ContentPath(FileId)}/../{Guid.NewGuid()}")).Should().BeNull();
        files.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Opens_nothing_for_a_file_outside_the_tenant_or_not_shown_as_an_image()
    {
        Mock<IObjectStorageService> storage = new(MockBehavior.Strict);
        FilesTenantImageFiles missing = Create(null, Verified(), storage.Object);
        FilesTenantImageFiles document = Create(Image("application/pdf"), Verified(), storage.Object);
        FilesTenantImageFiles heic = Create(Image("image/heic"), Verified(), storage.Object);

        (await missing.OpenAsync(FilesApiRoutes.ContentPath(FileId))).Should().BeNull();
        (await document.OpenAsync(FilesApiRoutes.ContentPath(FileId))).Should().BeNull();
        (await heic.OpenAsync(FilesApiRoutes.ContentPath(FileId))).Should().BeNull();
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Opens_nothing_whose_stored_content_fails_verification()
    {
        Mock<IObjectStorageService> storage = new(MockBehavior.Strict);
        FilesTenantImageFiles unavailable = Create(Image("image/png"), new FileIntegrityVerificationResult(FileIntegrityState.Unavailable, ObjectKey, 3, "image/png"), storage.Object);
        FilesTenantImageFiles unhashed = Create(Image("image/png") with { Checksum = null }, Verified(), storage.Object);

        (await unavailable.OpenAsync(FilesApiRoutes.ContentPath(FileId))).Should().BeNull();
        (await unhashed.OpenAsync(FilesApiRoutes.ContentPath(FileId))).Should().BeNull();
        storage.VerifyNoOtherCalls();
    }

    private static FilesTenantImageFiles Create(FileInformationReadDto? file, FileIntegrityVerificationResult verification, IObjectStorageService storage)
    {
        Mock<IFileInformationService> files = new();
        files.Setup(item => item.GetAsync(FileId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(file is null ? EntityResult<FileInformationReadDto>.Failure(EntityError.NotFound("missing")) : EntityResult<FileInformationReadDto>.Success(file));
        Mock<IFileIntegrityService> integrity = new();
        integrity.Setup(item => item.VerifyAsync(It.IsAny<FileInformationReadDto>(), It.IsAny<CancellationToken>())).ReturnsAsync(verification);
        return new FilesTenantImageFiles(files.Object, integrity.Object, storage);
    }

    private static FileInformationReadDto Image(string contentType) => new() { Id = FileId, ContentType = contentType, Checksum = "checksum" };

    private static FileIntegrityVerificationResult Verified() => new(FileIntegrityState.Verified, ObjectKey, 3, "image/png");
}
