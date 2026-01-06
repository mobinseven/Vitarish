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
using Vitarish.Accounts.Application.Abstractions;
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
        Mock<IUserService> userServiceMock = new();
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
        Mock<IDialogService> dialogServiceMock = new();

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
                new Claim("sub", "user-1"),
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
            userServiceMock,
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
            httpClientFactoryMock,
            dialogServiceMock);
    }

    public void ConfigureCommonComponent(IdentityCommonBase component)
    {
        SetProperty(component, "CipherService", Mocks.Cipher.Object, typeof(IdentityCommonBase));
        SetProperty(component, "UserService", Mocks.UserService.Object, typeof(IdentityCommonBase));
        SetProperty(component, "UserStore", Mocks.UserStore.Object, typeof(IdentityCommonBase));
        SetProperty(component, "SignInManager", Mocks.SignInManager, typeof(IdentityCommonBase));
        SetProperty(component, "NavigationManager", Navigation, typeof(IdentityCommonBase));
        SetProperty(component, "Localizer", Localizer, typeof(IdentityCommonBase));
        SetProperty(component, "RedirectManager", RedirectManager, typeof(IdentityCommonBase));
        SetProperty(component, "Snackbar", Snackbar, typeof(IdentityCommonBase));
        SetProperty(component, "Js", JsRuntime, typeof(IdentityCommonBase));
        SetProperty(component, "IdentityServerInteractionService", Mocks.IdentityInteraction.Object, typeof(IdentityCommonBase));
        SetProperty(component, "InteractionService", Mocks.IdentityInteraction.Object, typeof(IdentityCommonBase));
        SetProperty(component, "ApiClientService", Mocks.ApiClients.Object, typeof(IdentityCommonBase));
        SetProperty(component, "PersonalAccessTokenService", Mocks.PersonalAccessTokens.Object, typeof(IdentityCommonBase));
        SetProperty(component, "AuthenticationClientsService", Mocks.AuthenticationClients.Object, typeof(IdentityCommonBase));
        SetProperty(component, "RequestIdentitiesService", Mocks.RequestIdentities.Object, typeof(IdentityCommonBase));
        if (component is AdminBase)
        {
            SetProperty(component, "PartiesService", Mocks.Parties.Object, typeof(AdminBase));
        }
        SetProperty(component, "EmailAuthenticationService", Mocks.EmailAuthentication.Object, typeof(IdentityCommonBase));
        SetProperty(component, "SmsAuthenticationService", Mocks.SmsAuthentication.Object, typeof(IdentityCommonBase));
        SetProperty(component, "AuthenticationStateProvider", AuthenticationState, typeof(IdentityCommonBase));
        SetProperty(component, "HttpClientFactory", Mocks.HttpClientFactory.Object, typeof(IdentityCommonBase));
        SetProperty(component, "LoggerFactory", NullLoggerFactory.Instance, typeof(IdentityCommonBase));
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
