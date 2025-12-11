using System.Collections.Generic;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Interface.Tests.TestInfrastructure;

internal sealed class TestSignInManager : SignInManager<User>
{
    public List<User> InteractiveSignInUsers { get; } = new();
    public bool SignOutCalled { get; private set; }
    public List<(User User, string Password, bool IsPersistent, bool LockoutOnFailure)> PasswordSignInAttempts { get; } = new();
    public SignInResult PasswordSignInResult { get; set; } = SignInResult.Success;

    public TestSignInManager(
        UserManager<User> userManager,
        IHttpContextAccessor contextAccessor,
        IUserClaimsPrincipalFactory<User> claimsFactory,
        IOptions<IdentityOptions> optionsAccessor,
        ILogger<SignInManager<User>> logger,
        IAuthenticationSchemeProvider schemes,
        IUserConfirmation<User> confirmation)
        : base(userManager, contextAccessor, claimsFactory, optionsAccessor, logger, schemes, confirmation)
    {
    }

    public override Task SignOutAsync()
    {
        SignOutCalled = true;
        return Task.CompletedTask;
    }

    public override Task SignInAsync(User user, bool isPersistent, string? authenticationMethod = null)
    {
        InteractiveSignInUsers.Add(user);
        return Task.CompletedTask;
    }

    public override Task<SignInResult> PasswordSignInAsync(User user, string password, bool isPersistent, bool lockoutOnFailure)
    {
        PasswordSignInAttempts.Add((user, password, isPersistent, lockoutOnFailure));
        return Task.FromResult(PasswordSignInResult);
    }
}
