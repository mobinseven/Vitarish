namespace Vitarish.Modules.Base.Endpoints;

public sealed record ProtectedModuleEndpointMetadata(string Scheme);
public sealed record AnonymousModuleEndpointMetadata(string Justification);
public sealed record WebhookEndpointMetadata(string Provider, string VerificationStrategy);
public sealed record FileUploadEndpointMetadata(string FormFileFieldName);
public sealed record BinaryDownloadEndpointMetadata(string ContentType);
public sealed record StreamingEndpointMetadata(string ContentType);
public sealed record LongRunningCommandEndpointMetadata;
public sealed record OpenApiExclusionJustificationMetadata(string Justification);
