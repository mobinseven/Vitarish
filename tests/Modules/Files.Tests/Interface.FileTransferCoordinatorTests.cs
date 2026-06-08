using System.Threading.Channels;
using Microsoft.JSInterop;
using Vitarish.Files.Application.Abstractions;
using Vitarish.Files.Interface.Abstractions;
using Vitarish.Files.Interface.Models;
using Vitarish.Files.Interface.Services;

namespace Files.Tests;

public sealed class FileTransferCoordinatorTests
{
    [Fact]
    public async Task StartTransferAsync_WhenSaveFileReturnsWithoutReadingStream_CompletesTransfer()
    {
        // Arrange
        const int fileSize = (20 * 1024 * 2) + 1;
        byte[] fileBytes = new byte[fileSize];
        Array.Fill<byte>(fileBytes, 1);

        PendingFileDto pendingFile = new("existing.bin", fileSize, "application/octet-stream", "checksum", "thumbnail");
        FileInformationReadDto savedFile = new()
        {
            Checksum = pendingFile.Checksum,
            ContentType = pendingFile.Type,
            Thumbnail = pendingFile.Thumbnail
        };

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
            .ReturnsAsync(savedFile);

        FileTransferCoordinator coordinator = new(filesService.Object, fileInputInterop.Object);
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
    }
}
