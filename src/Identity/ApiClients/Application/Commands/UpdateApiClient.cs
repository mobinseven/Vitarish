using MediatR;
using Vitarish.ApiClients.Domain.Entities;

namespace Vitarish.ApiClients.Application.Commands;
/// <summary>
/// Command to edit an existing <see cref="ApiClient"/>.
/// </summary>
public sealed record UpdateApiClient(string Id, string Secret, string Scopes, string HostUri) : IRequest<ApiClient?>;
