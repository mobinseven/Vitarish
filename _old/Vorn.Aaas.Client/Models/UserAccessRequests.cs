namespace Vorn.Aaas.Client.Models;

internal sealed record SetUserAccessRequest(string UserId, string ClientId, string AccessValue);
internal sealed record RemoveUserAccessRequest(string UserId, string ClientId);
