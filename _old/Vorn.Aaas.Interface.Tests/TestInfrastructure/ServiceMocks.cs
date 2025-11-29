using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Moq;
using Vorn.Aaas.ApiClients.Presentation.Services;
using Vorn.Aaas.AuthenticationClients.Presentation.Services;
using Vorn.Aaas.Interface.Infrastructure.Services;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Presentation.Service;

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
    Mock<IUserStore<User>> UserStore,
    Mock<UserManager<User>> UserManager,
    TestSignInManager SignInManager,
    DefaultHttpContext HttpContext);
