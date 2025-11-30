
using System.ComponentModel.DataAnnotations;

namespace Vorn.Aaas.RequestIdentities.Domain.Entities;

public sealed class RequestIdentity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    [MaxLength(64)]
    public string RequestId { get; set; } = default!;

    [MaxLength(256)]
    public string Authority { get; set; } = default!;

    [MaxLength(128)]
    public string ClientId { get; set; } = default!;

    [MaxLength(128)]
    public string OwnerId { get; set; } = default!;

    [MaxLength(256)]
    public string? TenantId { get; set; }

    [MaxLength(128)]
    public string? UserId { get; set; }

    [MaxLength(512)]
    public string? ResourceId { get; set; }

    [MaxLength(64)]
    public string? TraceId { get; set; }

    [MaxLength(2048)]
    public string? AdditionalInfo { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}
