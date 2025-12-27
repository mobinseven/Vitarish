using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Vitarish.Accounts.Application.Abstractions;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.ApiClients.Application.Abstractions;
using Vitarish.AuthenticationClients.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Accounts.Interface.Tests.TestInfrastructure;

internal sealed record ServiceMocks(
    Mock<ICipherService> Cipher,
    Mock<IUsersService> Users,
    Mock<IUserService> UserService,
    Mock<ISmsAuthenticationService> SmsAuthentication,
    Mock<IEmailAuthenticationService> EmailAuthentication,
    Mock<IIdentityServerInteractionService> IdentityInteraction,
    Mock<IApiClientsService> ApiClients,
    Mock<IPersonalAccessTokenService> PersonalAccessTokens,
    Mock<IAuthenticationClientsService> AuthenticationClients,
    Mock<IRequestIdentitiesService> RequestIdentities,
    Mock<IRequestContext> RequestContext,
    Mock<IPartiesService> Parties,
    Mock<IUserStore<User>> UserStore,
    Mock<IRoleStore<IdentityRole>> RoleStore,
    Mock<UserManager<User>> UserManager,
    Mock<RoleManager<IdentityRole>> RoleManager,
    TestSignInManager SignInManager,
    DefaultHttpContext HttpContext,
    Mock<IHttpClientFactory> HttpClientFactory,
    Mock<MudBlazor.IDialogService> DialogService);
