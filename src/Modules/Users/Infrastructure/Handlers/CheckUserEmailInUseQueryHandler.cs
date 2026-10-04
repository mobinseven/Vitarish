using MediatR;
using Vitarish.Users.Application.Queries;

namespace Vitarish.Users.Infrastructure.Handlers;

public class CheckUserEmailInUseQueryHandler(ISender sender) : IRequestHandler<CheckUserEmailInUseQuery, bool>
{
    public Task<bool> Handle(CheckUserEmailInUseQuery request, CancellationToken cancellationToken) =>
        sender.Send(new CheckLoginIdentifierInUseQuery(request.Email, LoginIdentifierKind.Email, request.ExcludeUserId), cancellationToken);
}
