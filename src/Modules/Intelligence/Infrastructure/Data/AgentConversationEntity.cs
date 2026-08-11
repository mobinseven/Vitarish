namespace Vitarish.Intelligence.Infrastructure.Data;

public sealed class AgentConversationEntity
{
    public Guid Id { get; set; }
    public string TenantId { get; set; } = string.Empty;
    public string CreatingUserId { get; set; } = string.Empty;
    public string AgentKey { get; set; } = string.Empty;
    public string SubjectKind { get; set; } = string.Empty;
    public string SubjectId { get; set; } = string.Empty;
    public string? ProtectedHistory { get; set; }
    public string? ProtectedPresentationTranscript { get; set; }
    public string? Title { get; set; }
    public string? LatestTextPreview { get; set; }
    public string HistoryFormat { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public long Revision { get; set; }
    public Guid? ActiveLeaseId { get; set; }
    public DateTimeOffset? ActiveLeaseExpiresAt { get; set; }
}
