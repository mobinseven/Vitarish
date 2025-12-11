using Accounts.Interface.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Interface.Components.Account;
using Vitarish.Accounts.Interface.Pages.Account;
using Vitarish.Users.Domain.Entities;

namespace Accounts.Interface.Tests.Account;

public class EmailLoginFlowTests
{
    [Fact]
    public async Task EmailFlow_CompletesSuccessfulSignIn()
    {
        using ComponentFixture fixture = new();
        User user = new()
        { Id = "user-email", Email = "alice@example.com" };

        TaskCompletionSource<bool> sendCodeCompletion = new();
        _ = fixture.Mocks.EmailAuthentication
            .Setup(s => s.GetOrRegisterUser("alice@example.com"))
            .ReturnsAsync(user);
        _ = fixture.Mocks.EmailAuthentication
            .Setup(s => s.SendCode(user, "alice@example.com"))
            .Returns(Task.CompletedTask)
            .Callback(() => sendCodeCompletion.TrySetResult(true));

        EmailLoginForm emailForm = new();
        ComponentTestHelper.SetProperty(emailForm, nameof(EmailLoginForm.ReturnUrl), "/callback", typeof(EmailLoginForm));
        ComponentTestHelper.SetProperty(emailForm, nameof(EmailLoginForm.FailureUrl), "/login/email", typeof(EmailLoginForm));
        fixture.ConfigureCommonComponent(emailForm);

        _ = ComponentTestHelper.Invoke(
            emailForm,
            "ValidSendCodeSubmit",
            typeof(EmailLoginForm),
            new SendEmailCodeModel { Email = "alice@example.com" });
        _ = await sendCodeCompletion.Task;

        fixture.Mocks.EmailAuthentication.Verify(s => s.GetOrRegisterUser("alice@example.com"), Times.Once);
        fixture.Mocks.EmailAuthentication.Verify(s => s.SendCode(user, "alice@example.com"), Times.Once);
        Assert.Same(user, ComponentTestHelper.GetFieldValue<User?>(emailForm, "User", typeof(EmailLoginForm)));
        Assert.Equal("alice@example.com", ComponentTestHelper.GetFieldValue<string?>(emailForm, "Email", typeof(EmailLoginForm)));

        _ = fixture.Mocks.Users
            .Setup(s => s.GetUserAsync(user.Id!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _ = fixture.Mocks.EmailAuthentication
            .Setup(s => s.VerifyCode(user, "123456"))
            .ReturnsAsync(true);
        var emailStore = fixture.Mocks.UserStore.As<IUserEmailStore<User>>();
        _ = emailStore
            .Setup(s => s.GetEmailConfirmedAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ = emailStore
            .Setup(s => s.SetEmailConfirmedAsync(user, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        EmailLoginSubmitModel submitModel = new(
            fixture.Mocks.Users.Object,
            fixture.Mocks.EmailAuthentication.Object,
            fixture.Mocks.SignInManager,
            fixture.Mocks.UserStore.Object,
            fixture.Localizer,
            NullLogger<EmailLoginSubmitModel>.Instance)
        {
            PageContext = new PageContext
            {
                HttpContext = fixture.Mocks.HttpContext,
            },
            Input = new VerifyCodeModel
            {
                UserId = user.Id!,
                Code = "123456",
                Provider = TokenOptions.DefaultEmailProvider,
                ReturnUrl = "/callback",
                FailureUrl = "/login/email",
            }
        };

        IActionResult result = await submitModel.OnPostAsync();

        RedirectResult redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/Account/SuccessfulLogin?ReturnUrl=%2Fcallback", redirect.Url);

        emailStore.Verify(s => s.SetEmailConfirmedAsync(user, true, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Mocks.EmailAuthentication.Verify(s => s.VerifyCode(user, "123456"), Times.Once);
        fixture.Mocks.SmsAuthentication.Verify(s => s.VerifyCode(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        var signedInUser = Assert.Single(fixture.Mocks.SignInManager.InteractiveSignInUsers);
        Assert.Same(user, signedInUser);
    }
}
