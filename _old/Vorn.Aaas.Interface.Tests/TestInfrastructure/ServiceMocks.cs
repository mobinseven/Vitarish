using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Vorn.Aaas.ApiClients.Presentation.Abstractions;
using Vorn.Aaas.AuthenticationClients.Presentation.Abstractions;
using Vorn.Aaas.Interface.Infrastructure.Services;
using Vorn.Aaas.RequestIdentities.Presentation.Abstractions;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Presentation.Abstractions;

namespace Vorn.Aaas.Interface.Tests.TestInfrastructure;

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
