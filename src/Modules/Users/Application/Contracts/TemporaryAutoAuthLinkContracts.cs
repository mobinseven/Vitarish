namespace Vitarish.Users.Application.Contracts;

public sealed record IssueTemporaryAutoAuthLinkRequest(
    string UserId,
    string DestinationPath,
    string Purpose,
    string? CreatedByDeviceHint = null);

public sealed record IssueTemporaryAutoAuthLinkResult(
    Guid LinkId,
    string RawCode,
    string Purpose,
    string DestinationPath,
    DateTime CreatedAt,
    DateTime ExpiresAt);

public sealed record RedeemTemporaryAutoAuthLinkRequest(
    string RawCode,
    string Purpose,
    string? ConsumedByIp = null,
    string? ConsumedByUserAgent = null);

public sealed record RedeemTemporaryAutoAuthLinkResult(
    bool Succeeded,
    Guid? LinkId = null,
    string? UserId = null,
    string? DestinationPath = null);
