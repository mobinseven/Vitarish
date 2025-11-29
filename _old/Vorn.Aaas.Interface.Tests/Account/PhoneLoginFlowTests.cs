namespace Vorn.Aaas.Interface.Tests.Account;

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Vorn.Aaas.Interface.Domain.Models;
using Vorn.Aaas.Interface.Presentation.Components.Account;
using Vorn.Aaas.Interface.Presentation.Pages.Account;
using Vorn.Aaas.Interface.Tests.TestInfrastructure;
using Vorn.Aaas.Users.Domain.Entities;

public class PhoneLoginFlowTests
{
    [Fact]
    public async Task PhoneFlow_CompletesSuccessfulSignIn()
    {
        using var fixture = new ComponentFixture();
        var user = new User { Id = "user-phone" };

        var sendCodeCompletion = new TaskCompletionSource<bool>();
        fixture.Mocks.SmsAuthentication
            .Setup(s => s.GetOrRegisterUser("5551112222"))
            .ReturnsAsync(user);
        fixture.Mocks.SmsAuthentication
            .Setup(s => s.SendCode(user, "5551112222"))
            .Returns(Task.CompletedTask)
            .Callback(() => sendCodeCompletion.TrySetResult(true));

        var phoneForm = new PhoneLoginForm();
        ComponentTestHelper.SetProperty(phoneForm, nameof(PhoneLoginForm.ReturnUrl), "/callback", typeof(PhoneLoginForm));
        ComponentTestHelper.SetProperty(phoneForm, nameof(PhoneLoginForm.FailureUrl), "/login/phone", typeof(PhoneLoginForm));
        fixture.ConfigureCommonComponent(phoneForm);

        ComponentTestHelper.Invoke(
            phoneForm,
            "ValidSendCodeSubmit",
            typeof(PhoneLoginForm),
            new SendSmsCodeModel { PhoneNumber = "5551112222" });
        await sendCodeCompletion.Task;

        fixture.Mocks.SmsAuthentication.Verify(s => s.GetOrRegisterUser("5551112222"), Times.Once);
        fixture.Mocks.SmsAuthentication.Verify(s => s.SendCode(user, "5551112222"), Times.Once);
        Assert.Same(user, ComponentTestHelper.GetFieldValue<User?>(phoneForm, "User", typeof(PhoneLoginForm)));
        Assert.Equal("5551112222", ComponentTestHelper.GetFieldValue<string?>(phoneForm, "PhoneNumber", typeof(PhoneLoginForm)));

        fixture.Mocks.Users
            .Setup(s => s.GetUserAsync(user.Id!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        fixture.Mocks.SmsAuthentication
            .Setup(s => s.VerifyCode(user, "123456"))
            .ReturnsAsync(true);
        var phoneStore = fixture.Mocks.UserStore.As<IUserPhoneNumberStore<User>>();
        phoneStore
            .Setup(s => s.GetPhoneNumberConfirmedAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        phoneStore
            .Setup(s => s.SetPhoneNumberConfirmedAsync(user, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var submitModel = new PhoneLoginSubmitModel(
            fixture.Mocks.Users.Object,
            fixture.Mocks.SmsAuthentication.Object,
            fixture.Mocks.SignInManager,
            fixture.Mocks.UserStore.Object,
            fixture.Localizer,
            NullLogger<PhoneLoginSubmitModel>.Instance);
        submitModel.PageContext = new PageContext
        {
            HttpContext = fixture.Mocks.HttpContext,
        };
        submitModel.Input = new VerifyCodeModel
        {
            UserId = user.Id!,
            Code = "123456",
            Provider = TokenOptions.DefaultPhoneProvider,
            ReturnUrl = "/callback",
            FailureUrl = "/login/phone",
        };

        var result = await submitModel.OnPostAsync();

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/Account/SuccessfulLogin?ReturnUrl=%2Fcallback", redirect.Url);

        phoneStore.Verify(s => s.SetPhoneNumberConfirmedAsync(user, true, It.IsAny<CancellationToken>()), Times.Once);
        fixture.Mocks.SmsAuthentication.Verify(s => s.VerifyCode(user, "123456"), Times.Once);
        var signedInUser = Assert.Single(fixture.Mocks.SignInManager.InteractiveSignInUsers);
        Assert.Same(user, signedInUser);
    }
}
