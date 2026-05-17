using Vitarish.Users.Application.Contracts;

namespace Vitarish.Users.Application.Abstractions;

public interface ITemporaryAutoAuthLinkIssuer
{
    Task<IssueTemporaryAutoAuthLinkResult> IssueAsync(
        IssueTemporaryAutoAuthLinkRequest request,
        CancellationToken cancellationToken = default);
}
