using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.ApiClients.Application.Abstractions;
using Vitarish.AuthenticationClients.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Accounts.Interface.Tests.TestInfrastructure;

internal sealed record ServiceMocks(
    Mock<ICipherService> Cipher,
    Mock<IUsersService> Users,
    Mock<ISmsAuthenticationService> SmsAuthentication,
    Mock<IEmailAuthenticationService> EmailAuthentication,
    Mock<IIdentityServerInteractionService> IdentityInteraction,
    Mock<IApiClientsService> ApiClients,
    Mock<IPersonalAccessTokenService> PersonalAccessTokens,
    Mock<IAuthenticationClientsService> AuthenticationClients,
    Mock<IRequestIdentitiesService> RequestIdentities,
    Mock<IUserStore<User>> UserStore,
    Mock<UserManager<User>> UserManager,
    TestSignInManager SignInManager,
    DefaultHttpContext HttpContext);
