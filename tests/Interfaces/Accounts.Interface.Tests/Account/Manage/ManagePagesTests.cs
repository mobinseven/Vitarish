using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using Bunit;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using Moq;
using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Interface.Components.Account;
using Vitarish.Accounts.Interface.Pages.Account.Manage;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Domain.Entities;
using Accounts.Interface.Tests.TestInfrastructure;

namespace Accounts.Interface.Tests.Account.Manage;

public class ManagePagesTests
{
    [Fact]
    public void Profile_ShowsHeaderAndEditor()
    {
        using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();

        IRenderedComponent<Profile> cut = context.RenderComponent<Profile>(
            parameters => parameters.AddCascadingValue(httpContext));

        Assert.Contains("Profile", cut.Markup);
        Assert.NotNull(cut.Find("h4"));
        Assert.NotNull(cut.FindComponent<Vitarish.Accounts.Interface.Components.Account.ProfileEditor>());
    }

    [Fact]
    public void Passkeys_LoadsCredentialsAndProvidesBackNavigation()
    {
        using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();
        PasskeyJsRuntime jsRuntime = new();
        _ = context.Services.AddSingleton<IJSRuntime>(jsRuntime);

        RecordingHttpMessageHandler handler = new();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<PasskeyCredentialDto>())
        });

        HttpClient client = new(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        context.Mocks.HttpClientFactory
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(client);

        IRenderedComponent<Passkeys> cut = context.RenderComponent<Passkeys>(
            parameters => parameters.AddCascadingValue(httpContext));

        Assert.Contains("mud-progress-linear", cut.Markup, StringComparison.OrdinalIgnoreCase);
        cut.WaitForAssertion(() => Assert.Contains("RegisterPasskey", cut.Markup));

        var backForm = cut.Find("form[action='/Account/Manage']");
        Assert.Contains("Profile", backForm.TextContent);
    }

    [Fact]
    public void Passkeys_RegisterAndDelete_InvokeExpectedEndpoints()
    {
        using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();
        PasskeyJsRuntime jsRuntime = new();
        _ = context.Services.AddSingleton<IJSRuntime>(jsRuntime);

        Guid credentialId = Guid.NewGuid();

        RecordingHttpMessageHandler handler = new();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<PasskeyCredentialDto>())
        });
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { })
        });
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new { })
        });
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<PasskeyCredentialDto>
            {
                new()
                {
                    Id = credentialId,
                    FriendlyName = "Laptop",
                    RegisteredAt = DateTimeOffset.UtcNow,
                }
            })
        });
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK));
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<PasskeyCredentialDto>())
        });

        HttpClient client = new(handler)
        {
            BaseAddress = new Uri("http://localhost")
        };

        context.Mocks.HttpClientFactory
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(client);

        IRenderedComponent<Passkeys> cut = context.RenderComponent<Passkeys>(
            parameters => parameters.AddCascadingValue(httpContext));

        cut.WaitForAssertion(() => Assert.Contains("RegisterPasskey", cut.Markup));
        cut.FindAll("button").First(button => button.TextContent.Contains("RegisterPasskey")).Click();

        cut.WaitForAssertion(() => Assert.Contains("Laptop", cut.Markup));
        cut.FindAll("button").First(button => button.GetAttribute("aria-label") == "Delete").Click();

        cut.WaitForAssertion(() => Assert.Contains("NoPasskeysRegistered", cut.Markup));

        Assert.Contains(handler.RequestUris, uri => uri.AbsolutePath.EndsWith("/api/webauthn/register/begin", StringComparison.Ordinal));
        Assert.Contains(handler.RequestUris, uri => uri.AbsolutePath.EndsWith("/api/webauthn/register/complete", StringComparison.Ordinal));
        Assert.Contains(handler.RequestUris, uri => uri.AbsolutePath.Contains($"/api/webauthn/credentials/{credentialId}", StringComparison.Ordinal));
    }

    [Fact]
    public void PhonePage_LoadsUser_ShowsChangeFormAndBackLink()
    {
        using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();
        User user = new() { Id = "user-1", PhoneNumber = "+1234567890" };
        _ = context.Mocks.Users
            .Setup(service => service.GetUserAsync(user.Id!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        IRenderedComponent<Phone> cut = context.RenderComponent<Phone>(
            parameters => parameters.AddCascadingValue(httpContext));

        cut.WaitForAssertion(() => Assert.Contains(user.PhoneNumber, cut.Markup));
        var cancelForm = cut.Find("form[action='/Account/Manage']");
        Assert.Contains("Cancel", cancelForm.TextContent);
    }

    [Fact]
    public void EmailPage_ShowsFailureMessageAndRetry()
    {
        using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();
        User user = new() { Id = "user-1", Email = "old@example.com" };
        _ = context.Mocks.Users
            .Setup(service => service.GetUserAsync(user.Id!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        context.Navigation.NavigateTo("http://localhost/Account/Manage/Email?FailureMessage=EmailChangeFailed");

        IRenderedComponent<Email> cut = context.RenderComponent<Email>(parameters => parameters
            .AddCascadingValue(httpContext));

        cut.WaitForAssertion(() => Assert.Contains("EmailChangeFailed", cut.Markup));
        Assert.Contains("TryAgain", cut.Markup);
        var cancelForm = cut.Find("form[action='/Account/Manage']");
        Assert.Contains("Cancel", cancelForm.TextContent);
    }

    [Fact]
    public void PasswordPage_HasNavigationBackToProfile()
    {
        using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();

        IRenderedComponent<Password> cut = context.RenderComponent<Password>(
            parameters => parameters.AddCascadingValue(httpContext));

        var backForm = cut.Find("form[action='/Account/Manage']");
        Assert.Contains("Profile", backForm.TextContent);
    }

    [Fact]
    public async Task PersonalAccessTokens_GeneratesTokenFromSelectedScopes()
    {
        using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();
        Mock<IRequestContext> requestContextMock = new();
        _ = requestContextMock.SetupGet(r => r.UserId).Returns("user-1");
        _ = context.Services.AddSingleton(requestContextMock.Object);

        _ = context.Mocks.ApiClients
            .Setup(service => service.GetApiClientsAsync())
            .ReturnsAsync(new List<ApiClient>
            {
                new() { Id = "client-1", Secret = "secret", HostUri = "http://localhost", Scopes = "scope.read" },
                new() { Id = "client-2", Secret = "secret", HostUri = "http://localhost", Scopes = "scope.write" },
            });

        _ = context.Mocks.PersonalAccessTokens
            .Setup(service => service.CreateTokenAsync("user-1", It.IsAny<int>(), It.IsAny<IEnumerable<string>>(), false))
            .ReturnsAsync("generated-token");

        IRenderedComponent<PersonalAccessTokens> cut = context.RenderComponent<PersonalAccessTokens>(
            parameters => parameters.AddCascadingValue(httpContext));

        cut.WaitForAssertion(() => Assert.Contains("GenerateToken", cut.Markup));
        ComponentTestHelper.SetField(cut.Instance, "selectedScopes", new HashSet<string>(new[] { "scope.read" }, StringComparer.Ordinal), typeof(PersonalAccessTokens));

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "GenerateTokenAsync", typeof(PersonalAccessTokens)));

        context.Mocks.PersonalAccessTokens.Verify(service => service.CreateTokenAsync("user-1", It.IsAny<int>(), It.Is<IEnumerable<string>>(scopes => scopes.Contains("scope.read")), false), Times.Once);
    }

    [Fact]
    public async Task EmailChangeForm_SendsCodeWhenEmailIsUnique()
    {
        using ComponentFixture fixture = new();
        EmailChangeForm form = new();
        fixture.ConfigureCommonComponent(form);

        AuthenticationStateProvider authProvider = new TestAuthenticatedStateProvider("user-1");
        ComponentTestHelper.SetProperty(form, "AuthenticationStateProvider", authProvider, typeof(Vitarish.Accounts.Interface.Components.Base.CommonBase));

        User user = new() { Id = "user-1" };
        _ = fixture.Mocks.Users.Setup(service => service.GetUserAsync(user.Id!, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _ = fixture.Mocks.Users.Setup(service => service.GetUsersAsync()).ReturnsAsync(new List<User> { user });

        await ComponentTestHelper.InvokeAsync(form, "ValidSendCodeSubmit", typeof(EmailChangeForm), new SendEmailCodeModel { Email = "new@example.com", User = user });

        fixture.Mocks.EmailAuthentication.Verify(service => service.SendCode(user, "new@example.com"), Times.Once);
    }

    [Fact]
    public async Task PhoneChangeForm_BlocksDuplicateNumbers()
    {
        using ComponentFixture fixture = new();
        PhoneChangeForm form = new();
        fixture.ConfigureCommonComponent(form);

        AuthenticationStateProvider authProvider = new TestAuthenticatedStateProvider("user-1");
        ComponentTestHelper.SetProperty(form, "AuthenticationStateProvider", authProvider, typeof(Vitarish.Accounts.Interface.Components.Base.CommonBase));

        User user = new() { Id = "user-1", PhoneNumber = "+123" };
        User other = new() { Id = "user-2", PhoneNumber = "+999" };

        _ = fixture.Mocks.Users.Setup(service => service.GetUserAsync(user.Id!, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _ = fixture.Mocks.Users.Setup(service => service.GetUsersAsync()).ReturnsAsync(new List<User> { user, other });

        await ComponentTestHelper.InvokeAsync(form, "ValidSendCodeSubmit", typeof(PhoneChangeForm), new SendSmsCodeModel { PhoneNumber = other.PhoneNumber!, User = user });

        fixture.Mocks.SmsAuthentication.Verify(service => service.SendCode(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        string? error = ComponentTestHelper.GetFieldValue<string>(form, "error", typeof(PhoneChangeForm));
        Assert.Equal("InvalidPhoneNumber", error);
    }

    private static DefaultHttpContext CreateHttpContext()
    {
        ClaimsPrincipal user = new(new ClaimsIdentity(new[]
        {
            new Claim("sub", "user-1"),
            new Claim(ClaimTypes.Name, "TestUser"),
        }, "Cookies"));

        return new DefaultHttpContext { User = user };
    }

    private sealed class TestAuthenticatedStateProvider : AuthenticationStateProvider
    {
        private readonly ClaimsPrincipal principal;

        public TestAuthenticatedStateProvider(string userId)
        {
            principal = new ClaimsPrincipal(new ClaimsIdentity(new[]
            {
                new Claim("sub", userId),
                new Claim(ClaimTypes.Name, "TestUser"),
            }, "Cookies"));
        }

        public override Task<AuthenticationState> GetAuthenticationStateAsync()
            => Task.FromResult(new AuthenticationState(principal));
    }

    private sealed class PasskeyJsRuntime : IJSRuntime, IJSObjectReference
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return identifier switch
            {
                "import" when typeof(TValue) == typeof(IJSObjectReference) => new ValueTask<TValue>((TValue)(object)this),
                "isWebAuthnSupported" => new ValueTask<TValue>((TValue)(object)true!),
                "createCredential" => new ValueTask<TValue>((TValue)(object)new { }!),
                _ => new ValueTask<TValue>(default(TValue)!)
            };
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new();
        public List<Uri> RequestUris { get; } = new();

        public void EnqueueResponse(HttpResponseMessage response) => responses.Enqueue(response);

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestUris.Add(request.RequestUri!);
            if (responses.Count == 0)
            {
                throw new InvalidOperationException("No response available for request");
            }

            return Task.FromResult(responses.Dequeue());
        }
    }
}
