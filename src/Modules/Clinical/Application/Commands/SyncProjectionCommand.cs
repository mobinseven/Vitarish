using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record SyncProjectionCommand(
    string ResourceType,
    string ResourceId,
    Guid? ResourceLinkId = null) : IRequest<ProjectionReadDto>;
