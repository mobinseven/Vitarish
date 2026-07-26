using MediatR;
using Vitarish.Medical.Domain.Enums.Prescriptions;

namespace Vitarish.Medical.Application.Commands.Prescriptions;

/// <summary>Creates or updates a doctor provider credential mapping.</summary>
public sealed record SaveCredentialCommand : IRequest<SaveCredentialResult>
{
    public Guid? CredentialId { get; init; }
    public required Guid DoctorId { get; init; }
    public required string ProviderKey { get; init; }
    public required Guid ClinicId { get; init; }
    public string? ProviderDoctorIdentifier { get; init; }
    public string? ProviderUsername { get; init; }
    public string? ProviderClinicIdentifier { get; init; }
    public string? CredentialSecretRef { get; init; }
}

public sealed record SaveCredentialResult
{
    public bool IsSuccess { get; init; }
    public Guid? CredentialId { get; init; }
    public string? ErrorMessage { get; init; }

    public static SaveCredentialResult Success(Guid id) => new() { IsSuccess = true, CredentialId = id };
    public static SaveCredentialResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>Tests a credential by authenticating with the provider.</summary>
public sealed record TestCredentialCommand : IRequest<TestCredentialResult>
{
    public required Guid CredentialId { get; init; }
}

public sealed record TestCredentialResult
{
    public bool IsSuccess { get; init; }
    public CredentialStatus ResultStatus { get; init; }
    public string? ErrorMessage { get; init; }

    public static TestCredentialResult Success(CredentialStatus status) => new() { IsSuccess = true, ResultStatus = status };
    public static TestCredentialResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

/// <summary>Revokes a doctor provider credential.</summary>
public sealed record RevokeCredentialCommand : IRequest<RevokeCredentialResult>
{
    public required Guid CredentialId { get; init; }
}

public sealed record RevokeCredentialResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static RevokeCredentialResult Success() => new() { IsSuccess = true };
    public static RevokeCredentialResult Failure(string error) => new() { IsSuccess = false, ErrorMessage = error };
}

public sealed record BeginProviderAuthorizationCommand : IRequest<BeginProviderAuthorizationResult>
{
    public required Guid CredentialId { get; init; }
}

public sealed record BeginProviderAuthorizationResult
{
    public bool IsSuccess { get; init; }
    public Uri? AuthorizationUri { get; init; }
    public string? ErrorMessage { get; init; }

    public static BeginProviderAuthorizationResult Success(Uri authorizationUri) =>
        new() { IsSuccess = true, AuthorizationUri = authorizationUri };

    public static BeginProviderAuthorizationResult Failure(string error) =>
        new() { ErrorMessage = error };
}

public sealed record CompleteProviderAuthorizationCommand : IRequest<CompleteProviderAuthorizationResult>
{
    public required Guid CredentialId { get; init; }
    public required string AuthorizationCode { get; init; }
    public required string State { get; init; }
}

public sealed record CompleteProviderAuthorizationResult
{
    public bool IsSuccess { get; init; }
    public string? ErrorMessage { get; init; }

    public static CompleteProviderAuthorizationResult Success() => new() { IsSuccess = true };
    public static CompleteProviderAuthorizationResult Failure(string error) => new() { ErrorMessage = error };
}

/// <summary>Returns doctor × provider credential grid for a clinic.</summary>
public sealed record GetCredentialSetupGridQuery : IRequest<IReadOnlyList<CredentialSetupDto>>
{
    public required Guid ClinicId { get; init; }
    public string? ProviderKey { get; init; }
}

public sealed record CredentialSetupDto
{
    public Guid CredentialId { get; init; }
    public Guid DoctorId { get; init; }
    public string DoctorName { get; init; } = string.Empty;
    public string ProviderKey { get; init; } = string.Empty;
    public Guid ClinicId { get; init; }
    public string? ProviderDoctorIdentifier { get; init; }
    public string? ProviderUsername { get; init; }
    public string? ProviderClinicIdentifier { get; init; }
    public CredentialStatus Status { get; init; }
    public DateTime? ExpiresAt { get; init; }
    public int DaysUntilExpiry { get; init; }
    public bool IsExpiringSoon { get; init; }
    public DateTime? LastTestedAt { get; init; }
}
