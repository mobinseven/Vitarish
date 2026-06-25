using Microsoft.JSInterop;
using Vitarish.Files.Client;
using Vitarish.Files.Contracts.Errors;
using Vitarish.Files.Contracts.Metadata;
using Vitarish.Files.Contracts.Requests;
using Vitarish.Files.Contracts.Responses;
using Vitarish.Files.Contracts.Results;

namespace Vitarish.Files.Interface.Services;

public sealed class FileTransferCoordinator(
    IFilesClient filesClient,
    IFileInputInterop fileInputInterop) : IFileTransferCoordinator
{
    public async Task StartTransferAsync(
        IReadOnlyList<PendingFileDto> pendingFiles,
        string inputId,
        FileSelectionMode mode,
        Func<Task> onProgressAsync,
        Func<FileInformationDto, Task> onUploadedAsync,
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

            FileInformationDto? existingFile = await FindExistingChecksumAsync(file.Checksum, cancellationToken).ConfigureAwait(false);
            if (existingFile is not null)
            {
                FileInformationDto savedFile = await CreateFileRecordAsync(file, existingFile, cancellationToken).ConfigureAwait(false);
                _ = onPendingRemoved(file, mode);
                await onUploadedAsync(savedFile).ConfigureAwait(false);
                continue;
            }

            await using IJSStreamReference? streamRef = await fileInputInterop.GetFileStreamAsync(inputId, file.Name, cancellationToken).ConfigureAwait(false)
                ?? throw new InvalidOperationException("Unable to read selected file stream.");
            await using Stream stream = await streamRef.OpenReadStreamAsync(file.Size, cancellationToken).ConfigureAwait(false);
            using ProgressStream progressStream = new(stream, file.Size, progress => file.Progress = progress, onProgressAsync);

            FileUploadRequest request = new()
            {
                FileName = file.Name,
                ContentType = file.Type,
                Size = file.Size,
                Checksum = file.Checksum,
                Thumbnail = file.Thumbnail
            };

            FilesResult<FileUploadResponse> uploadResult = await filesClient.UploadFileAsync(request, progressStream, cancellationToken).ConfigureAwait(false);
            FileUploadResponse response = EnsureSuccess(uploadResult, "upload file");
            _ = onPendingRemoved(file, mode);
            await onUploadedAsync(response.File).ConfigureAwait(false);
        }
    }

    private async Task<FileInformationDto?> FindExistingChecksumAsync(string checksum, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(checksum))
        {
            return null;
        }

        FilesResult<IReadOnlyList<FileInformationDto>> result = await filesClient
            .FindFilesAsync(new FileInformationDescriptorDto { Checksum = checksum }, cancellationToken)
            .ConfigureAwait(false);

        return EnsureSuccess(result, "find files by checksum").FirstOrDefault();
    }

    private async Task<FileInformationDto> CreateFileRecordAsync(PendingFileDto file, FileInformationDto existingFile, CancellationToken cancellationToken)
    {
        FileInformationWriteRequest request = new()
        {
            AllowedOrigins = existingFile.AllowedOrigins,
            Checksum = existingFile.Checksum,
            ContentType = existingFile.ContentType,
            Owner = existingFile.Owner,
            Thumbnail = file.Thumbnail,
            Tag = existingFile.Tag,
            Url = existingFile.Url
        };

        FilesResult<FileInformationDto> result = await filesClient.CreateFileRecordAsync(request, cancellationToken).ConfigureAwait(false);
        return EnsureSuccess(result, "create duplicate file record");
    }

    private static T EnsureSuccess<T>(FilesResult<T> result, string operationName)
    {
        if (result.Succeeded && result.Value is not null)
        {
            return result.Value;
        }

        FilesError error = result.Error ?? new FilesError(FilesErrorCode.Unexpected, "Files operation failed without error details.");
        throw new InvalidOperationException($"Unable to {operationName}: {error.Message}");
    }

    private sealed class ProgressStream(Stream inner, long size, Action<double> setProgress, Func<Task> onProgressAsync) : Stream
    {
        private long _transferred;
        private double _lastReportedProgress;

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => inner.Length;
        public override long Position { get => inner.Position; set => throw new NotSupportedException(); }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();

        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int read = await inner.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);
            if (read > 0 && size > 0)
            {
                _transferred += read;
                double currentProgress = Math.Min(100d, 100d * _transferred / size);
                if (currentProgress - _lastReportedProgress >= 1.0 || currentProgress >= 100d)
                {
                    _lastReportedProgress = currentProgress;
                    setProgress(currentProgress);
                    await onProgressAsync().ConfigureAwait(false);
                }
            }

            return read;
        }
    }
}
