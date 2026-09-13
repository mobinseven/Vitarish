using Vitarish.Files.Domain.Models;

namespace Vitarish.Files.Application.Abstractions;

public interface IFileIntegrityService
{
    Task<FileIntegrityVerificationResult> BeginEnrollmentAsync(
        FileInformationReadDto file,
        long contentLength,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<FileIntegrityVerificationResult> BeginLegacyEnrollmentAsync(
        FileInformationReadDto file,
        CancellationToken cancellationToken = default);

    Task<FileIntegrityVerificationResult> VerifyAsync(
        FileInformationReadDto file,
        CancellationToken cancellationToken = default);
}
