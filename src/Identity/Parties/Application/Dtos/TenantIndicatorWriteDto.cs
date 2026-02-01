using Vitarish.Entities.Server.Models;
using Vitarish.Parties.Domain.Entities;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Parties.Application.Dtos;

/// <summary>
/// DTO for writing TenantIndicator entities.
/// </summary>
public record TenantIndicatorWriteDto : EntityWriteDto
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
