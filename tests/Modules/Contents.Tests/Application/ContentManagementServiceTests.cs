using Moq;
using Vitarish.Contents.Application;
using Vitarish.Contents.Application.Abstractions;
using Vitarish.Contents.Domain.Dto;
using Vitarish.Contents.Domain.Enums;
using Vitarish.Entities.Contracts.Models;

namespace Vitarish.Contents.Tests.Application;

public sealed class ContentManagementServiceTests
{
    [Fact]
    public async Task Update_preserves_server_owned_fields_and_original_publication_time()
    {
        Guid contentId = Guid.NewGuid();
        Guid authorId = Guid.NewGuid();
        DateTime publishedAt = new(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc);
        Dictionary<string, object?> metadata = new() { ["source"] = "import" };
        ContentItemReadDto current = new()
        {
            Id = contentId,
            AuthorId = authorId,
            Status = ContentStatus.Published,
            PublishedAt = publishedAt,
            Metadata = metadata
        };
        ContentItemWriteDto submitted = new()
        {
            Status = ContentStatus.Published,
            Title = "Updated",
            Slug = "updated",
            CanonicalPath = "/updated",
            Locale = "en"
        };
        Mock<IContentItemsService> items = new();
        items.Setup(service => service.GetAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ContentItemReadDto>.Success(current));
        items.Setup(service => service.UpdateAsync(submitted, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ContentItemReadDto>.Success(current));
        ContentManagementService service = new(items.Object, Mock.Of<IContentRevisionsService>());

        await service.UpdateAsync(contentId, submitted, CancellationToken.None);

        submitted.Id.Should().Be(contentId);
        submitted.AuthorId.Should().Be(authorId);
        submitted.PublishedAt.Should().Be(publishedAt);
        submitted.Metadata.Should().BeEquivalentTo(metadata);
        items.Verify(value => value.UpdateAsync(submitted, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Archive_preserves_content_and_changes_only_the_status()
    {
        Guid contentId = Guid.NewGuid();
        ContentItemReadDto current = new()
        {
            Id = contentId,
            Status = ContentStatus.Published,
            Title = "Original",
            Slug = "original",
            CanonicalPath = "/original",
            Locale = "en"
        };
        ContentItemWriteDto? archived = null;
        Mock<IContentItemsService> items = new();
        items.Setup(service => service.GetAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ContentItemReadDto>.Success(current));
        items.Setup(service => service.UpdateAsync(It.IsAny<ContentItemWriteDto>(), It.IsAny<CancellationToken>()))
            .Callback<ContentItemWriteDto, CancellationToken>((item, _) => archived = item)
            .ReturnsAsync(EntityResult<ContentItemReadDto>.Success(current));
        ContentManagementService service = new(items.Object, Mock.Of<IContentRevisionsService>());

        await service.ArchiveAsync(contentId, CancellationToken.None);

        archived.Should().NotBeNull();
        archived!.Status.Should().Be(ContentStatus.Archived);
        archived.Title.Should().Be(current.Title);
        archived.CanonicalPath.Should().Be(current.CanonicalPath);
    }

    [Fact]
    public async Task Update_clears_publication_time_when_content_returns_to_draft()
    {
        Guid contentId = Guid.NewGuid();
        ContentItemReadDto current = new()
        {
            Id = contentId,
            Status = ContentStatus.Published,
            PublishedAt = new DateTime(2026, 7, 1, 12, 0, 0, DateTimeKind.Utc)
        };
        ContentItemWriteDto submitted = new() { Status = ContentStatus.Draft };
        Mock<IContentItemsService> items = new();
        items.Setup(service => service.GetAsync(contentId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ContentItemReadDto>.Success(current));
        items.Setup(service => service.UpdateAsync(submitted, It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<ContentItemReadDto>.Success(current));
        ContentManagementService service = new(items.Object, Mock.Of<IContentRevisionsService>());

        await service.UpdateAsync(contentId, submitted, CancellationToken.None);

        submitted.PublishedAt.Should().BeNull();
    }
}
