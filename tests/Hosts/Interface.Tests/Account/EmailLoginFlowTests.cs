namespace Vitarish.Interface.Tests.Account;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vitarish.Interface.Domain.Models;
using Vitarish.Interface.Presentation.Components.Account;
using Vitarish.Interface.Presentation.Pages.Account;
using Vitarish.Interface.Tests.TestInfrastructure;
using Vitarish.Users.Domain.Entities;

public class EmailLoginFlowTests
{
    [Fact]
    public async Task EmailFlow_CompletesSuccessfulSignIn()
    {
        using var fixture = new ComponentFixture();
        var user = new User { Id = "user-email", Email = "alice@example.com" };

        var sendCodeCompletion = new TaskCompletionSource<bool>();
        fixture.Mocks.EmailAuthentication
            .Setup(s => s.GetOrRegisterUser("alice@example.com"))
            .ReturnsAsync(user);
        fixture.Mocks.EmailAuthentication
            .Setup(s => s.SendCode(user, "alice@example.com"))
            .Returns(Task.CompletedTask)
            .Callback(() => sendCodeCompletion.TrySetResult(true));

        var emailForm = new EmailLoginForm();
        ComponentTestHelper.SetProperty(emailForm, nameof(EmailLoginForm.ReturnUrl), "/callback", typeof(EmailLoginForm));
        ComponentTestHelper.SetProperty(emailForm, nameof(EmailLoginForm.FailureUrl), "/login/email", typeof(EmailLoginForm));
        fixture.ConfigureCommonComponent(emailForm);

        ComponentTestHelper.Invoke(
            emailForm,
            "ValidSendCodeSubmit",
            typeof(EmailLoginForm),
            new SendEmailCodeModel { Email = "alice@example.com" });
        await sendCodeCompletion.Task;

        fixture.Mocks.EmailAuthentication.Verify(s => s.GetOrRegisterUser("alice@example.com"), Times.Once);
        fixture.Mocks.EmailAuthentication.Verify(s => s.SendCode(user, "alice@example.com"), Times.Once);
        Assert.Same(user, ComponentTestHelper.GetFieldValue<User?>(emailForm, "User", typeof(EmailLoginForm)));
        Assert.Equal("alice@example.com", ComponentTestHelper.GetFieldValue<string?>(emailForm, "Email", typeof(EmailLoginForm)));

        fixture.Mocks.Users
            .Setup(s => s.GetUserAsync(user.Id!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        fixture.Mocks.EmailAuthentication
            .Setup(s => s.VerifyCode(user, "123456"))
            .ReturnsAsync(true);
        var emailStore = fixture.Mocks.UserStore.As<IUserEmailStore<User>>();
        emailStore
            .Setup(s => s.GetEmailConfirmedAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        emailStore
            .Setup(s => s.SetEmailConfirmedAsync(user, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var submitModel = new EmailLoginSubmitModel(
            fixture.Mocks.Users.Object,
            fixture.Mocks.EmailAuthentication.Object,
            fixture.Mocks.SignInManager,
            fixture.Mocks.UserStore.Object,
            fixture.Localizer,
            NullLogger<EmailLoginSubmitModel>.Instance);
        submitModel.PageContext = new PageContext
        {
            HttpContext = fixture.Mocks.HttpContext,
        };
        submitModel.Input = new VerifyCodeModel
        {
            UserId = user.Id!,
            Code = "123456",
            Provider = TokenOptions.DefaultEmailProvider,
            ReturnUrl = "/callback",
            FailureUrl = "/login/email",
        };

        var result = await submitModel.OnPostAsync();

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/Account/SuccessfulLogin?ReturnUrl=%2Fcallback", redirect.Url);

        emailStore.Verify(s => s.SetEmailConfirmedAsync(user, true, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Mocks.EmailAuthentication.Verify(s => s.VerifyCode(user, "123456"), Times.Once);
        fixture.Mocks.SmsAuthentication.Verify(s => s.VerifyCode(It.IsAny<User>(), It.IsAny<string>()), Times.Never);
        var signedInUser = Assert.Single(fixture.Mocks.SignInManager.InteractiveSignInUsers);
        Assert.Same(user, signedInUser);
    }
}
