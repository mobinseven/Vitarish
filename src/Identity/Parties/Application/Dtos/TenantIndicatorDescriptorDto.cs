using Vitarish.Entities.Server.Models;
using Vitarish.Parties.Domain.Enums;

namespace Vitarish.Parties.Application.Dtos;

/// <summary>
/// DTO for filtering and querying TenantIndicator entities.
/// </summary>
public record TenantIndicatorDescriptorDto : EntityDescriptorDto
{
    public Guid? TenantId { get; set; }
    public IndicatorMethod? Method { get; set; }
    public string? Value { get; set; }
    public IndicatorStatus? Status { get; set; }
    public bool? IsPrimary { get; set; }
}
