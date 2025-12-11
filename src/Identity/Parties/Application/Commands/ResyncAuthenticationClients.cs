using MediatR;

namespace Vitarish.Parties.Application.Commands;

/// <summary>
/// Command to resync authentication clients from tenant indicators.
/// </summary>
public sealed record ResyncAuthenticationClients(Guid? TenantId = null, bool DryRun = false) : IRequest<ResyncAuthenticationClientsResult>;

/// <summary>
/// Result of resync operation.
/// </summary>
public sealed record ResyncAuthenticationClientsResult(int Created, int Updated, int Deleted, int Unchanged);
