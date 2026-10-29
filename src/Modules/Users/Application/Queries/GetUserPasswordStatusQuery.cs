using MediatR;

namespace Vitarish.Users.Application.Queries;

public sealed record GetUserPasswordStatusQuery(string UserId) : IRequest<UserPasswordStatus?>;

public sealed record UserPasswordStatus(bool PasswordSet, DateTime? PasswordChangedAt);
