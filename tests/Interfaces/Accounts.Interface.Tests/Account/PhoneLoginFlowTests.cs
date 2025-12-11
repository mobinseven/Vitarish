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

public class PhoneLoginFlowTests
{
    [Fact]
    public async Task PhoneFlow_CompletesSuccessfulSignIn()
    {
        using ComponentFixture fixture = new();
        User user = new()
        { Id = "user-phone" };

        TaskCompletionSource<bool> sendCodeCompletion = new();
        _ = fixture.Mocks.SmsAuthentication
            .Setup(s => s.GetOrRegisterUser("5551112222"))
            .ReturnsAsync(user);
        _ = fixture.Mocks.SmsAuthentication
            .Setup(s => s.SendCode(user, "5551112222"))
            .Returns(Task.CompletedTask)
            .Callback(() => sendCodeCompletion.TrySetResult(true));

        PhoneLoginForm phoneForm = new();
        ComponentTestHelper.SetProperty(phoneForm, nameof(PhoneLoginForm.ReturnUrl), "/callback", typeof(PhoneLoginForm));
        ComponentTestHelper.SetProperty(phoneForm, nameof(PhoneLoginForm.FailureUrl), "/login/phone", typeof(PhoneLoginForm));
        fixture.ConfigureCommonComponent(phoneForm);

        _ = ComponentTestHelper.Invoke(
            phoneForm,
            "ValidSendCodeSubmit",
            typeof(PhoneLoginForm),
            new SendSmsCodeModel { PhoneNumber = "5551112222" });
        _ = await sendCodeCompletion.Task;

        fixture.Mocks.SmsAuthentication.Verify(s => s.GetOrRegisterUser("5551112222"), Times.Once);
        fixture.Mocks.SmsAuthentication.Verify(s => s.SendCode(user, "5551112222"), Times.Once);
        Assert.Same(user, ComponentTestHelper.GetFieldValue<User?>(phoneForm, "User", typeof(PhoneLoginForm)));
        Assert.Equal("5551112222", ComponentTestHelper.GetFieldValue<string?>(phoneForm, "PhoneNumber", typeof(PhoneLoginForm)));

        _ = fixture.Mocks.Users
            .Setup(s => s.GetUserAsync(user.Id!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _ = fixture.Mocks.SmsAuthentication
            .Setup(s => s.VerifyCode(user, "123456"))
            .ReturnsAsync(true);
        Mock<IUserPhoneNumberStore<User>> phoneStore = fixture.Mocks.UserStore.As<IUserPhoneNumberStore<User>>();
        _ = phoneStore
            .Setup(s => s.GetPhoneNumberConfirmedAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ = phoneStore
            .Setup(s => s.SetPhoneNumberConfirmedAsync(user, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        PhoneLoginSubmitModel submitModel = new(
            fixture.Mocks.Users.Object,
            fixture.Mocks.SmsAuthentication.Object,
            fixture.Mocks.SignInManager,
            fixture.Mocks.UserStore.Object,
            fixture.Localizer,
            NullLogger<PhoneLoginSubmitModel>.Instance)
        {
            PageContext = new PageContext
            {
                HttpContext = fixture.Mocks.HttpContext,
            },
            Input = new VerifyCodeModel
            {
                UserId = user.Id!,
                Code = "123456",
                Provider = TokenOptions.DefaultPhoneProvider,
                ReturnUrl = "/callback",
                FailureUrl = "/login/phone",
            }
        };

        IActionResult result = await submitModel.OnPostAsync();

        RedirectResult redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/Account/SuccessfulLogin?ReturnUrl=%2Fcallback", redirect.Url);

        phoneStore.Verify(s => s.SetPhoneNumberConfirmedAsync(user, true, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Mocks.SmsAuthentication.Verify(s => s.VerifyCode(user, "123456"), Times.Once);
        User signedInUser = Assert.Single(fixture.Mocks.SignInManager.InteractiveSignInUsers);
        Assert.Same(user, signedInUser);
    }
}
