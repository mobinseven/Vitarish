using System.Security.Claims;
using FluentAssertions;
using Vitarish.Audit.Application;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Application.Results;
using Vitarish.Authentication.Application.Security;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Authentication.Infrastructure;
using Vitarish.Authentication.Infrastructure.Identity;
using Vitarish.Users.Application.Abstractions;
using Vitarish.Modules.Base.Core.Endpoints;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Security;

public sealed class DeletedAccountAuthenticationTests
{
    [Fact]
    public async Task Deletion_invalidates_directory_identifiers_passkeys_and_preissued_user_principals_even_with_a_tracked_account()
    {
        await using UsersDbContext db = UserAccountDeletionTests.Database();
        const string email = "authentication-deletion@example.test";
        User user = new() { Id = Guid.NewGuid().ToString(), UserName = email, Email = email, PhoneNumber = "+989123456789" };
        using UserManager<User> manager = Manager(db);
        (await manager.CreateAsync(user)).Succeeded.Should().BeTrue();
        AspNetIdentityAuthenticationUserDirectory directory = new(manager, Mock.Of<ISender>(), new HttpContextAccessor());
        (await directory.FindByIdAsync(user.Id, default)).Should().NotBeNull();
        ClaimsPrincipal issued = new(new ClaimsIdentity([new Claim(AuthenticationSessionClaimTypes.Subject, user.Id)], IdentityConstants.ApplicationScheme));
        LiveAccountClaimsTransformation validation = new(directory);
        (await validation.TransformAsync(issued)).Identity!.IsAuthenticated.Should().BeTrue();
        db.WebAuthnCredentials.Add(new WebAuthnCredential { UserId = user.Id, CredentialId = [1, 2], PublicKey = [3, 4], CredentialType = WebAuthnCredential.PublicKeyCredentialType });
        await db.SaveChangesAsync();
        UserAccountDeletionStore store = new(db, new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())));
        await using (IUserAccountDeletionScope scope = await store.BeginAsync(user.Id, default))
            await scope.EraseAsync(DateTime.UtcNow, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), default);

        (await directory.FindByIdAsync(user.Id, default)).Should().BeNull("an outstanding OTP/recovery/handoff resolves the old user ID");
        (await directory.FindByLoginAsync(email, default)).Should().BeNull();
        (await directory.FindByEmailAsync(email, default)).Should().BeNull();
        (await directory.FindByPhoneNumberAsync(user.PhoneNumber!, default)).Should().BeNull();
        (await db.WebAuthnCredentials.CountAsync()).Should().Be(0);
        (await validation.TransformAsync(issued)).Identity!.IsAuthenticated.Should().BeFalse("a signed token/cookie is insufficient after its subject is deleted");
        Mock<IAuthenticationSessionRegistry> sessions = new(MockBehavior.Strict);
        Mock<SignInManager<User>> signIn = new(manager, new HttpContextAccessor(), Mock.Of<IUserClaimsPrincipalFactory<User>>(),
            Options.Create(new IdentityOptions()), NullLogger<SignInManager<User>>.Instance,
            Mock.Of<IAuthenticationSchemeProvider>(), Mock.Of<IUserConfirmation<User>>());
        IAuthenticationAttemptTracker attempts = Mock.Of<IAuthenticationAttemptTracker>();
        IAuthenticationAttemptKeyFactory keys = Mock.Of<IAuthenticationAttemptKeyFactory>();
        AspNetIdentitySignInService service = new(manager, signIn.Object, Mock.Of<IAuthenticationSecurityPolicyProvider>(), attempts,
            new UnknownPasswordVerifier(new PasswordHasher<User>()), keys, sessions.Object, new HttpContextAccessor(),
            new AuthenticationSourceAttemptGuard(attempts, keys, Mock.Of<IClientIpAddressAccessor>(), Mock.Of<ISecurityAuditWriter>(), Mock.Of<IRequestContext>()),
            new UserSignInGuard(db));
        AuthenticationSessionSignInResult denied = await service.SignInAsync(user.Id, default);
        denied.Succeeded.Should().BeFalse();
        denied.Failure!.Reason.Should().Be(AuthenticationFailureReason.InvalidCredentials);
        sessions.VerifyNoOtherCalls();
        await using IUserSignInLease lease = await new UserSignInGuard(db).AcquireAsync(user.Id, default);
        lease.AccountAvailable.Should().BeFalse();
    }

    [Fact]
    public async Task Unknown_or_deleted_user_tokens_fail_closed_while_subjectless_client_credentials_keep_their_semantics()
    {
        Mock<IAuthenticationUserDirectory> users = new();
        LiveAccountClaimsTransformation validation = new(users.Object);
        ClaimsPrincipal token = new(new ClaimsIdentity([new Claim(AuthenticationSessionClaimTypes.Subject, "missing")], ModuleEndpointAuthenticationSchemes.Bearer));
        (await validation.TransformAsync(token)).Identity!.IsAuthenticated.Should().BeFalse();
        ClaimsPrincipal client = new(new ClaimsIdentity([new Claim(IdentityDefinitions.ClientIdClaim, "test-client")], ModuleEndpointAuthenticationSchemes.Bearer));
        (await validation.TransformAsync(client)).Should().BeSameAs(client);
    }

    private static UserManager<User> Manager(UsersDbContext db) => new(new UserStore<User>(db), Options.Create(new IdentityOptions()),
        new PasswordHasher<User>(), [new UserValidator<User>()], [new PasswordValidator<User>()], new UpperInvariantLookupNormalizer(),
        new IdentityErrorDescriber(), new ServiceCollection().BuildServiceProvider(), NullLogger<UserManager<User>>.Instance);
}
