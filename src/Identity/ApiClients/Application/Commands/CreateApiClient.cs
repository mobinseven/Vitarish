using MediatR;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Application.Commands;
/// <summary>
/// Command to create a new <see cref="ApiClient"/>.
/// </summary>
public sealed record CreateApiClient(string Id, string Secret, string Scopes, string HostUri) : IRequest<ApiClient?>;
