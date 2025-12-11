
using FluentValidation;
using MediatR;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Application.Queries;

public record GetUsersQuery : IRequest<IEnumerable<User>>;

public class GetUsersQueryValidator : AbstractValidator<GetUsersQuery> { }
