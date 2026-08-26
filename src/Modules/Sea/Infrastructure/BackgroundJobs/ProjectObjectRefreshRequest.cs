namespace Vitarish.Sea.Infrastructure.BackgroundJobs;

public sealed record ProjectObjectRefreshRequest(string TenantId, string ObjectType);
