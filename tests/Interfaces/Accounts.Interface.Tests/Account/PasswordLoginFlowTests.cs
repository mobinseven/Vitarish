using Accounts.Interface.Tests.TestInfrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging.Abstractions;
using Vitarish.Accounts.Domain.Models;
using Vitarish.Main.Pages.Identity;
using Vitarish.Users.Domain.Entities;
using SignInResult = Microsoft.AspNetCore.Identity.SignInResult;

namespace Accounts.Interface.Tests.Account;

public class PasswordLoginFlowTests
{
    [Fact]
    public async Task PasswordFlow_CompletesSuccessfulSignIn()
    {
        using ComponentFixture fixture = new();
        User user = new()
        { Id = "user-123", UserName = "alice" };

        _ = fixture.Mocks.Users
            .Setup(s => s.GetUserByNameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        fixture.Mocks.SignInManager.PasswordSignInResult = SignInResult.Success;

        PasswordLoginSubmitModel submitModel = new(
            fixture.Mocks.Users.Object,
            fixture.Mocks.SignInManager,
            fixture.Localizer,
            NullLogger<PasswordLoginSubmitModel>.Instance)
        {
            PageContext = new PageContext
            {
                HttpContext = fixture.Mocks.HttpContext,
            },
            Input = new PasswordLoginModel
            {
                Username = "alice",
                Password = "p@ss",
            },
            ReturnUrl = "/callback",
            FailureUrl = "/login/password"
        };

        IActionResult result = await submitModel.OnPostAsync();

        RedirectResult redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/Account/SuccessfulLogin?ReturnUrl=%2Fcallback", redirect.Url);

        (User User, string Password, bool IsPersistent, bool LockoutOnFailure) attempt = Assert.Single(fixture.Mocks.SignInManager.PasswordSignInAttempts);
        Assert.Same(user, attempt.User);
        Assert.Equal("p@ss", attempt.Password);
        Assert.True(attempt.IsPersistent);
        Assert.False(attempt.LockoutOnFailure);
    }
}
