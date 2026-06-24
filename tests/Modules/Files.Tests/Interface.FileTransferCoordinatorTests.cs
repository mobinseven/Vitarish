using System.Threading.Channels;
using Microsoft.JSInterop;
using Vitarish.Entities.Contracts;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Interface.Abstractions;
using Vitarish.Files.Interface.Models;
using Vitarish.Files.Interface.Services;

namespace Files.Tests;

public sealed class FileTransferCoordinatorTests
{
    [Fact]
    public async Task StartTransferAsync_WhenChecksumExists_CreatesFileRecordWithoutOpeningStream()
    {
        // Arrange
        PendingFileDto pendingFile = new("existing.bin", 1024, "application/octet-stream", "checksum", "thumbnail");
        FileInformationReadDto existingFile = new()
        {
            Checksum = pendingFile.Checksum,
            ContentType = pendingFile.Type,
            Owner = "tenant",
            Thumbnail = pendingFile.Thumbnail,
            Url = "https://example.com/existing.bin"
        };
        FileInformationReadDto savedFile = existingFile with { Id = Guid.NewGuid() };

        Mock<IFileInformationService> fileInformationService = new(MockBehavior.Strict);
        _ = fileInformationService
            .Setup(service => service.GetListAsync(
                It.Is<FileInformationDescriptorDto>(descriptor => descriptor.Checksum == pendingFile.Checksum),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<FileInformationReadDto>>.Success([existingFile]));
        _ = fileInformationService
            .Setup(service => service.AddAsync(
                It.Is<FileInformationWriteDto>(dto =>
                    dto.Checksum == pendingFile.Checksum &&
                    dto.ContentType == pendingFile.Type &&
                    dto.Thumbnail == pendingFile.Thumbnail &&
                    dto.Owner == "tenant" &&
                    dto.Url == "https://example.com/existing.bin"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<FileInformationReadDto>.Success(savedFile));

        Mock<IFileInputInterop> fileInputInterop = new(MockBehavior.Strict);
        Mock<IFilesService> filesService = new(MockBehavior.Strict);

        FileTransferCoordinator coordinator = new(filesService.Object, fileInformationService.Object, fileInputInterop.Object);
        bool removed = false;
        FileInformationReadDto? uploaded = null;

        // Act
        await coordinator.StartTransferAsync(
            [pendingFile],
            "input",
            "user",
            FileSelectionMode.SingleCapture,
            () => Task.CompletedTask,
            fileInfo =>
            {
                uploaded = fileInfo;
                return Task.CompletedTask;
            },
            (_, _) => removed = true,
            CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(3));

        // Assert
        _ = removed.Should().BeTrue();
        _ = uploaded.Should().Be(savedFile);
        _ = pendingFile.UserId.Should().Be("user");
        filesService.VerifyNoOtherCalls();
        fileInputInterop.VerifyNoOtherCalls();
        fileInformationService.VerifyAll();
    }

    [Fact]
    public async Task StartTransferAsync_WhenChecksumIsNew_StreamsFileToFilesService()
    {
        // Arrange
        const int fileSize = (20 * 1024 * 2) + 1;
        byte[] fileBytes = new byte[fileSize];
        Array.Fill<byte>(fileBytes, 1);

        PendingFileDto pendingFile = new("new.bin", fileSize, "application/octet-stream", "new-checksum", "thumbnail");
        FileInformationReadDto savedFile = new()
        {
            Checksum = pendingFile.Checksum,
            ContentType = pendingFile.Type,
            Thumbnail = pendingFile.Thumbnail
        };

        Mock<IFileInformationService> fileInformationService = new(MockBehavior.Strict);
        _ = fileInformationService
            .Setup(service => service.GetListAsync(
                It.Is<FileInformationDescriptorDto>(descriptor => descriptor.Checksum == pendingFile.Checksum),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(EntityResult<IReadOnlyList<FileInformationReadDto>>.Success([]));

        Mock<IJSStreamReference> streamReference = new(MockBehavior.Strict);
        _ = streamReference
            .Setup(reference => reference.OpenReadStreamAsync(fileSize, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new MemoryStream(fileBytes));
        _ = streamReference
            .Setup(reference => reference.DisposeAsync())
            .Returns(ValueTask.CompletedTask);

        Mock<IFileInputInterop> fileInputInterop = new(MockBehavior.Strict);
        _ = fileInputInterop
            .Setup(interop => interop.GetFileStreamAsync("input", pendingFile.Name, It.IsAny<CancellationToken>()))
            .ReturnsAsync(streamReference.Object);

        Mock<IFilesService> filesService = new(MockBehavior.Strict);
        _ = filesService
            .Setup(service => service.SaveFile(It.IsAny<ChannelReader<byte[]>>(), It.IsAny<FileInformationReadDto>(), It.IsAny<CancellationToken>()))
            .Returns<ChannelReader<byte[]>, FileInformationReadDto, CancellationToken>(async (reader, _, cancellationToken) =>
            {
                await foreach (byte[] chunk in reader.ReadAllAsync(cancellationToken))
                {
                }

                return savedFile;
            });

        FileTransferCoordinator coordinator = new(filesService.Object, fileInformationService.Object, fileInputInterop.Object);
        bool removed = false;
        FileInformationReadDto? uploaded = null;

        // Act
        await coordinator.StartTransferAsync(
            [pendingFile],
            "input",
            "user",
            FileSelectionMode.SingleCapture,
            () => Task.CompletedTask,
            fileInfo =>
            {
                uploaded = fileInfo;
                return Task.CompletedTask;
            },
            (_, _) => removed = true,
            CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(3));

        // Assert
        _ = removed.Should().BeTrue();
        _ = uploaded.Should().Be(savedFile);
        _ = pendingFile.UserId.Should().Be("user");
        filesService.VerifyAll();
        fileInputInterop.VerifyAll();
        streamReference.VerifyAll();
        fileInformationService.VerifyAll();
    }
}
