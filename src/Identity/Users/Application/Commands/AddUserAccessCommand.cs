using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Commands;

public record AddUserAccessCommand(UserAccess UserAccess) : IRequest;
