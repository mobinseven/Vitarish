using Vitarish.Users.Contracts.Requests;
using Vitarish.Users.Contracts.Responses;

namespace Vitarish.Users.Client;

public interface IUsersClient
{
    Task<UsersClientResult<IReadOnlyList<UserSummaryResponse>>> SearchUsersAsync(SearchUsersQueryParameters parameters, CancellationToken cancellationToken);
    Task<UsersClientResult<UserSummaryResponse>> GetUserAsync(string userId, CancellationToken cancellationToken);
    Task<UsersClientResult<UserSummaryResponse>> CreateUserWithPhoneAsync(CreateUserWithPhoneRequest request, CancellationToken cancellationToken);
    Task<UsersClientResult<UserSummaryResponse>> CreateUserWithEmailAsync(CreateUserWithEmailRequest request, CancellationToken cancellationToken);
    Task<UsersClientResult<UserExistenceResponse>> CheckUserPhoneNumberInUseAsync(CheckUserPhoneNumberInUseQueryParameters parameters, CancellationToken cancellationToken);
    Task<UsersClientResult<UserExistenceResponse>> CheckUserEmailInUseAsync(CheckUserEmailInUseQueryParameters parameters, CancellationToken cancellationToken);
    Task<UsersClientResult<UsersCommandResponse>> SetUserEmailAsync(string userId, SetUserEmailRequest request, CancellationToken cancellationToken);
    Task<UsersClientResult<UserRoleCheckResponse>> CheckUserRoleAsync(CheckUserRoleQueryParameters parameters, CancellationToken cancellationToken);
    Task<UsersClientResult<UsersCommandResponse>> AddUserToRoleAsync(string userId, AddUserToRoleRequest request, CancellationToken cancellationToken);
}
