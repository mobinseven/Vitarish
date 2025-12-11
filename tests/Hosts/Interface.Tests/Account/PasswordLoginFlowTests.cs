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

public class PasswordLoginFlowTests
{
    [Fact]
    public async Task PasswordFlow_CompletesSuccessfulSignIn()
    {
        using var fixture = new ComponentFixture();
        var user = new User { Id = "user-123", UserName = "alice" };

        fixture.Mocks.Users
            .Setup(s => s.GetUserByNameAsync("alice", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        fixture.Mocks.SignInManager.PasswordSignInResult = SignInResult.Success;

        var submitModel = new PasswordLoginSubmitModel(
            fixture.Mocks.Users.Object,
            fixture.Mocks.SignInManager,
            fixture.Localizer,
            NullLogger<PasswordLoginSubmitModel>.Instance);
        submitModel.PageContext = new PageContext
        {
            HttpContext = fixture.Mocks.HttpContext,
        };
        submitModel.Input = new PasswordLoginModel
        {
            Username = "alice",
            Password = "p@ss",
        };
        submitModel.ReturnUrl = "/callback";
        submitModel.FailureUrl = "/login/password";

        var result = await submitModel.OnPostAsync();

        var redirect = Assert.IsType<Microsoft.AspNetCore.Mvc.RedirectResult>(result);
        Assert.Equal("/Account/SuccessfulLogin?ReturnUrl=%2Fcallback", redirect.Url);

        var attempt = Assert.Single(fixture.Mocks.SignInManager.PasswordSignInAttempts);
        Assert.Same(user, attempt.User);
        Assert.Equal("p@ss", attempt.Password);
        Assert.True(attempt.IsPersistent);
        Assert.False(attempt.LockoutOnFailure);
    }
}
