using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;
using Vitarish.Users.Application.Queries;
using Vitarish.Users.Domain.Entities;
using Vitarish.Users.Infrastructure.Data;
using Vitarish.Users.Infrastructure.Handlers;

namespace Vitarish.Users.Tests.Security;

public sealed class GetUserPasswordStatusQueryHandlerTests
{
    [Fact]
    public async Task Legacy_password_rows_without_a_fingerprint_have_no_known_date()
    {
        await using UsersDbContext db = CreateDb();
        DateTime oldDate = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        db.Users.Add(new User
        {
            UserName = "legacy-password-row",
            PasswordHash = "legacy-salted-password-hash",
            PasswordChangedAt = oldDate
        });
        await db.SaveChangesAsync();
        string userId = await db.Users.Select(user => user.Id).SingleAsync();

        UserPasswordStatus? status = await ReadStatusAsync(db, userId);

        status.Should().BeEquivalentTo(new UserPasswordStatus(true, null));
        User persisted = await db.Users.AsNoTracking().SingleAsync(user => user.Id == userId);
        persisted.PasswordHashFingerprint.Should().BeNull();
        persisted.PasswordChangedAt.Should().Be(oldDate);
    }

    [Fact]
    public async Task A_hash_mismatch_returns_unknown_without_mutating_the_persisted_fingerprint()
    {
        await using UsersDbContext db = CreateDb();
        DateTime oldDate = new(2026, 10, 1, 8, 0, 0, DateTimeKind.Utc);
        byte[] fingerprint = SHA256.HashData(Encoding.UTF8.GetBytes("previous-salted-password-hash"));
        User user = new()
        {
            UserName = "mismatched-password-row",
            PasswordHash = "current-salted-password-hash",
            PasswordChangedAt = oldDate,
            PasswordHashFingerprint = fingerprint
        };
        db.Users.Add(user);
        await db.SaveChangesAsync();
        byte[] expectedFingerprint = fingerprint.ToArray();

        UserPasswordStatus? status = await ReadStatusAsync(db, user.Id);

        status.Should().BeEquivalentTo(new UserPasswordStatus(true, null));
        user.PasswordHashFingerprint.Should().Equal(expectedFingerprint);
        user.PasswordChangedAt.Should().Be(oldDate);
    }

    private static UsersDbContext CreateDb() => new(new DbContextOptionsBuilder<UsersDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString("N")).Options);

    private static Task<UserPasswordStatus?> ReadStatusAsync(UsersDbContext db, string userId) =>
        new GetUserPasswordStatusQueryHandler(db).Handle(new GetUserPasswordStatusQuery(userId), CancellationToken.None);
}
