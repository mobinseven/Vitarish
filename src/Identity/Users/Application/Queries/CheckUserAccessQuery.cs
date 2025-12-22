using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public record CheckUserAccessQuery(string Resource) : IRequest<bool>;
