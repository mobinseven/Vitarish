using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Vitarish.Entities.Server.Models;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Domain.Models;
using Vitarish.Main.Infrastructure.BackgroundJobs.Base;
using Vitarish.Main.Infrastructure.BackgroundJobs.Sea;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Parties.Application.Dtos;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Infrastructure.Services;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Main.Tests.Infrastructure.BackgroundJobs;

public class AttachmentMigrationProcessorTests : IDisposable
{
    private readonly Mock<ILogger<AttachmentMigrationProcessor>> mockLogger;
    private readonly Mock<ILogger<TenantBackgroundExecutor>> mockExecutorLogger;
    private readonly Mock<IEventsService> mockEventsService;
    private readonly Mock<IAttachmentsService> mockAttachmentsService;
    private readonly Mock<IFileInformationService> mockFileInformationService;
    private readonly Mock<IPartiesService> mockPartiesService;
    private readonly ServiceProvider serviceProvider;
    private readonly AttachmentMigrationProcessor sut;

    public AttachmentMigrationProcessorTests()
    {
        mockLogger = new Mock<ILogger<AttachmentMigrationProcessor>>();
        mockExecutorLogger = new Mock<ILogger<TenantBackgroundExecutor>>();
        mockEventsService = new Mock<IEventsService>();
        mockAttachmentsService = new Mock<IAttachmentsService>();
        mockFileInformationService = new Mock<IFileInformationService>();
        mockPartiesService = new Mock<IPartiesService>();

        // Setup default tenant
        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new TenantReadDto
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                    ClientId = "client-1",
                    Name = "Tenant 1"
                }
            });

        ServiceCollection services = new();
        _ = services.AddHttpContextAccessor();
        services.AddScoped<RequestContext>();
        services.AddScoped<IRequestContext>(sp => sp.GetRequiredService<RequestContext>());
        services.AddScoped<IRequestContextWriter>(sp => sp.GetRequiredService<RequestContext>());
        services.AddScoped(_ => mockEventsService.Object);
        services.AddScoped(_ => mockAttachmentsService.Object);
        services.AddScoped(_ => mockFileInformationService.Object);
        services.AddScoped(_ => mockPartiesService.Object);
        services.AddSingleton(_ => mockExecutorLogger.Object);
        services.AddScoped<TenantBackgroundExecutor>();

        serviceProvider = services.BuildServiceProvider();

        TenantBackgroundExecutor executor = serviceProvider.GetRequiredService<TenantBackgroundExecutor>();
        sut = new AttachmentMigrationProcessor(mockLogger.Object, executor);
    }

    [Fact]
    public async Task ProcessMigration_ShouldSkipAlreadyMigratedAttachments()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        Guid attachmentId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();

        EventDto eventDto = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid())
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Event Description"
        };

        // Attachment already has a GUID as content (already migrated)
        AttachmentDto attachmentDto = new(
            fileId.ToString(),
            "image/png",
            null,
            1024)
        {
            Id = attachmentId
        };

        mockEventsService
            .Setup(x => x.GetListAsync(It.IsAny<EventDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<EventDto>>.Success(new List<EventDto> { eventDto }));

        mockAttachmentsService
            .Setup(x => x.GetByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AttachmentDto> { attachmentDto });

        // Act
        await sut.ProcessMigration(CancellationToken.None);

        // Assert
        mockAttachmentsService.Verify(
            x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not update attachments that are already migrated (content is already a GUID)");
    }

    [Fact]
    public async Task ProcessMigration_ShouldSkipTextAttachments()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        Guid attachmentId = Guid.NewGuid();

        EventDto eventDto = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid())
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Event Description"
        };

        // Text attachment with actual text content
        AttachmentDto attachmentDto = new(
            "This is some text content",
            "text/plain",
            null,
            25)
        {
            Id = attachmentId
        };

        mockEventsService
            .Setup(x => x.GetListAsync(It.IsAny<EventDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<EventDto>>.Success(new List<EventDto> { eventDto }));

        mockAttachmentsService
            .Setup(x => x.GetByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AttachmentDto> { attachmentDto });

        // Act
        await sut.ProcessMigration(CancellationToken.None);

        // Assert
        mockAttachmentsService.Verify(
            x => x.UpdateAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never,
            "Should not update text attachments as they contain actual text, not file references");
    }

    [Fact]
    public async Task ProcessMigration_ShouldMigrateUrlToFileId()
    {
        // Arrange
        Guid eventId = Guid.NewGuid();
        Guid attachmentId = Guid.NewGuid();
        Guid fileId = Guid.NewGuid();
        string fileUrl = "https://example.com/uploads/test-file.png";

        EventDto eventDto = new(
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null,
            Guid.NewGuid())
        {
            Id = eventId,
            Title = "Test Event",
            Description = "Event Description"
        };

        // Attachment with URL-based content (old format)
        AttachmentDto attachmentDto = new(
            fileUrl,
            "image/png",
            null,
            1024)
        {
            Id = attachmentId
        };

        FileInformationDto fileInfoDto = new()
        {
            Id = fileId,
            Url = fileUrl,
            ContentType = "image/png",
            Metadata = new FileMetadata { Size = 1024 }
        };

        mockEventsService
            .Setup(x => x.GetListAsync(It.IsAny<EventDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<EventDto>>.Success(new List<EventDto> { eventDto }));

        mockAttachmentsService
            .Setup(x => x.GetByEventAsync(eventId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AttachmentDto> { attachmentDto });

        mockFileInformationService
            .Setup(x => x.GetListAsync(It.Is<FileInformationDescriptorDto>(d => d.Url == fileUrl), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<FileInformationDto>>.Success(new List<FileInformationDto> { fileInfoDto }));

        AttachmentDto updatedAttachment = new(
            fileId.ToString(),
            "image/png",
            null,
            1024)
        {
            Id = attachmentId
        };

        mockAttachmentsService
            .Setup(x => x.UpdateAsync(eventId, attachmentId, fileId.ToString(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(updatedAttachment);

        // Act
        await sut.ProcessMigration(CancellationToken.None);

        // Assert
        mockAttachmentsService.Verify(
            x => x.UpdateAsync(eventId, attachmentId, fileId.ToString(), It.IsAny<CancellationToken>()),
            Times.Once,
            "Should update the attachment with the file ID from the matching FileInformation");
    }

    [Fact]
    public async Task ProcessMigration_ShouldHandleMultipleTenants()
    {
        // Arrange
        mockPartiesService
            .Setup(x => x.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[]
            {
                new TenantReadDto
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000001"),
                    OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000010"),
                    ClientId = "client-1",
                    Name = "Tenant 1"
                },
                new TenantReadDto
                {
                    Id = Guid.Parse("00000000-0000-0000-0000-000000000002"),
                    OwnerId = Guid.Parse("00000000-0000-0000-0000-000000000020"),
                    ClientId = "client-2",
                    Name = "Tenant 2"
                }
            });

        mockEventsService
            .Setup(x => x.GetListAsync(It.IsAny<EventDescriptorDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<EventDto>>.Success(new List<EventDto>()));

        // Act
        await sut.ProcessMigration(CancellationToken.None);

        // Assert
        mockEventsService.Verify(
            x => x.GetListAsync(It.IsAny<EventDescriptorDto>(), It.IsAny<CancellationToken>()),
            Times.Exactly(2),
            "Should process migration for all tenants");
    }

    public void Dispose()
    {
        serviceProvider?.Dispose();
        GC.SuppressFinalize(this);
    }
}
