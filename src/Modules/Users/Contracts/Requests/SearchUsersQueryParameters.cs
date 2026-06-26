namespace Vitarish.Users.Contracts.Requests;

public sealed record SearchUsersQueryParameters(string? Query, int? MaxResults, bool IncludeTenantUsers = false);
