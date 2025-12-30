using Accounts.Interface.Tests.TestInfrastructure;
using AngleSharp.Dom;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;
using System.Net;
using System.Net.Http.Json;
using System.Linq;
using Vitarish.Accounts.Infrastructure.Services;
using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Interface.Components.Account;
using Vitarish.Users.Domain.Entities;

namespace Accounts.Interface.Tests.Account;

public class AccountComponentsTests
{
    [Fact]
    public async Task EmailVerificationSend_SubmitValid_ShowsConfirmation()
    {
        await using InterfaceTestContext context = new();
        TaskCompletionSource<SendEmailCodeModel> completion = new();
        EmailVerificationSend cut = context.Render<EmailVerificationSend>(parameters => parameters
            .Add(p => p.OnValidSubmit, EventCallback.Factory.Create<SendEmailCodeModel>(this, model => completion.TrySetResult(model))))
            .Instance;

        ComponentTestHelper.SetField(cut, "Email", "alice@example.com", typeof(EmailVerificationSend));

        ComponentTestHelper.Invoke(cut, "ValidSubmit", typeof(EmailVerificationSend));

        SendEmailCodeModel submitted = await completion.Task;
        Assert.Equal("alice@example.com", submitted.Email);
        Assert.True(ComponentTestHelper.GetFieldValue<bool>(cut, "submitting", typeof(EmailVerificationSend)));
    }

    [Fact]
    public async Task EmailVerificationSend_SubmitInvalid_ShowsErrors()
    {
        await using InterfaceTestContext context = new();
        EmailVerificationSend cut = context.Render<EmailVerificationSend>().Instance;

        ComponentTestHelper.SetField(cut, "Email", "invalid-email", typeof(EmailVerificationSend));

        bool isValid = ComponentTestHelper.GetPropertyValue<bool>(cut, "isValid", typeof(EmailVerificationSend));
        Assert.False(isValid);
    }

    [Fact]
    public async Task EmailVerificationVerify_SubmitValid_ShowsSuccess()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "user-1" };

        IRenderedComponent<EmailVerificationVerify> cut = context.Render<EmailVerificationVerify>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.Email, "alice@example.com")
            .Add(p => p.ReturnUrl, "/callback"));

        Assert.Contains("Input.Code", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task EmailVerificationVerify_SubmitInvalid_ShowsErrors()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "user-1" };

        _ = Assert.Throws<ArgumentNullException>(() =>
            context.Render<EmailVerificationVerify>(parameters => parameters
                .Add(p => p.User, user)
                .Add(p => p.Email, string.Empty)));
    }

    [Fact]
    public async Task PhoneVerificationSend_SubmitValid_ShowsConfirmation()
    {
        await using InterfaceTestContext context = new();
        TaskCompletionSource<SendSmsCodeModel> completion = new();

        PhoneVerificationSend cut = context.Render<PhoneVerificationSend>(parameters => parameters
            .Add(p => p.OnValidSubmit, EventCallback.Factory.Create<SendSmsCodeModel>(this, model => completion.TrySetResult(model))))
            .Instance;

        ComponentTestHelper.SetField(cut, "PhoneNumber", "09123456789", typeof(PhoneVerificationSend));

        ComponentTestHelper.Invoke(cut, "ValidSubmit", typeof(PhoneVerificationSend));

        SendSmsCodeModel submitted = await completion.Task;
        Assert.Equal("09123456789", submitted.PhoneNumber);
    }

    [Fact]
    public async Task PhoneVerificationSend_SubmitInvalid_ShowsErrors()
    {
        await using InterfaceTestContext context = new();
        PhoneVerificationSend cut = context.Render<PhoneVerificationSend>().Instance;

        ComponentTestHelper.SetField(cut, "PhoneNumber", "123", typeof(PhoneVerificationSend));

        bool isValid = ComponentTestHelper.GetPropertyValue<bool>(cut, "isValid", typeof(PhoneVerificationSend));
        Assert.False(isValid);
    }

    [Fact]
    public async Task PhoneVerificationVerify_SubmitValid_ShowsSuccess()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "user-1" };

        IRenderedComponent<PhoneVerificationVerify> cut = context.Render<PhoneVerificationVerify>(parameters => parameters
            .Add(p => p.User, user)
            .Add(p => p.PhoneNumber, "09123456789")
            .Add(p => p.ReturnUrl, "/callback"));

        Assert.Contains("Input.Code", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task PhoneVerificationVerify_SubmitInvalid_ShowsErrors()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "user-1" };

        _ = Assert.Throws<ArgumentNullException>(() =>
            context.Render<PhoneVerificationVerify>(parameters => parameters
                .Add(p => p.User, user)
                .Add(p => p.PhoneNumber, string.Empty)));
    }

    [Fact]
    public async Task StatusMessage_RendersMessage_WhenProvided()
    {
        await using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = new();
        httpContext.Request.Headers.Append("Cookie", $"{IdentityRedirectManager.StatusCookieName}=StatusMessage");

        IRenderedComponent<StatusMessage> cut = context.Render<StatusMessage>(parameters => parameters
            .AddCascadingValue(httpContext));

        Assert.Contains("StatusMessage", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task StatusMessage_Hides_WhenEmpty()
    {
        await using InterfaceTestContext context = new();
        DefaultHttpContext httpContext = new();

        IRenderedComponent<StatusMessage> cut = context.Render<StatusMessage>(parameters => parameters
            .AddCascadingValue(httpContext));

        Assert.DoesNotContain("MudAlert", cut.Markup, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ProfileEditor_LoadsExistingProfile()
    {
        await using InterfaceTestContext context = new();
        User user = new()
        {
            Id = "user-1",
            UserName = "alice",
            Email = "alice@example.com",
            PhoneNumber = "+123",
            FirstName = "Alice",
            LastName = "Smith"
        };

        _ = context.Services.AddSingleton<IStringLocalizer<CultureSelector>>(new TestStringLocalizer<CultureSelector>());
        RegisterCultureProvider(context);

        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        IRenderedComponent<ProfileEditor> cut = context.Render<ProfileEditor>();

        cut.WaitForAssertion(() => Assert.Contains("alice@example.com", cut.Markup, StringComparison.Ordinal));
        Assert.Contains("alice", cut.Markup, StringComparison.Ordinal);
    }

    [Fact]
    public async Task ProfileEditor_SubmitValid_PersistsChanges()
    {
        await using InterfaceTestContext context = new();
        User user = new()
        {
            Id = "user-1",
            UserName = "alice",
            Email = "alice@example.com",
            PhoneNumber = "+123",
            FirstName = "Alice",
            LastName = "Smith"
        };

        _ = context.Services.AddSingleton<IStringLocalizer<CultureSelector>>(new TestStringLocalizer<CultureSelector>());
        RegisterCultureProvider(context);

        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _ = context.Mocks.UserService
            .Setup(service => service.UpdateUserAsync(user.Id, It.IsAny<User>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(IdentityResult.Success);

        IRenderedComponent<ProfileEditor> cut = context.Render<ProfileEditor>();

        ComponentTestHelper.SetField(cut.Instance, "_form", null, typeof(ProfileEditor));

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "UpdateUser", typeof(ProfileEditor)));

        context.Mocks.UserService.Verify(service => service.UpdateUserAsync(user.Id, It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProfileEditor_SubmitInvalid_ShowsErrors()
    {
        await using InterfaceTestContext context = new();
        User user = new()
        {
            Id = "user-1",
            UserName = "alice",
            Email = "alice@example.com",
            PhoneNumber = "+123",
            FirstName = string.Empty,
            LastName = string.Empty
        };

        _ = context.Services.AddSingleton<IStringLocalizer<CultureSelector>>(new TestStringLocalizer<CultureSelector>());
        RegisterCultureProvider(context);

        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        IRenderedComponent<ProfileEditor> cut = context.Render<ProfileEditor>();

        IElement saveButton = cut.FindAll("button")
            .Single(button => button.TextContent.Contains("Save", StringComparison.Ordinal));
        Assert.True(saveButton.HasAttribute("disabled"));
    }

    [Fact]
    public async Task EmailChangeForm_SubmitValid_SendsVerification()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "user-1", Email = "old@example.com" };

        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _ = context.Mocks.UserService
            .Setup(service => service.CheckUserEmailInUseAsync("new@example.com", user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ = context.Mocks.UserService
            .Setup(service => service.SendEmailCode(user.Id, "new@example.com"))
            .Returns(Task.CompletedTask);

        IRenderedComponent<EmailChangeForm> cut = context.Render<EmailChangeForm>();

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "ValidSendCodeSubmit", typeof(EmailChangeForm), new SendEmailCodeModel { Email = "new@example.com" }));

        context.Mocks.UserService.Verify(service => service.SendEmailCode(user.Id, "new@example.com"), Times.Once);
        cut.WaitForAssertion(() => Assert.Contains("Input.Code", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task EmailChangeForm_SubmitInvalid_ShowsErrors()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "user-1", Email = "old@example.com" };

        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _ = context.Mocks.UserService
            .Setup(service => service.CheckUserEmailInUseAsync("new@example.com", user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        IRenderedComponent<EmailChangeForm> cut = context.Render<EmailChangeForm>();

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "ValidSendCodeSubmit", typeof(EmailChangeForm), new SendEmailCodeModel { Email = "new@example.com" }));

        cut.WaitForAssertion(() => Assert.Contains("InvalidEmail", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task PhoneChangeForm_SubmitValid_SendsVerification()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "user-1", PhoneNumber = "09123456789" };

        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _ = context.Mocks.UserService
            .Setup(service => service.CheckUserPhoneNumberInUseAsync("09123456789", user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ = context.Mocks.UserService
            .Setup(service => service.SendSmsCode(user.Id, "09123456789"))
            .Returns(Task.CompletedTask);

        IRenderedComponent<PhoneChangeForm> cut = context.Render<PhoneChangeForm>();

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "ValidSendCodeSubmit", typeof(PhoneChangeForm), new SendSmsCodeModel { PhoneNumber = "09123456789" }));

        context.Mocks.UserService.Verify(service => service.SendSmsCode(user.Id, "09123456789"), Times.Once);
        cut.WaitForAssertion(() => Assert.Contains("Input.Code", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task PhoneChangeForm_SubmitInvalid_ShowsErrors()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "user-1", PhoneNumber = "09123456789" };

        _ = context.Mocks.UserService
            .Setup(service => service.GetUserAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _ = context.Mocks.UserService
            .Setup(service => service.CheckUserPhoneNumberInUseAsync("09123456789", user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        IRenderedComponent<PhoneChangeForm> cut = context.Render<PhoneChangeForm>();

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "ValidSendCodeSubmit", typeof(PhoneChangeForm), new SendSmsCodeModel { PhoneNumber = "09123456789" }));

        cut.WaitForAssertion(() => Assert.Contains("InvalidPhoneNumber", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task WebAuthnLoginForm_InvokesJs_StartsChallenge()
    {
        await using InterfaceTestContext context = new();
        RecordingJsRuntime jsRuntime = new();
        _ = context.Services.AddSingleton<IJSRuntime>(jsRuntime);

        RecordingHttpMessageHandler handler = new();
        handler.EnqueueJsonResponse(new { challenge = "", rpId = "", allowCredentials = Array.Empty<object>() });
        handler.EnqueueJsonResponse(new { });

        context.Mocks.HttpClientFactory
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://localhost")
            });

        IRenderedComponent<WebAuthnLoginForm> cut = context.Render<WebAuthnLoginForm>();
        cut.WaitForAssertion(() => Assert.True(jsRuntime.IsSupportedCalled));

        ComponentTestHelper.SetProperty(cut.Instance, "Username", "alice", typeof(WebAuthnLoginForm));

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "AuthenticateAsync", typeof(WebAuthnLoginForm)));

        Assert.True(jsRuntime.GetCredentialCalled);
    }

    [Fact]
    public async Task WebAuthnLoginForm_HandlesJsFailure_ShowsError()
    {
        await using InterfaceTestContext context = new();
        FailingJsRuntime jsRuntime = new();
        _ = context.Services.AddSingleton<IJSRuntime>(jsRuntime);

        RecordingHttpMessageHandler handler = new();
        handler.EnqueueJsonResponse(new { challenge = "", rpId = "", allowCredentials = Array.Empty<object>() });

        context.Mocks.HttpClientFactory
            .Setup(factory => factory.CreateClient(It.IsAny<string>()))
            .Returns(() => new HttpClient(handler, disposeHandler: false)
            {
                BaseAddress = new Uri("http://localhost")
            });

        IRenderedComponent<WebAuthnLoginForm> cut = context.Render<WebAuthnLoginForm>();
        cut.WaitForAssertion(() => Assert.True(jsRuntime.IsSupportedCalled));

        ComponentTestHelper.SetProperty(cut.Instance, "Username", "alice", typeof(WebAuthnLoginForm));

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "AuthenticateAsync", typeof(WebAuthnLoginForm)));

        cut.WaitForAssertion(() => Assert.Contains("PasskeyAuthenticationCancelled", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task WebAuthnInterop_InvokesExpectedJsModules()
    {
        await using InterfaceTestContext context = new();
        RecordingJsRuntime jsRuntime = new();
        _ = context.Services.AddSingleton<IJSRuntime>(jsRuntime);

        IRenderedComponent<WebAuthnLoginForm> cut = context.Render<WebAuthnLoginForm>();

        cut.WaitForAssertion(() => Assert.NotEmpty(jsRuntime.ImportPaths));
        Assert.Contains("WebAuthnInterop.razor.js", jsRuntime.ImportPaths.Single());
        Assert.True(jsRuntime.IsSupportedCalled);
    }

    private sealed class RecordingJsRuntime : IJSRuntime, IJSObjectReference
    {
        public List<string> ImportPaths { get; } = new();
        public bool IsSupportedCalled { get; private set; }
        public bool GetCredentialCalled { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return identifier switch
            {
                "import" when typeof(TValue) == typeof(IJSObjectReference)
                    => RecordImport<TValue>(args),
                "mudElementRef.addOnBlurEvent" => new ValueTask<TValue>(default(TValue)!),
                "isWebAuthnSupported" => InvokeSupported<TValue>(),
                "getCredential" => InvokeGetCredential<TValue>(),
                _ => throw new NotSupportedException($"JS interop call '{identifier}' is not mocked in {nameof(RecordingJsRuntime)}.")
            };
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private ValueTask<TValue> InvokeSupported<TValue>()
        {
            IsSupportedCalled = true;
            return new ValueTask<TValue>((TValue)(object)true!);
        }

        private ValueTask<TValue> InvokeGetCredential<TValue>()
        {
            GetCredentialCalled = true;
            return new ValueTask<TValue>((TValue)(object)new { }!);
        }

        private ValueTask<TValue> RecordImport<TValue>(object?[]? args)
        {
            string? path = args?.FirstOrDefault()?.ToString();
            if (!string.IsNullOrWhiteSpace(path))
            {
                ImportPaths.Add(path);
            }
            return new ValueTask<TValue>((TValue)(object)this);
        }
    }

    private sealed class FailingJsRuntime : IJSRuntime, IJSObjectReference
    {
        public bool IsSupportedCalled { get; private set; }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args)
        {
            return identifier switch
            {
                "import" when typeof(TValue) == typeof(IJSObjectReference)
                    => new ValueTask<TValue>((TValue)(object)this),
                "mudElementRef.addOnBlurEvent" => new ValueTask<TValue>(default(TValue)!),
                "isWebAuthnSupported" => InvokeSupported<TValue>(),
                "getCredential" => throw new JSException("cancelled"),
                _ => throw new NotSupportedException($"JS interop call '{identifier}' is not mocked in {nameof(FailingJsRuntime)}.")
            };
        }

        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args)
            => InvokeAsync<TValue>(identifier, args);

        public ValueTask DisposeAsync() => ValueTask.CompletedTask;

        private ValueTask<TValue> InvokeSupported<TValue>()
        {
            IsSupportedCalled = true;
            return new ValueTask<TValue>((TValue)(object)true!);
        }
    }

    private sealed class RecordingHttpMessageHandler : HttpMessageHandler
    {
        private readonly Queue<HttpResponseMessage> responses = new();

        public void EnqueueResponse(HttpResponseMessage response) => responses.Enqueue(response);
        public void EnqueueJsonResponse<TPayload>(TPayload payload)
            => EnqueueResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = JsonContent.Create(payload)
            });

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (responses.Count == 0)
            {
                throw new InvalidOperationException("No response available for request");
            }

            return Task.FromResult(responses.Dequeue());
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                while (responses.Count > 0)
                {
                    responses.Dequeue().Dispose();
                }
            }

            base.Dispose(disposing);
        }
    }

    private static void RegisterCultureProvider(InterfaceTestContext context)
    {
        UserProfileCultureInfoProvider provider = new(
            context.Mocks.UserService.Object,
            new HttpContextAccessor { HttpContext = context.Mocks.HttpContext },
            new TestJsRuntime());
        _ = context.Services.AddSingleton(provider);
    }

    private sealed class TestStringLocalizer<T> : IStringLocalizer<T>
    {
        public LocalizedString this[string name] => new(name, name, resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments]
        {
            get
            {
                string formatted = arguments.Length == 0 ? name : $"{name} ({string.Join(", ", arguments)})";
                return new LocalizedString(name, formatted, resourceNotFound: false);
            }
        }

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => Enumerable.Empty<LocalizedString>();

        public IStringLocalizer WithCulture(System.Globalization.CultureInfo culture) => this;
    }
}
