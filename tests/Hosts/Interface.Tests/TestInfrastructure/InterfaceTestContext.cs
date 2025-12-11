using System.Collections.Generic;
using System.Security.Claims;
using Bunit;
using Bunit.JSInterop;
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
using Moq;
using MudBlazor;
using Vitarish.Interface.Infrastructure.Localization;
using Vitarish.Interface.Infrastructure.Services;
using Vitarish.Interface.Presentation.Components.Base;
using Vitarish.Users.Domain.Entities;
using MudBlazor.Services;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.AuthenticationClients.Application.Abstractions;
using Vitarish.ApiClients.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;

namespace Vitarish.Interface.Tests.TestInfrastructure;

internal sealed class InterfaceTestContext : TestContext
{
    public ServiceMocks Mocks { get; }
    public RecordingNavigationManager Navigation { get; }

    public InterfaceTestContext()
    {
        Navigation = new RecordingNavigationManager();
        Services.RemoveAll<NavigationManager>();
        Services.AddSingleton<NavigationManager>(Navigation);

        Services.AddLogging();
        Services.AddMudServices();
        Services.AddSingleton<IStringLocalizer<Strings>>(new TestStringLocalizer());
        Services.AddSingleton<AuthenticationStateProvider>(new TestAuthenticationStateProvider());
        Services.AddSingleton<IJSRuntime>(new TestJsRuntime());
        Services.AddScoped(sp => new IdentityRedirectManager(
            sp.GetRequiredService<NavigationManager>(),
            NullLogger<IdentityRedirectManager>.Instance));

        var cipherMock = new Mock<ICipherService>();
        Services.AddSingleton(cipherMock.Object);

        var usersServiceMock = new Mock<IUsersService>();
        Services.AddSingleton(usersServiceMock.Object);

        var smsAuthenticationMock = new Mock<ISmsAuthenticationService>();
        Services.AddSingleton(smsAuthenticationMock.Object);

        var emailAuthenticationMock = new Mock<IEmailAuthenticationService>();
        Services.AddSingleton(emailAuthenticationMock.Object);

        var apiClientsServiceMock = new Mock<IApiClientsService>();
        Services.AddSingleton(apiClientsServiceMock.Object);

        var personalAccessTokenServiceMock = new Mock<IPersonalAccessTokenService>();
        Services.AddSingleton(personalAccessTokenServiceMock.Object);

        var authenticationClientsServiceMock = new Mock<IAuthenticationClientsService>();
        Services.AddSingleton(authenticationClientsServiceMock.Object);

        var requestIdentitiesServiceMock = new Mock<IRequestIdentitiesService>();
        Services.AddSingleton(requestIdentitiesServiceMock.Object);

        var identityInteractionMock = new Mock<IIdentityServerInteractionService>();
        Services.AddSingleton(identityInteractionMock.Object);

        var userStoreMock = new Mock<IUserStore<User>>();
        userStoreMock.As<IUserPhoneNumberStore<User>>();
        userStoreMock.As<IUserEmailStore<User>>();
        Services.AddSingleton<IUserStore<User>>(userStoreMock.Object);

        var userManagerMock = CreateUserManager(userStoreMock);
        Services.AddSingleton<UserManager<User>>(userManagerMock.Object);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, "AuthenticatedUser") },
                "Cookies"))
        };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(httpContext);
        Services.AddSingleton<IHttpContextAccessor>(httpContextAccessorMock.Object);

        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();
        claimsFactoryMock
            .Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) =>
                new ClaimsPrincipal(new ClaimsIdentity(
                    new[] { new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty) },
                    "Cookies")));

        var options = Options.Create(new IdentityOptions());
        var signInLogger = new Mock<ILogger<SignInManager<User>>>();
        var schemeProviderMock = new Mock<IAuthenticationSchemeProvider>();
        var confirmationMock = new Mock<IUserConfirmation<User>>();

        var signInManager = new TestSignInManager(
            userManagerMock.Object,
            httpContextAccessorMock.Object,
            claimsFactoryMock.Object,
            options,
            signInLogger.Object,
            schemeProviderMock.Object,
            confirmationMock.Object);
        Services.AddSingleton<SignInManager<User>>(signInManager);

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
        var options = new Mock<IOptions<IdentityOptions>>();
        options.SetupGet(o => o.Value).Returns(new IdentityOptions());

        var passwordHasher = new Mock<IPasswordHasher<User>>();
        var userValidators = new List<IUserValidator<User>>();
        var passwordValidators = new List<IPasswordValidator<User>>();
        var keyNormalizer = new Mock<ILookupNormalizer>();
        var errors = new IdentityErrorDescriber();
        var services = new Mock<IServiceProvider>();
        var logger = new Mock<ILogger<UserManager<User>>>();

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
