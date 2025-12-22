using Duende.IdentityServer.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.JSInterop;
using MudBlazor;
using System.Reflection;
using System.Security.Claims;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Interface.Components.Base;
using Vitarish.Accounts.Interface.Localization;
using Vitarish.ApiClients.Application.Abstractions;
using Vitarish.AuthenticationClients.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Parties.Application.Abstractions;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Users.Domain.Entities;

namespace Accounts.Interface.Tests.TestInfrastructure;

internal sealed class ComponentFixture : IDisposable
{
    public RecordingNavigationManager Navigation { get; } = new();
    public ServiceMocks Mocks { get; }
    public IdentityRedirectManager RedirectManager { get; }
    public TestAuthenticationStateProvider AuthenticationState { get; } = new();
    public IStringLocalizer Localizer { get; } = new TestStringLocalizer();
    public IJSRuntime JsRuntime { get; } = new TestJsRuntime();
    public ISnackbar Snackbar { get; } = Mock.Of<ISnackbar>();

    public ComponentFixture()
    {
        Mock<ICipherService> cipherMock = new();
        Mock<IUsersService> usersServiceMock = new();
        Mock<ISmsAuthenticationService> smsAuthMock = new();
        Mock<IEmailAuthenticationService> emailAuthMock = new();
        Mock<IApiClientsService> apiClientsMock = new();
        Mock<IPersonalAccessTokenService> personalAccessTokensMock = new();
        Mock<IAuthenticationClientsService> authClientsMock = new();
        Mock<IIdentityServerInteractionService> identityInteractionMock = new();
        Mock<IRequestIdentitiesService> requestIdentitiesMock = new();
        Mock<IRequestContext> requestContextMock = new();
        Mock<IPartiesService> partiesServiceMock = new();
        Mock<IHttpClientFactory> httpClientFactoryMock = new();
        _ = httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

        Mock<IUserStore<User>> userStoreMock = new();
        _ = userStoreMock.As<IUserPhoneNumberStore<User>>();
        _ = userStoreMock.As<IUserEmailStore<User>>();

        Mock<UserManager<User>> userManagerMock = CreateUserManager(userStoreMock);

        Mock<IRoleStore<IdentityRole>> roleStoreMock = new();
        Mock<RoleManager<IdentityRole>> roleManagerMock = CreateRoleManager(roleStoreMock);

        DefaultHttpContext httpContext = new()
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, "AuthenticatedUser"),
                new Claim(ClaimTypes.NameIdentifier, "AuthenticatedUserId"),
            }, IdentityConstants.ApplicationScheme))
        };

        Mock<IHttpContextAccessor> httpContextAccessorMock = new();
        _ = httpContextAccessorMock.SetupGet(x => x.HttpContext).Returns(httpContext);

        Mock<IUserClaimsPrincipalFactory<User>> claimsFactoryMock = new();
        _ = claimsFactoryMock
            .Setup(x => x.CreateAsync(It.IsAny<User>()))
            .ReturnsAsync((User user) =>
                new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, user.Id ?? string.Empty) }, "Cookies")));

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
            requestIdentitiesMock,
            requestContextMock,
            partiesServiceMock,
            userStoreMock,
            roleStoreMock,
            userManagerMock,
            roleManagerMock,
            signInManager,
            httpContext,
            httpClientFactoryMock);
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
        SetProperty(component, "RequestIdentitiesService", Mocks.RequestIdentities.Object, typeof(CommonBase));
        if (component is AdminBase)
        {
            SetProperty(component, "PartiesService", Mocks.Parties.Object, typeof(AdminBase));
        }
        SetProperty(component, "EmailAuthenticationService", Mocks.EmailAuthentication.Object, typeof(CommonBase));
        SetProperty(component, "SmsAuthenticationService", Mocks.SmsAuthentication.Object, typeof(CommonBase));
        SetProperty(component, "AuthenticationStateProvider", AuthenticationState, typeof(CommonBase));
        SetProperty(component, "HttpClientFactory", Mocks.HttpClientFactory.Object, typeof(CommonBase));
        SetProperty(component, "LoggerFactory", NullLoggerFactory.Instance, typeof(CommonBase));
    }

    private static void SetProperty(object instance, string propertyName, object value, Type declaringType)
        => declaringType
            .GetProperty(propertyName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public)
            ?.SetValue(instance, value);

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

    private static Mock<RoleManager<IdentityRole>> CreateRoleManager(Mock<IRoleStore<IdentityRole>> roleStoreMock)
    {
        Mock<ILookupNormalizer> keyNormalizer = new();
        Mock<IdentityErrorDescriber> errors = new();
        Mock<ILogger<RoleManager<IdentityRole>>> logger = new();
        return new Mock<RoleManager<IdentityRole>>(
            roleStoreMock.Object,
            Array.Empty<IRoleValidator<IdentityRole>>(),
            keyNormalizer.Object,
            errors.Object,
            logger.Object);
    }

    public void Dispose() { }
}
