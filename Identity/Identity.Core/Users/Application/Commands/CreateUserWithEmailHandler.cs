using System;
using MediatR;
using Vitarish.Base.Domain.Results;
using Vitarish.Identity.Core.Users.Domain.Entities;
using Vitarish.Identity.Core.Users.Domain.Events;

namespace Vitarish.Identity.Core.Users.Application.Commands;

/// <summary>
/// Handles <see cref="CreateUserWithEmail"/> commands.
/// </summary>
internal sealed class CreateUserWithEmailHandler(IPublisher publisher)
    : IRequestHandler<CreateUserWithEmail, Result<User>>
{
    public async Task<Result<User>> Handle(CreateUserWithEmail request, CancellationToken cancellationToken)
    {
        User user = new(
            Guid.NewGuid().ToString(),
            request.Email,
            request.Email,
            phoneNumber: null,
            request.FirstName,
            request.LastName);

        await publisher.Publish(new UserCreatedEvent(user.Id), cancellationToken);

        return Result.Success(user);
    }
}
