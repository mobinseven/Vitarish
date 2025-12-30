using Accounts.Interface.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using System.Security.Claims;
using Vitarish.Accounts.Domain.Models;
using Vitarish.Accounts.Interface.Components.Account;
using Vitarish.Main.Pages.Account;
using Vitarish.Main.Pages.Account.Manage;
using Vitarish.Users.Domain.Entities;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Accounts.Interface.Tests.Account;

public class AccountFormSubmitTests
{
    [Fact]
    public async Task PasswordLoginForm_SubmitValid_ShowsSuccess()
    {
        using ComponentFixture fixture = new();
        User user = new() { Id = "user-123", UserName = "alice" };

        _ = fixture.Mocks.Users
            .Setup(service => service.GetUserByNameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        fixture.Mocks.SignInManager.PasswordSignInResult = SignInResult.Success;

        PasswordLoginSubmitModel submitModel = new(
            fixture.Mocks.Users.Object,
            fixture.Mocks.SignInManager,
            fixture.Localizer,
            NullLogger<PasswordLoginSubmitModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = fixture.Mocks.HttpContext },
            Input = new PasswordLoginModel { Username = "alice", Password = "p@ss" },
            ReturnUrl = "/callback",
            FailureUrl = "/Account/Login/Password"
        };

        IActionResult result = await submitModel.OnPostAsync();

        RedirectResult redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Account/SuccessfulLogin?ReturnUrl=%2Fcallback", redirect.Url);
    }

    [Fact]
    public async Task PasswordLoginForm_SubmitInvalid_ShowsValidationErrors()
    {
        using ComponentFixture fixture = new();

        PasswordLoginSubmitModel submitModel = new(
            fixture.Mocks.Users.Object,
            fixture.Mocks.SignInManager,
            fixture.Localizer,
            NullLogger<PasswordLoginSubmitModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = fixture.Mocks.HttpContext },
            Input = new PasswordLoginModel { Username = "", Password = "" },
            FailureUrl = "/Account/Login/Password"
        };
        submitModel.ModelState.AddModelError("Input.Username", "Required");

        IActionResult result = await submitModel.OnPostAsync();

        RedirectResult redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Account/Login/Password", redirect.Url);
    }

    [Fact]
    public async Task EmailLoginForm_SubmitValid_ShowsSuccess()
    {
        using ComponentFixture fixture = new();
        User user = new() { Id = "user-email", Email = "alice@example.com", UserName = "alice" };

        _ = fixture.Mocks.Users
            .Setup(service => service.GetUserAsync(user.Id!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _ = fixture.Mocks.EmailAuthentication
            .Setup(service => service.VerifyCode(user, "123456"))
            .ReturnsAsync(true);
        var emailStore = fixture.Mocks.UserStore.As<IUserEmailStore<User>>();
        _ = emailStore
            .Setup(store => store.GetEmailConfirmedAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ = emailStore
            .Setup(store => store.SetEmailConfirmedAsync(user, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        EmailLoginSubmitModel submitModel = new(
            fixture.Mocks.Users.Object,
            fixture.Mocks.EmailAuthentication.Object,
            fixture.Mocks.SignInManager,
            fixture.Mocks.UserStore.Object,
            fixture.Localizer,
            NullLogger<EmailLoginSubmitModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = fixture.Mocks.HttpContext },
            Input = new VerifyCodeModel
            {
                UserId = user.Id!,
                Code = "123456",
                Provider = TokenOptions.DefaultEmailProvider,
                ReturnUrl = "/callback",
                FailureUrl = "/Account/Login/Email"
            }
        };

        IActionResult result = await submitModel.OnPostAsync();

        RedirectResult redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Account/SuccessfulLogin?ReturnUrl=%2Fcallback", redirect.Url);
        emailStore.Verify(store => store.SetEmailConfirmedAsync(user, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task EmailLoginForm_SubmitInvalid_ShowsValidationErrors()
    {
        using ComponentFixture fixture = new();

        EmailLoginSubmitModel submitModel = new(
            fixture.Mocks.Users.Object,
            fixture.Mocks.EmailAuthentication.Object,
            fixture.Mocks.SignInManager,
            fixture.Mocks.UserStore.Object,
            fixture.Localizer,
            NullLogger<EmailLoginSubmitModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = fixture.Mocks.HttpContext },
            Input = new VerifyCodeModel
            {
                UserId = "",
                Code = "",
                Provider = TokenOptions.DefaultEmailProvider,
                FailureUrl = "/Account/Login/Email"
            }
        };

        IActionResult result = await submitModel.OnPostAsync();

        RedirectResult redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Account/Login/Email", redirect.Url);
    }

    [Fact]
    public async Task PhoneLoginForm_SubmitValid_ShowsSuccess()
    {
        using ComponentFixture fixture = new();
        User user = new() { Id = "user-phone", PhoneNumber = "+123", UserName = "alice" };

        _ = fixture.Mocks.Users
            .Setup(service => service.GetUserAsync(user.Id!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        _ = fixture.Mocks.SmsAuthentication
            .Setup(service => service.VerifyCode(user, "123456"))
            .ReturnsAsync(true);
        var phoneStore = fixture.Mocks.UserStore.As<IUserPhoneNumberStore<User>>();
        _ = phoneStore
            .Setup(store => store.GetPhoneNumberConfirmedAsync(user, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        _ = phoneStore
            .Setup(store => store.SetPhoneNumberConfirmedAsync(user, true, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        PhoneLoginSubmitModel submitModel = new(
            fixture.Mocks.Users.Object,
            fixture.Mocks.SmsAuthentication.Object,
            fixture.Mocks.SignInManager,
            fixture.Mocks.UserStore.Object,
            fixture.Localizer,
            NullLogger<PhoneLoginSubmitModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = fixture.Mocks.HttpContext },
            Input = new VerifyCodeModel
            {
                UserId = user.Id!,
                Code = "123456",
                Provider = TokenOptions.DefaultPhoneProvider,
                ReturnUrl = "/callback",
                FailureUrl = "/Account/Login/Phone"
            }
        };

        IActionResult result = await submitModel.OnPostAsync();

        RedirectResult redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Account/SuccessfulLogin?ReturnUrl=%2Fcallback", redirect.Url);
        phoneStore.Verify(store => store.SetPhoneNumberConfirmedAsync(user, true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PhoneLoginForm_SubmitInvalid_ShowsValidationErrors()
    {
        using ComponentFixture fixture = new();

        PhoneLoginSubmitModel submitModel = new(
            fixture.Mocks.Users.Object,
            fixture.Mocks.SmsAuthentication.Object,
            fixture.Mocks.SignInManager,
            fixture.Mocks.UserStore.Object,
            fixture.Localizer,
            NullLogger<PhoneLoginSubmitModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = fixture.Mocks.HttpContext },
            Input = new VerifyCodeModel
            {
                UserId = "",
                Code = "",
                Provider = TokenOptions.DefaultPhoneProvider,
                FailureUrl = "/Account/Login/Phone"
            }
        };

        IActionResult result = await submitModel.OnPostAsync();

        RedirectResult redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Account/Login/Phone", redirect.Url);
    }

    [Fact]
    public async Task PasswordChangeForm_MismatchedPasswords_ShowsError()
    {
        using ComponentFixture fixture = new();

        PasswordChangeSubmitModel submitModel = new(
            fixture.Mocks.SignInManager,
            fixture.Localizer,
            NullLogger<PasswordChangeSubmitModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = fixture.Mocks.HttpContext },
            Input = new PasswordChangeModel
            {
                CurrentPassword = "old",
                NewPassword = "new",
                ConfirmPassword = "different"
            },
            FailureUrl = "/Account/Manage/Password"
        };
        submitModel.ModelState.AddModelError("Input.ConfirmPassword", "ConfirmPasswordError");

        IActionResult result = await submitModel.OnPostAsync();

        RedirectResult redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Account/Manage/Password", redirect.Url);
    }

    [Fact]
    public async Task PasswordChangeForm_SubmitValid_ShowsSuccess()
    {
        using ComponentFixture fixture = new();
        User user = new() { Id = "user-1", UserName = "alice" };

        fixture.Mocks.HttpContext.RequestServices = new ServiceCollection()
            .AddLogging()
            .AddAuthentication(IdentityConstants.ApplicationScheme)
            .AddCookie(IdentityConstants.ApplicationScheme)
            .Services
            .BuildServiceProvider();

        _ = fixture.Mocks.UserManager
            .Setup(manager => manager.GetUserAsync(It.IsAny<ClaimsPrincipal>()))
            .ReturnsAsync(user);
        _ = fixture.Mocks.UserManager
            .Setup(manager => manager.HasPasswordAsync(user))
            .ReturnsAsync(true);
        _ = fixture.Mocks.UserManager
            .Setup(manager => manager.CheckPasswordAsync(user, "old"))
            .ReturnsAsync(true);
        _ = fixture.Mocks.UserManager
            .Setup(manager => manager.ChangePasswordAsync(user, "old", "new"))
            .ReturnsAsync(IdentityResult.Success);

        PasswordChangeSubmitModel submitModel = new(
            fixture.Mocks.SignInManager,
            fixture.Localizer,
            NullLogger<PasswordChangeSubmitModel>.Instance)
        {
            PageContext = new PageContext { HttpContext = fixture.Mocks.HttpContext },
            Input = new PasswordChangeModel
            {
                CurrentPassword = "old",
                NewPassword = "new",
                ConfirmPassword = "new"
            },
            ReturnUrl = "/Account/Manage"
        };

        IActionResult result = await submitModel.OnPostAsync();

        RedirectResult redirect = Assert.IsType<RedirectResult>(result);
        Assert.Equal("/Account/Manage", redirect.Url);
    }
}
