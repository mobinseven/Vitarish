using Vitarish.Entities.Server.Models;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Parties.Domain.Dtos;

/// <summary>
/// DTO for reading TenantIndicator entities.
/// </summary>
public record TenantIndicatorReadDto : EntityReadDto
{
    public Guid TenantId { get; set; }
    public IndicatorMethod Method { get; set; }
    public required string Value { get; set; }
    public IndicatorStatus Status { get; set; }
    public bool IsPrimary { get; set; }
    public VerificationInfo? Verification { get; set; }
    public DateTimeOffset? VerifiedAt { get; set; }
    public DateTimeOffset? LastCheckedAt { get; set; }
}
