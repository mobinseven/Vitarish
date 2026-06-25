using Vitarish.Entities.Contracts;
namespace Vitarish.Parties.Contracts.Tenants;
public enum IndicatorMethod { Hostname = 1, Subdomain = 2 }
public enum IndicatorStatus { PendingVerification = 1, Verified = 2, Active = 3, Disabled = 4 }
public enum IndicatorVerificationMethod { DnsTxt = 1, HttpFile = 2 }
public sealed record VerificationInfoDto { public IndicatorVerificationMethod Method { get; init; } public required string Evidence { get; init; } public required string EvidenceExpectedHash { get; init; } public string? Token { get; init; } }
public record TenantReadDto : EntityReadDto { public Guid OwnerId { get; set; } public required string Name { get; set; } public string? LogoUrl { get; set; } public string? Description { get; set; } public required string AppId { get; set; } public new Dictionary<string, object?> Metadata { get; set; } = []; }
public record TenantWriteDto : EntityWriteDto { public Guid OwnerId { get; set; } public string? Name { get; set; } public string? LogoUrl { get; set; } public string? Description { get; set; } public string? AppId { get; set; } public new Dictionary<string, object?> Metadata { get; set; } = []; }
public record TenantIndicatorReadDto : EntityReadDto { public Guid TenantId { get; set; } public IndicatorMethod Method { get; set; } public required string Value { get; set; } public IndicatorStatus Status { get; set; } public bool IsPrimary { get; set; } public VerificationInfoDto? Verification { get; set; } public DateTime? VerifiedAt { get; set; } public DateTime? LastCheckedAt { get; set; } }
public record TenantIndicatorWriteDto : EntityWriteDto { public Guid TenantId { get; set; } public IndicatorMethod Method { get; set; } public required string Value { get; set; } public IndicatorStatus Status { get; set; } public bool IsPrimary { get; set; } public VerificationInfoDto? Verification { get; set; } public DateTime? VerifiedAt { get; set; } public DateTime? LastCheckedAt { get; set; } }
public record SetupTenantRequest(string OwnerName,string TenantName,string AppId,IndicatorMethod IndicatorMethod,string IndicatorValue,IndicatorStatus IndicatorStatus,IReadOnlyList<string> SelectedModules,string UserId);
