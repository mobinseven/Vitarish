using MediatR;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Security;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CheckUserPhoneNumberInUseQueryHandler(ISender sender) : IRequestHandler<CheckUserPhoneNumberInUseQuery, bool>
{
    public Task<bool> Handle(CheckUserPhoneNumberInUseQuery request, CancellationToken cancellationToken)
    {
        if (!UserPhoneNumberPolicy.TryNormalize(request.PhoneNumber, out string? phoneNumber))
            return Task.FromResult(false);

        return sender.Send(new CheckLoginIdentifierInUseQuery(phoneNumber, LoginIdentifierKind.Phone, request.ExcludeUserId), cancellationToken);
    }
}
