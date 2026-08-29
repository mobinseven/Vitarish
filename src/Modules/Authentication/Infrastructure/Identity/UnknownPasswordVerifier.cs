using Microsoft.AspNetCore.Identity;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Authentication.Infrastructure;

/// <summary>
/// Performs the configured Identity password-hash work for an unknown login
/// without creating or persisting a user-discovery account.
/// </summary>
public sealed class UnknownPasswordVerifier
{
    private const string DummyPassword = "authentication-unknown-login-timing-only";
    private readonly IPasswordHasher<User> passwordHasher;
    private readonly User dummyUser = new()
    {
        Id = "authentication-unknown-login-timing-only",
        UserName = "authentication-unknown-login-timing-only"
    };
    private readonly string dummyHash;

    public UnknownPasswordVerifier(IPasswordHasher<User> passwordHasher)
    {
        this.passwordHasher = passwordHasher;
        dummyHash = passwordHasher.HashPassword(dummyUser, DummyPassword);
    }

    public void Verify(string password) => _ = passwordHasher.VerifyHashedPassword(dummyUser, dummyHash, password);
}
