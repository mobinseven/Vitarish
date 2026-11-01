using System.Security.Claims;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Moq;
using Vitarish.Audit.Application;
using Vitarish.Audit.Domain;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Users.Application.Commands;
using Vitarish.Users.Application.Security;
using Vitarish.Users.Application.Authorization;
using Vitarish.Users.Contracts.Responses;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Domain.Security;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Service;

namespace Vitarish.Users.Tests.Security;

public sealed class UserAccountDeletionTests
{
    private const string SubjectId = "deletion-subject";
    private const string ActorId = "deletion-admin";
    private const string Revision = "deletion-revision";
    private const string Contact = "deletion@example.test";

    [Theory]
    [InlineData(true, 1, false, false, UserDeletionRefusal.SelfDeletion)]
    [InlineData(false, 0, false, false, UserDeletionRefusal.LastPlatformAdministrator)]
    [InlineData(false, 1, true, false, UserDeletionRefusal.LastOwnerProtection)]
    [InlineData(false, 1, false, true, UserDeletionRefusal.ConcurrencyFailure)]
    [InlineData(false, 1, false, false, UserDeletionRefusal.None)]
    public void Domain_protects_identity_ownership_and_revision(bool self, int others, bool soleOwner, bool stale, UserDeletionRefusal expected)
    {
        UserDeletionPolicy.Evaluate(SubjectId, self ? SubjectId : ActorId, stale ? "stale" : Revision, Revision, true, others, soleOwner)
            .Should().Be(expected);
    }

    [Fact]
    public void Erasure_is_terminal_idempotent_and_clears_all_personal_account_fields()
    {
        User user = Account();
        byte[] fingerprint = [1, 2, 3];
        user.PasswordHashFingerprint = fingerprint;
        DateTime now = new(2026, 11, 1, 8, 0, 0, DateTimeKind.Utc);
        user.EraseAccount(now, "rotated", "new-revision");
        user.EraseAccount(now.AddDays(1), "ignored", "ignored");
        user.Id.Should().Be(SubjectId);
        user.DeletedAt.Should().Be(now);
        user.IsEnabled.Should().BeFalse();
        new[] { user.FirstName, user.LastName, user.IdentificationCode, user.BirthCity, user.BirthCountryCode, user.ProfileJson,
            user.Email, user.NormalizedEmail, user.PhoneNumber, user.UserName, user.NormalizedUserName, user.PasswordHash }
            .Should().OnlyContain(value => value == null);
        user.DateOfBirth.Should().BeNull();
        user.PasswordChangedAt.Should().BeNull();
        user.PasswordHashFingerprint.Should().BeNull();
        fingerprint.Should().OnlyContain(value => value == 0);
        user.TwoFactorEnabled.Should().BeFalse();
        user.EmailConfirmed.Should().BeFalse();
        user.PhoneNumberConfirmed.Should().BeFalse();
        user.ConcurrencyStamp.Should().Be("new-revision");
    }

    [Fact]
    public async Task Deletion_retains_id_removes_credentials_and_memberships_and_audits_only_status()
    {
        await using UsersDbContext db = Database();
        await SeedAsync(db);
        List<SecurityAuditEvent> events = [];
        DeleteUserCommandHandler handler = Handler(db, events);
        (await handler.Handle(new(SubjectId, Revision), default)).Succeeded.Should().BeTrue();
        (await db.Users.ToListAsync()).Should().NotContain(user => user.Id == SubjectId);
        User deleted = await db.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == SubjectId);
        deleted.IsDeleted.Should().BeTrue();
        deleted.Email.Should().BeNull();
        (await db.UserRoles.Where(row => row.UserId == SubjectId).CountAsync()).Should().Be(0);
        (await db.UserClaims.CountAsync()).Should().Be(0);
        (await db.UserLogins.CountAsync()).Should().Be(0);
        (await db.UserTokens.CountAsync()).Should().Be(0);
        (await db.WebAuthnCredentials.CountAsync()).Should().Be(0);
        (await db.TemporaryAutoAuthLinks.CountAsync()).Should().Be(0);
        SecurityAuditEvent audit = events.Should().ContainSingle().Subject;
        audit.EventName.Should().Be(SecurityAuditEventCatalog.UserDeletionEvent);
        audit.ActorId.Should().Be(ActorId);
        audit.SubjectId.Should().Be(SubjectId);
        audit.OccurredAt.Kind.Should().Be(DateTimeKind.Utc);
        audit.Changes.Should().Equal(new SecurityAuditChange(SecurityAuditChangeFields.AccountStatus,
            SecurityAuditChangeFields.AccountStatusEnabled, SecurityAuditChangeFields.AccountStatusDeleted));
        System.Text.Json.JsonSerializer.Serialize(audit).Should().NotContain(Contact).And.NotContain("Private name").And.NotContain("private-profile");
    }

    [Theory]
    [InlineData(false, false, Revision, UsersCommandErrorCodes.Unauthorized, SecurityAuditEventCatalog.UserDeletionDeniedEvent)]
    [InlineData(true, true, Revision, UsersCommandErrorCodes.ActiveSessionConflict, SecurityAuditEventCatalog.UserDeletionConflictEvent)]
    [InlineData(true, false, "stale", UsersCommandErrorCodes.ConcurrencyFailure, SecurityAuditEventCatalog.UserDeletionConflictEvent)]
    [InlineData(true, false, null, UsersCommandErrorCodes.ConcurrencyFailure, SecurityAuditEventCatalog.UserDeletionConflictEvent)]
    public async Task Refusals_leave_account_and_credentials_unchanged(bool authorized, bool active, string? revision, string code, string eventName)
    {
        await using UsersDbContext db = Database();
        await SeedAsync(db);
        List<SecurityAuditEvent> events = [];
        IdentityResult result = await Handler(db, events, authorized, active).Handle(new(SubjectId, revision), default);
        result.Errors.Should().ContainSingle(error => error.Code == code);
        (await db.Users.SingleAsync(user => user.Id == SubjectId)).ConcurrencyStamp.Should().Be(Revision);
        (await db.UserTokens.CountAsync()).Should().Be(1);
        events.Should().ContainSingle(item => item.EventName == eventName && item.Outcome == SecurityAuditOutcome.Denied);
    }

    [Fact]
    public async Task Any_clinic_where_subject_is_sole_owner_refuses_with_its_catalogued_conflict()
    {
        await using UsersDbContext db = Database();
        await SeedAsync(db);
        string ownerRole = TenantRoleDefinitions.BuildRoleName(Guid.NewGuid(), IdentityDefinitions.TenantOwner);
        db.Roles.Add(new IdentityRole { Id = "sole-owner-role", Name = ownerRole, NormalizedName = ownerRole.ToUpperInvariant() });
        db.UserRoles.Add(new IdentityUserRole<string> { RoleId = "sole-owner-role", UserId = SubjectId });
        db.Users.Add(new User { Id = "co-owner", UserName = "co-owner@example.test" });
        string sharedRole = TenantRoleDefinitions.BuildRoleName(Guid.NewGuid(), IdentityDefinitions.TenantOwner);
        db.Roles.Add(new IdentityRole { Id = "shared-owner-role", Name = sharedRole, NormalizedName = sharedRole.ToUpperInvariant() });
        db.UserRoles.AddRange(new IdentityUserRole<string> { RoleId = "shared-owner-role", UserId = SubjectId },
            new IdentityUserRole<string> { RoleId = "shared-owner-role", UserId = "co-owner" });
        await db.SaveChangesAsync();
        string revision = (await db.Users.SingleAsync(user => user.Id == SubjectId)).ConcurrencyStamp!;
        db.ChangeTracker.Clear();
        List<SecurityAuditEvent> events = [];
        IdentityResult result = await Handler(db, events).Handle(new(SubjectId, revision), default);
        result.Errors.Should().ContainSingle(error => error.Code == UsersCommandErrorCodes.LastOwnerProtection);
        events.Should().ContainSingle(item => item.EventName == SecurityAuditEventCatalog.UserDeletionLastOwnerConflictEvent &&
            item.RedactedReason == SecurityAuditReasonCatalog.LastTenantOwner && item.Outcome == SecurityAuditOutcome.Denied);
        (await db.UserTokens.CountAsync()).Should().Be(1);
        (await db.Users.SingleAsync(user => user.Id == SubjectId)).IsDeleted.Should().BeFalse();
        db.ChangeTracker.Clear();
        db.UserRoles.Add(new IdentityUserRole<string> { RoleId = "sole-owner-role", UserId = "co-owner" });
        await db.SaveChangesAsync();
        revision = (await db.Users.AsNoTracking().SingleAsync(user => user.Id == SubjectId)).ConcurrencyStamp!;
        (await Handler(db, events).Handle(new(SubjectId, revision), default)).Succeeded.Should().BeTrue();
        (await db.UserRoles.CountAsync(role => role.UserId == "co-owner")).Should().Be(2);
    }

    [Fact]
    public async Task Self_and_last_platform_administrator_refusals_are_audited_without_mutation()
    {
        await using UsersDbContext db = Database();
        await SeedAsync(db);
        List<SecurityAuditEvent> events = [];
        IdentityResult self = await Handler(db, events, actorId: SubjectId).Handle(new(SubjectId, Revision), default);
        self.Errors.Should().ContainSingle(error => error.Code == UsersCommandErrorCodes.SelfDeletion);
        events.Last().RedactedReason.Should().Be(SecurityAuditReasonCatalog.SelfDeletion);
        db.Roles.Add(new IdentityRole { Id = "platform-role", Name = IdentityDefinitions.PlatformAdmin });
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = SubjectId, RoleId = "platform-role" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        IdentityResult last = await Handler(db, events).Handle(new(SubjectId, Revision), default);
        last.Errors.Should().ContainSingle(error => error.Code == UsersCommandErrorCodes.LastPlatformAdministrator);
        events.Last().RedactedReason.Should().Be(SecurityAuditReasonCatalog.LastPlatformAdministrator);
        (await db.UserTokens.CountAsync()).Should().Be(1);
    }

    [Fact]
    public async Task A_disabled_account_does_not_count_as_the_remaining_administrator_or_owner()
    {
        await using UsersDbContext db = Database();
        await SeedAsync(db);
        string ownerRole = TenantRoleDefinitions.BuildRoleName(Guid.NewGuid(), IdentityDefinitions.TenantOwner);
        db.Roles.AddRange(
            new IdentityRole { Id = "platform-role", Name = IdentityDefinitions.PlatformAdmin },
            new IdentityRole { Id = "owner-role", Name = ownerRole, NormalizedName = ownerRole.ToUpperInvariant() });
        db.Users.Add(new User { Id = "disabled-peer", UserName = "disabled-peer@example.test", IsEnabled = false });
        db.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = SubjectId, RoleId = "platform-role" },
            new IdentityUserRole<string> { UserId = "disabled-peer", RoleId = "platform-role" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        List<SecurityAuditEvent> events = [];

        IdentityResult lastAdministrator = await Handler(db, events).Handle(new(SubjectId, Revision), default);
        lastAdministrator.Errors.Should().ContainSingle(error => error.Code == UsersCommandErrorCodes.LastPlatformAdministrator);

        db.UserRoles.RemoveRange(db.UserRoles.Where(row => row.RoleId == "platform-role"));
        db.UserRoles.AddRange(
            new IdentityUserRole<string> { UserId = SubjectId, RoleId = "owner-role" },
            new IdentityUserRole<string> { UserId = "disabled-peer", RoleId = "owner-role" });
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();

        IdentityResult lastOwner = await Handler(db, events).Handle(new(SubjectId, Revision), default);
        lastOwner.Errors.Should().ContainSingle(error => error.Code == UsersCommandErrorCodes.LastOwnerProtection);
    }

    [Fact]
    public async Task Replay_is_successful_without_changing_revision_or_checking_sessions()
    {
        await using UsersDbContext db = Database();
        await SeedAsync(db);
        List<SecurityAuditEvent> events = [];
        await Handler(db, events).Handle(new(SubjectId, Revision), default);
        string? revision = (await db.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == SubjectId)).ConcurrencyStamp;
        (await Handler(db, events, active: true).Handle(new(SubjectId, Revision), default)).Succeeded.Should().BeTrue();
        (await db.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == SubjectId)).ConcurrencyStamp.Should().Be(revision);
        events.Last().Changes.Should().Equal(new SecurityAuditChange(SecurityAuditChangeFields.AccountStatus,
            SecurityAuditChangeFields.AccountStatusDeleted, SecurityAuditChangeFields.AccountStatusDeleted));
    }

    [Fact]
    public async Task Erased_contacts_can_be_assigned_to_a_new_distinct_account_and_stale_writers_cannot_restore_them()
    {
        await using UsersDbContext db = Database();
        await SeedAsync(db);
        User stale = await db.Users.AsNoTracking().SingleAsync(user => user.Id == SubjectId);
        await Handler(db, []).Handle(new(SubjectId, Revision), default);
        User replacement = new() { Id = "replacement", UserName = Contact, NormalizedUserName = Contact.ToUpperInvariant(),
            Email = Contact, NormalizedEmail = Contact.ToUpperInvariant(), PhoneNumber = "+989121234567" };
        db.Users.Add(replacement);
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
        db.Update(stale);
        Func<Task> restore = () => db.SaveChangesAsync();
        await restore.Should().ThrowAsync<DbUpdateConcurrencyException>();
        db.ChangeTracker.Clear();
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = SubjectId, RoleId = "member-role" });
        Func<Task> grant = () => db.SaveChangesAsync();
        await grant.Should().ThrowAsync<DbUpdateConcurrencyException>();
    }

    [Fact]
    public async Task Audit_store_failure_does_not_replace_the_successful_erasure()
    {
        await using UsersDbContext db = Database();
        await SeedAsync(db);
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(writer => writer.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new SecurityAuditStoreException("unavailable", new IOException()));
        (await Handler(db, [], writer: audit.Object).Handle(new(SubjectId, Revision), default)).Succeeded.Should().BeTrue();
        (await db.Users.IgnoreQueryFilters().SingleAsync(user => user.Id == SubjectId)).IsDeleted.Should().BeTrue();
    }

    [Fact]
    public async Task Cache_failure_before_erasure_leaves_persisted_account_and_children_unchanged()
    {
        await using UsersDbContext db = Database();
        await SeedAsync(db);
        Mock<IDistributedCache> cache = new();
        cache.Setup(value => value.RemoveAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("unavailable"));
        List<SecurityAuditEvent> events = [];
        Func<Task> erase = () => Handler(db, events, cache: cache.Object).Handle(new(SubjectId, Revision), default);
        await erase.Should().ThrowAsync<IOException>();
        (await db.Users.SingleAsync(user => user.Id == SubjectId)).Email.Should().Be(Contact);
        (await db.UserTokens.CountAsync()).Should().Be(1);
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Failed &&
            item.Changes!.Single().PreviousValue == SecurityAuditChangeFields.AccountStatusEnabled && item.Changes!.Single().NewValue == null);
    }

    [Fact]
    public async Task Unavailable_session_registry_prevents_erasure_and_records_a_redacted_failed_attempt()
    {
        await using UsersDbContext db = Database();
        await SeedAsync(db);
        List<SecurityAuditEvent> events = [];
        Mock<IAuthenticationActiveSessionQuery> sessions = new();
        sessions.Setup(value => value.HasActiveSessionAsync(SubjectId, It.IsAny<CancellationToken>())).ThrowsAsync(new IOException("unavailable"));
        Func<Task> erase = () => Handler(db, events, sessionQuery: sessions.Object).Handle(new(SubjectId, Revision), default);
        await erase.Should().ThrowAsync<IOException>();
        (await db.Users.SingleAsync()).Email.Should().Be(Contact);
        (await db.UserTokens.CountAsync()).Should().Be(1);
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Failed && item.RedactedReason == SecurityAuditReasonCatalog.MutationRejected);
        System.Text.Json.JsonSerializer.Serialize(events).Should().NotContain(Contact);
    }

    [Fact]
    public async Task Cancellation_while_waiting_for_sign_in_rolls_back_without_erasing_the_account()
    {
        await using UsersDbContext db = Database();
        await SeedAsync(db);
        await using var lease = await new UserSignInGuard(db).AcquireAsync(SubjectId, default);
        using CancellationTokenSource cancellation = new();
        List<SecurityAuditEvent> events = [];
        Task<IdentityResult> pending = Handler(db, events).Handle(new(SubjectId, Revision), cancellation.Token);
        pending.IsCompleted.Should().BeFalse();
        cancellation.Cancel();
        await ((Func<Task>)(async () => await pending)).Should().ThrowAsync<OperationCanceledException>();
        (await db.Users.SingleAsync()).Email.Should().Be(Contact);
        (await db.UserTokens.CountAsync()).Should().Be(1);
        events.Should().ContainSingle(item => item.Outcome == SecurityAuditOutcome.Failed && item.Changes == null);
    }

    private static DeleteUserCommandHandler Handler(UsersDbContext db, List<SecurityAuditEvent> events, bool authorized = true,
        bool active = false, ISecurityAuditWriter? writer = null, IDistributedCache? cache = null, string actorId = ActorId,
        IAuthenticationActiveSessionQuery? sessionQuery = null)
    {
        Mock<IRequestContext> context = new();
        context.SetupGet(value => value.UserId).Returns(actorId);
        context.SetupGet(value => value.RequestId).Returns(Guid.NewGuid().ToString("N"));
        context.SetupGet(value => value.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(authorized
            ? [new Claim(ClaimTypes.Role, IdentityDefinitions.PlatformAdmin)] : Array.Empty<Claim>(), "test")));
        Mock<ISecurityAuditWriter> audit = new();
        audit.Setup(value => value.WriteAsync(It.IsAny<SecurityAuditEvent>(), It.IsAny<CancellationToken>()))
            .Callback<SecurityAuditEvent, CancellationToken>((item, _) => events.Add(item)).ReturnsAsync(SecurityAuditWriteResult.Recorded);
        Mock<IAuthenticationActiveSessionQuery> sessions = new();
        sessions.Setup(value => value.HasActiveSessionAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync(active);
        return new(new UserAccountDeletionStore(db, cache ?? new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions()))),
            sessionQuery ?? sessions.Object, context.Object, new UsersManagementAudit(writer ?? audit.Object, context.Object, TimeProvider.System), TimeProvider.System);
    }

    internal static UsersDbContext Database() => new(new DbContextOptionsBuilder<UsersDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static User Account() => new()
    {
        Id = SubjectId, UserName = Contact, NormalizedUserName = Contact.ToUpperInvariant(), Email = Contact, NormalizedEmail = Contact.ToUpperInvariant(),
        PhoneNumber = "+989121234567", FirstName = "Private name", LastName = "Private surname", IdentificationCode = "1234567890",
        DateOfBirth = new(2000, 1, 1), BirthCity = "Private city", BirthCountryCode = "IR", ProfileJson = "private-profile",
        PasswordHash = "hash", PasswordChangedAt = DateTime.UtcNow, TwoFactorEnabled = true, EmailConfirmed = true, PhoneNumberConfirmed = true,
        ConcurrencyStamp = Revision
    };

    private static async Task SeedAsync(UsersDbContext db)
    {
        db.Users.Add(Account());
        db.Roles.Add(new IdentityRole { Id = "member-role", Name = IdentityDefinitions.PlatformUser });
        db.UserRoles.Add(new IdentityUserRole<string> { UserId = SubjectId, RoleId = "member-role" });
        db.UserClaims.Add(new IdentityUserClaim<string> { UserId = SubjectId, ClaimType = IdentityDefinitions.AccessClaim, ClaimValue = "test-access" });
        db.UserTokens.Add(new IdentityUserToken<string> { UserId = SubjectId, LoginProvider = "test-provider", Name = "test-token", Value = "protected-value" });
        db.UserLogins.Add(new IdentityUserLogin<string> { UserId = SubjectId, LoginProvider = "test-provider", ProviderKey = "test-key" });
        db.WebAuthnCredentials.Add(new WebAuthnCredential { UserId = SubjectId, CredentialId = [1], PublicKey = [2], CredentialType = WebAuthnCredential.PublicKeyCredentialType });
        db.TemporaryAutoAuthLinks.Add(TemporaryAutoAuthLink.Create(Guid.NewGuid(), "protected-link-hash", SubjectId,
            "/test-destination", "test-purpose", DateTime.UtcNow, DateTime.UtcNow.AddMinutes(1)));
        await db.SaveChangesAsync();
        db.ChangeTracker.Clear();
    }
}
