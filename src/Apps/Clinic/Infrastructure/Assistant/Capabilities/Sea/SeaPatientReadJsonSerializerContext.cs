using System.Text.Json.Serialization;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Infrastructure.Assistant.Capabilities.Sea;

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(SeaProcedureListResponse))]
[JsonSerializable(typeof(SeaEventListResponse))]
[JsonSerializable(typeof(SeaEventDetailResponse))]
[JsonSerializable(typeof(SeaAttachmentListResponse))]
internal sealed partial class SeaPatientReadJsonSerializerContext : JsonSerializerContext;

internal sealed record SeaProcedureListResponse(IReadOnlyList<SeaProcedureRead> Procedures, bool HasMore);
internal sealed record SeaProcedureRead(Guid Id, string Name, string? Description, StatsReadDto? Stats, DateTime SourceUpdatedAt);
internal sealed record SeaEventListResponse(bool Found, IReadOnlyList<SeaEventRead>? Events = null, bool HasMore = false);
internal sealed record SeaEventRead(Guid Id, string Title, bool IsCommitted, DateTime? CommittedAt, DateTime SourceUpdatedAt);
internal sealed record SeaEventDetailResponse(bool Found, SeaEventDetail? Event = null);
internal sealed record SeaEventDetail(Guid Id, string Title, string? Description, bool IsCommitted, DateTime? CommittedAt, DateTime SourceUpdatedAt);
internal sealed record SeaAttachmentListResponse(bool Found, IReadOnlyList<SeaAttachmentRead>? Attachments = null, bool HasMore = false);
internal sealed record SeaAttachmentRead(Guid Id, string? FileName, string? ContentType, long Size, string Kind, DateTime SourceUpdatedAt);
