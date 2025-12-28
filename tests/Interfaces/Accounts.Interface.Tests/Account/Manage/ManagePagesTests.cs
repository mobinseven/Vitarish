using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using Bunit;
using AngleSharp.Dom;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.JSInterop;
using MudBlazor;
using Moq;
using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Interface.Components.Account;
using Vitarish.Main.Pages.Account.Manage;
using Vitarish.ApiClients.Domain.Entities;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Domain.Entities;
using Accounts.Interface.Tests.TestInfrastructure;

namespace Accounts.Interface.Tests.Account.Manage;

public class ManagePagesTests
{
    [Fact]
    public async Task Profile_ShowsHeaderAndEditor()
    {
        await using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();

        IRenderedComponent<Profile> cut = context.Render<Profile>(
            parameters => parameters.AddCascadingValue(httpContext));

        Assert.Contains("Profile", cut.Markup);
        Assert.NotNull(cut.Find("h4"));
        Assert.NotNull(cut.FindComponent<Vitarish.Accounts.Interface.Components.Account.ProfileEditor>());
    }

    [Fact]
    public async Task Passkeys_LoadsCredentialsAndProvidesBackNavigation()
    {
        await using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();
        PasskeyJsRuntime jsRuntime = new();
        _ = context.Services.AddSingleton<IJSRuntime>(jsRuntime);

        RecordingHttpMessageHandler handler = new();
        handler.EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = JsonContent.Create(new List<PasskeyCredentialDto>())
        });

        context.Mocks.HttpClientFactory
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://localhost")
            });

        IRenderedComponent<Passkeys> cut = context.Render<Passkeys>(
            parameters => parameters.AddCascadingValue(httpContext));

        cut.WaitForState(() => cut.Markup.Contains("RegisterPasskey", StringComparison.Ordinal), timeout: TimeSpan.FromSeconds(2));

        var backForm = cut.Find("form[action='/Account/Manage']");
        Assert.Contains("Profile", backForm.TextContent);
    }

    [Fact]
    public async Task Passkeys_RegisterAndDelete_InvokeExpectedEndpoints()
    {
        await using InterfaceTestContext context = new();
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

        context.Mocks.HttpClientFactory
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://localhost")
            });

        IRenderedComponent<Passkeys> cut = context.Render<Passkeys>(
            parameters => parameters.AddCascadingValue(httpContext));

        ComponentTestHelper.SetField(cut.Instance, "_jsModule", jsRuntime, typeof(Passkeys));

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "RegisterPasskeyAsync", typeof(Passkeys)));

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "DeletePasskeyAsync", typeof(Passkeys), credentialId));

        Assert.Contains(handler.RequestUris, uri => uri.AbsolutePath.Contains("/api/webauthn/credentials", StringComparison.Ordinal));
    }

    [Fact]
    public async Task PhonePage_LoadsUser_ShowsChangeFormAndBackLink()
    {
        await using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();
        User user = new() { Id = "user-1", PhoneNumber = "+1234567890" };
        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        IRenderedComponent<Phone> cut = context.Render<Phone>(
            parameters => parameters.AddCascadingValue(httpContext));

        cut.WaitForAssertion(() => Assert.Contains(user.PhoneNumber, cut.Markup));
        var cancelForm = cut.Find("form[action='/Account/Manage']");
        Assert.Contains("Cancel", cancelForm.TextContent);
    }

    [Fact]
    public async Task EmailPage_ShowsFailureMessageAndRetry()
    {
        await using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();
        User user = new() { Id = "user-1", Email = "old@example.com" };
        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        context.Navigation.NavigateTo("http://localhost/Account/Manage/Email?FailureMessage=EmailChangeFailed");

        IRenderedComponent<Email> cut = context.Render<Email>(parameters => parameters
            .AddCascadingValue(httpContext));

        cut.WaitForAssertion(() => Assert.Contains("EmailChangeFailed", cut.Markup));
        Assert.Contains("TryAgain", cut.Markup);
        var cancelForm = cut.Find("form[action='/Account/Manage']");
        Assert.Contains("Cancel", cancelForm.TextContent);
    }

    [Fact]
    public async Task PasswordPage_HasNavigationBackToProfile()
    {
        await using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = CreateHttpContext();

        IRenderedComponent<Password> cut = context.Render<Password>(
            parameters => parameters.AddCascadingValue(httpContext));

        var backForm = cut.Find("form[action='/Account/Manage']");
        Assert.Contains("Profile", backForm.TextContent);
    }

    [Fact]
    public async Task PersonalAccessTokens_GeneratesTokenFromSelectedScopes()
    {
        await using InterfaceTestContext context = new();
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
            .Setup(service => service.CreateTokenAsync("user-1", It.IsAny<int>(), It.IsAny<IEnumerable<string>>(), false, It.IsAny<CancellationToken>()))
            .ReturnsAsync("generated-token");

        context.Render<MudPopoverProvider>();
        IRenderedComponent<PersonalAccessTokens> cut = context.Render<PersonalAccessTokens>(
            parameters => parameters.AddCascadingValue(httpContext));

        MudSelect<string> select = cut.FindComponent<MudSelect<string>>().Instance;
        await cut.InvokeAsync(select.OpenMenu);
        await cut.InvokeAsync(() => select.SelectOption("scope.read"));
        cut.FindAll("button").First(b => b.TextContent.Contains("GenerateToken", StringComparison.Ordinal)).Click();

        context.Mocks.PersonalAccessTokens.Verify(service => service.CreateTokenAsync("user-1", It.IsAny<int>(), It.Is<IEnumerable<string>>(scopes => scopes.Contains("scope.read")), false, It.IsAny<CancellationToken>()), Times.Once);
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
        _ = fixture.Mocks.UserService.Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _ = fixture.Mocks.UserService.Setup(service => service.CheckUserEmailInUseAsync("new@example.com", It.IsAny<CancellationToken>())).ReturnsAsync(false);

        await ComponentTestHelper.InvokeAsync(form, "ValidSendCodeSubmit", typeof(EmailChangeForm), new SendEmailCodeModel { Email = "new@example.com", User = user });

        fixture.Mocks.UserService.Verify(service => service.SendEmailCode("new@example.com"), Times.Once);
    }

    [Fact]
    public async Task EmailChangeForm_ShowsErrorWhenUserIsMissing()
    {
        using ComponentFixture fixture = new();
        EmailChangeForm form = new();
        fixture.ConfigureCommonComponent(form);

        AuthenticationStateProvider authProvider = new TestAuthenticatedStateProvider("user-1");
        ComponentTestHelper.SetProperty(form, "AuthenticationStateProvider", authProvider, typeof(Vitarish.Accounts.Interface.Components.Base.CommonBase));

        _ = fixture.Mocks.UserService.Setup(service => service.GetUserAsync(null, It.IsAny<CancellationToken>())).ReturnsAsync((User?)null);

        await ComponentTestHelper.InvokeAsync(form, "ValidSendCodeSubmit", typeof(EmailChangeForm), new SendEmailCodeModel
        {
            Email = "new@example.com",
            User = new User { Id = "user-1" }
        });

        string? error = ComponentTestHelper.GetFieldValue<string?>(form, "error", typeof(EmailChangeForm));
        Assert.Equal(fixture.Localizer["UserNotFound"].Value, error);
        fixture.Mocks.UserService.Verify(service => service.CheckUserEmailInUseAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        fixture.Mocks.UserService.Verify(service => service.SendEmailCode(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task PhoneChangeForm_BlocksDuplicateNumbers()
    {
        await using InterfaceTestContext context = new();
        _ = context.Services.AddSingleton<AuthenticationStateProvider>(new TestAuthenticatedStateProvider("user-1"));

        User user = new() { Id = "user-1" };
        User other = new() { Id = "user-2", PhoneNumber = "09123456789" };

        _ = context.Mocks.UserService.Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>())).ReturnsAsync(user);
        _ = context.Mocks.UserService.Setup(service => service.CheckUserPhoneNumberInUseAsync(other.PhoneNumber!, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        IRenderedComponent<PhoneChangeForm> cut = context.Render<PhoneChangeForm>();

        IElement phoneInput = cut.Find("input");
        phoneInput.Input(other.PhoneNumber!);

        IElement continueButton = null!;
        cut.WaitForAssertion(() =>
        {
            continueButton = cut.FindAll("button").First(b => b.TextContent.Contains("Continue", StringComparison.Ordinal));
            Assert.Null(continueButton.GetAttribute("disabled"));
        });

        continueButton.Click();

        cut.WaitForState(() => cut.Markup.Contains("InvalidPhoneNumber", StringComparison.Ordinal), timeout: TimeSpan.FromSeconds(5));
        context.Mocks.UserService.Verify(service => service.SendSmsCode(It.IsAny<string>()), Times.Never);
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
                _ => throw new InvalidOperationException($"JS interop call '{identifier}' is not mocked.")
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
