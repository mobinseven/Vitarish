namespace Vorn.Aaas.Users.Application.Auditing;

public interface IUserAccessAuditLogger
{
    Task LogAsync(UserAccessAuditEntry entry, CancellationToken cancellationToken = default);
}
