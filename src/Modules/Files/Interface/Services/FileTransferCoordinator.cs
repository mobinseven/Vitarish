using Microsoft.JSInterop;
using System.Threading.Channels;
using Vitarish.Files.Application.Abstractions;

namespace Vitarish.Files.Interface.Services;

public sealed class FileTransferCoordinator(
    IFilesService filesService,
    IFileInformationService fileInformationService,
    IFileInputInterop fileInputInterop) : IFileTransferCoordinator
{
    public async Task StartTransferAsync(
        IReadOnlyList<PendingFileDto> pendingFiles,
        string inputId,
        string? userId,
        FileSelectionMode mode,
        Func<Task> onProgressAsync,
        Func<FileInformationReadDto, Task> onUploadedAsync,
        Func<PendingFileDto, FileSelectionMode, bool> onPendingRemoved,
        CancellationToken cancellationToken = default)
    {
        List<PendingFileDto> filesToTransfer = [.. pendingFiles];
        foreach (PendingFileDto file in filesToTransfer)
        {
            if (file.Size <= 0 || string.IsNullOrWhiteSpace(file.Name))
            {
                continue;
            }

            file.UserId = userId;
            FileInformationReadDto info = CreateFileInformation(file);

            FileInformationReadDto? existingFile = await FindExistingChecksumAsync(file.Checksum, cancellationToken).ConfigureAwait(false);
            if (existingFile is not null)
            {
                FileInformationReadDto savedFile = await CreateFileRecordAsync(info, existingFile, cancellationToken).ConfigureAwait(false);
                _ = onPendingRemoved(file, mode);
                await onUploadedAsync(savedFile);
                continue;
            }

            await using IJSStreamReference? streamRef = await fileInputInterop.GetFileStreamAsync(inputId, file.Name, cancellationToken) ?? throw new InvalidOperationException("Unable to read selected file stream.");
            await using Stream stream = await streamRef.OpenReadStreamAsync(file.Size, cancellationToken);

            Channel<byte[]> channel = Channel.CreateBounded<byte[]>(new BoundedChannelOptions(1)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });
            using CancellationTokenSource pumpCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            CancellationToken pumpCancellationToken = pumpCancellationTokenSource.Token;

            Task pump = Task.Run(async () =>
            {
                try
                {
                    const int bufferLength = 20 * 1024;
                    byte[] buffer = new byte[bufferLength];
                    while (true)
                    {
                        int read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), pumpCancellationToken);
                        if (read == 0)
                        {
                            break;
                        }

                        byte[] chunk = new byte[read];
                        Array.Copy(buffer, 0, chunk, 0, read);
                        await channel.Writer.WriteAsync(chunk, pumpCancellationToken);
                        file.Progress += 100d * Convert.ToDouble(read) / file.Size;
                        await onProgressAsync();
                    }

                    _ = channel.Writer.TryComplete();
                }
                catch (Exception ex)
                {
                    _ = channel.Writer.TryComplete(ex);
                    throw;
                }
            }, cancellationToken);

            bool success = false;
            try
            {
                FileInformationReadDto fileInfo = await filesService.SaveFile(channel.Reader, info, cancellationToken).ConfigureAwait(false)
                    ?? throw new InvalidDataException("The returned result must not be null");

                if (!pumpCancellationTokenSource.IsCancellationRequested)
                {
                    await pumpCancellationTokenSource.CancelAsync().ConfigureAwait(false);
                }

                _ = channel.Writer.TryComplete();
                try
                {
                    await pump.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (pumpCancellationTokenSource.IsCancellationRequested)
                {
                    // SaveFile may return without consuming the stream when the file already exists.
                }
                _ = onPendingRemoved(file, mode);
                await onUploadedAsync(fileInfo);
                success = true;
            }
            finally
            {
                if (!pumpCancellationTokenSource.IsCancellationRequested)
                {
                    await pumpCancellationTokenSource.CancelAsync().ConfigureAwait(false);
                }

                _ = channel.Writer.TryComplete();
                try
                {
                    await pump.ConfigureAwait(false);
                }
                catch (Exception) when (!success)
                {
                    // Ignore pump exceptions if the main operation already failed to avoid masking the original exception.
                }
                catch (OperationCanceledException) when (pumpCancellationTokenSource.IsCancellationRequested)
                {
                    // Pump cancellation is expected when upload fails or finishes early.
                }
            }
        }
    }

    private static FileInformationReadDto CreateFileInformation(PendingFileDto file) => new()
    {
        Checksum = file.Checksum,
        Thumbnail = file.Thumbnail,
        AllowedOrigins = "*",
        ContentType = file.Type,
        Metadata = new FileMetadata { Size = file.Size }
    };

    private async Task<FileInformationReadDto?> FindExistingChecksumAsync(string checksum, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(checksum))
        {
            return null;
        }

        IReadOnlyList<FileInformationReadDto> existingFiles = await fileInformationService
            .GetListAsync(new FileInformationDescriptorDto { Checksum = checksum }, cancellationToken)
            .EnsureValueAsync()
            .ConfigureAwait(false);

        return existingFiles.FirstOrDefault();
    }

    private async Task<FileInformationReadDto> CreateFileRecordAsync(FileInformationReadDto info, FileInformationReadDto existingFile, CancellationToken cancellationToken)
    {
        FileInformationWriteDto writeDto = new()
        {
            AllowedOrigins = info.AllowedOrigins,
            Checksum = info.Checksum,
            ContentType = info.ContentType,
            Owner = existingFile.Owner,
            Thumbnail = info.Thumbnail,
            Tag = info.Tag,
            Url = existingFile.Url
        };

        return await fileInformationService.AddAsync(writeDto, cancellationToken).EnsureValueAsync().ConfigureAwait(false);
    }
}
