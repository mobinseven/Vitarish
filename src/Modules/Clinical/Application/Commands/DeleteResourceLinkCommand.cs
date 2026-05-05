using MediatR;

namespace Vitarish.Clinical.Application.Commands;

public sealed record DeleteResourceLinkCommand(Guid Id) : IRequest;
