using Bunit;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor.Services;
using System.Security.Claims;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.ApiClients.Application.Abstractions;
using Vitarish.AuthenticationClients.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Accounts.Interface.Tests.TestInfrastructure;

internal sealed class InterfaceTestContext : TestContext
{
    public ServiceMocks Mocks { get; }
    public RecordingNavigationManager Navigation { get; }

    public InterfaceTestContext()
    {
        Navigation = new RecordingNavigationManager();
        _ = Services.RemoveAll<NavigationManager>();
        _ = Services.AddSingleton<NavigationManager>(Navigation);

        _ = Services.AddLogging();
        _ = Services.AddMudServices();
        _ = Services.AddSingleton<IStringLocalizer>(new TestStringLocalizer());
        _ = Services.AddSingleton<AuthenticationStateProvider>(new TestAuthenticationStateProvider());
        _ = Services.AddSingleton<IJSRuntime>(new TestJsRuntime());
        _ = Services.AddScoped(sp => new IdentityRedirectManager(
            sp.GetRequiredService<NavigationManager>(),
            NullLogger<IdentityRedirectManager>.Instance));

        Mock<ICipherService> cipherMock = new();
        _ = Services.AddSingleton(cipherMock.Object);

        Mock<IUsersService> usersServiceMock = new();
        _ = Services.AddSingleton(usersServiceMock.Object);

        Mock<ISmsAuthenticationService> smsAuthenticationMock = new();
        _ = Services.AddSingleton(smsAuthenticationMock.Object);

        Mock<IEmailAuthenticationService> emailAuthenticationMock = new();
        _ = Services.AddSingleton(emailAuthenticationMock.Object);

        Mock<IApiClientsService> apiClientsServiceMock = new();
        _ = Services.AddSingleton(apiClientsServiceMock.Object);

        Mock<IPersonalAccessTokenService> personalAccessTokenServiceMock = new();
        _ = Services.AddSingleton(personalAccessTokenServiceMock.Object);

        Mock<IAuthenticationClientsService> authenticationClientsServiceMock = new();
        _ = Services.AddSingleton(authenticationClientsServiceMock.Object);

        Mock<IRequestIdentitiesService> requestIdentitiesServiceMock = new();
        _ = Services.AddSingleton(requestIdentitiesServiceMock.Object);

        Mock<IIdentityServerInteractionService> identityInteractionMock = new();
        _ = Services.AddSingleton(identityInteractionMock.Object);

        Mock<IUserStore<User>> userStoreMock = new();
        _ = userStoreMock.As<IUserPhoneNumberStore<User>>();
        _ = userStoreMock.As<IUserEmailStore<User>>();
        _ = Services.AddSingleton<IUserStore<User>>(userStoreMock.Object);

        Mock<UserManager<User>> userManagerMock = CreateUserManager(userStoreMock);
        _ = Services.AddSingleton<UserManager<User>>(userManagerMock.Object);

        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "AuthenticatedUser") },
                "Cookies"))
        };

        Mock<IHttpContextAccessor> httpContextAccessorMock = new();
        _ = httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(httpContext);
        _ = Services.AddSingleton<IHttpContextAccessor>(httpContextAccessorMock.Object);

        Mock<IUserClaimsPrincipalFactory<User>> claimsFactoryMock = new();
        _ = claimsFactoryMock
            .Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) =>
                new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty) },
                    "Cookies")));

        IOptions<IdentityOptions> options = Options.Create(new IdentityOptions());
        Mock<ILogger<SignInManager<User>>> signInLogger = new();
        Mock<IAuthenticationSchemeProvider> schemeProviderMock = new();
        Mock<IUserConfirmation<User>> confirmationMock = new();

        TestSignInManager signInManager = new(
            userManagerMock.Object,
            httpContextAccessorMock.Object,
            claimsFactoryMock.Object,
            options,
            signInLogger.Object,
            schemeProviderMock.Object,
            confirmationMock.Object);
        _ = Services.AddSingleton<SignInManager<User>>(signInManager);

        Mocks = new ServiceMocks(
            cipherMock,
            usersServiceMock,
            smsAuthenticationMock,
            emailAuthenticationMock,
            identityInteractionMock,
            apiClientsServiceMock,
            personalAccessTokenServiceMock,
            authenticationClientsServiceMock,
            requestIdentitiesServiceMock,
            userStoreMock,
            userManagerMock,
            signInManager,
            httpContext);
    }

    public void SetupOtpModules()
    {
        // No-op: handled by TestJsRuntime.
    }

    private static Mock<UserManager<User>> CreateUserManager(Mock<IUserStore<User>> userStoreMock)
    {
        Mock<IOptions<IdentityOptions>> options = new();
        _ = options.SetupGet(o => o.Value).Returns(new IdentityOptions());

        Mock<IPasswordHasher<User>> passwordHasher = new();
        List<IUserValidator<User>> userValidators = [];
        List<IPasswordValidator<User>> passwordValidators = [];
        Mock<ILookupNormalizer> keyNormalizer = new();
        IdentityErrorDescriber errors = new();
        Mock<IServiceProvider> services = new();
        Mock<ILogger<UserManager<User>>> logger = new();

        return new Mock<UserManager<User>>(
            userStoreMock.Object,
            options.Object,
            passwordHasher.Object,
            userValidators,
            passwordValidators,
            keyNormalizer.Object,
            errors,
            services.Object,
            logger.Object);
    }
}
