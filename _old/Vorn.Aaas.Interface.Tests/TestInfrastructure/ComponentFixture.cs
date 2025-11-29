using System.Collections.Generic;
using System.Reflection;
using System.Security.Claims;
using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using Moq;
using MudBlazor;
using Vorn.Aaas.ApiClients.Presentation.Services;
using Vorn.Aaas.AuthenticationClients.Presentation.Services;
using Vorn.Aaas.Interface.Infrastructure.Localization;
using Vorn.Aaas.Interface.Infrastructure.Services;
using Vorn.Aaas.Interface.Presentation.Components.Base;
using Vorn.Aaas.Users.Domain.Entities;
using Vorn.Aaas.Users.Presentation.Service;

namespace Vorn.Aaas.Interface.Tests.TestInfrastructure;

internal sealed class ComponentFixture : IDisposable
{
    public RecordingNavigationManager Navigation { get; } = new();
    public ServiceMocks Mocks { get; }
    public IdentityRedirectManager RedirectManager { get; }
    public TestAuthenticationStateProvider AuthenticationState { get; } = new();
    public IStringLocalizer<Strings> Localizer { get; } = new TestStringLocalizer();
    public IJSRuntime JsRuntime { get; } = new TestJsRuntime();
    public ISnackbar Snackbar { get; } = Mock.Of<ISnackbar>();

    public ComponentFixture()
    {
        var cipherMock = new Mock<ICipherService>();
        var usersServiceMock = new Mock<IUsersService>();
        var smsAuthMock = new Mock<ISmsAuthenticationService>();
        var emailAuthMock = new Mock<IEmailAuthenticationService>();
        var apiClientsMock = new Mock<IApiClientsService>();
        var personalAccessTokensMock = new Mock<IPersonalAccessTokenService>();
        var authClientsMock = new Mock<IAuthenticationClientsService>();
        var identityInteractionMock = new Mock<IIdentityServerInteractionService>();

        var userStoreMock = new Mock<IUserStore<User>>();
        userStoreMock.As<IUserPhoneNumberStore<User>>();
        userStoreMock.As<IUserEmailStore<User>>();

        var userManagerMock = CreateUserManager(userStoreMock);

        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "AuthenticatedUser"),
                new Claim(ClaimTypes.NameIdentifier, "AuthenticatedUserId"),
            }, IdentityConstants.ApplicationScheme))
        };

        var httpContextAccessorMock = new Mock<IHttpContextAccessor>();
        httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(httpContext);

        var claimsFactoryMock = new Mock<IUserClaimsPrincipalFactory<User>>();
        claimsFactoryMock
            .Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) =>
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty) }, "Cookies")));

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

        RedirectManager = new IdentityRedirectManager(Navigation, NullLogger<IdentityRedirectManager>.Instance)
        {
            HttpContext = httpContext
        };

        Mocks = new ServiceMocks(
            cipherMock,
            usersServiceMock,
            smsAuthMock,
            emailAuthMock,
            identityInteractionMock,
            apiClientsMock,
            personalAccessTokensMock,
            authClientsMock,
            userStoreMock,
            userManagerMock,
            signInManager,
            httpContext);
    }

    public void ConfigureCommonComponent(CommonBase component)
    {
        SetProperty(component, "CipherService", Mocks.Cipher.Object, typeof(CommonBase));
        SetProperty(component, "UserService", Mocks.Users.Object, typeof(CommonBase));
        SetProperty(component, "UserStore", Mocks.UserStore.Object, typeof(CommonBase));
        SetProperty(component, "SignInManager", Mocks.SignInManager, typeof(CommonBase));
        SetProperty(component, "NavigationManager", Navigation, typeof(CommonBase));
        SetProperty(component, "Localizer", Localizer, typeof(CommonBase));
        SetProperty(component, "RedirectManager", RedirectManager, typeof(CommonBase));
        SetProperty(component, "Snackbar", Snackbar, typeof(CommonBase));
        SetProperty(component, "Js", JsRuntime, typeof(CommonBase));
        SetProperty(component, "IdentityServerInteractionService", Mocks.IdentityInteraction.Object, typeof(CommonBase));
        SetProperty(component, "InteractionService", Mocks.IdentityInteraction.Object, typeof(CommonBase));
        SetProperty(component, "ApiClientService", Mocks.ApiClients.Object, typeof(CommonBase));
        SetProperty(component, "PersonalAccessTokenService", Mocks.PersonalAccessTokens.Object, typeof(CommonBase));
        SetProperty(component, "AuthenticationClientsService", Mocks.AuthenticationClients.Object, typeof(CommonBase));
        SetProperty(component, "EmailAuthenticationService", Mocks.EmailAuthentication.Object, typeof(CommonBase));
        SetProperty(component, "SmsAuthenticationService", Mocks.SmsAuthentication.Object, typeof(CommonBase));
        SetProperty(component, "AuthenticationStateProvider", AuthenticationState, typeof(CommonBase));
    }

    static void SetProperty(object instance, string propertyName, object value, Type declaringType)
        => declaringType
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?.SetValue(instance, value);

    static Mock<UserManager<User>> CreateUserManager(Mock<IUserStore<User>> userStoreMock)
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

    public void Dispose() { }
}
