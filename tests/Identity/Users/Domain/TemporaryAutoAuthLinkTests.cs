using Vitarish.Users.Domain.Entities;

namespace Users.Tests.Domain;

public class TemporaryAutoAuthLinkTests
{
    [Fact]
    public void Create_ValidLink_SetsExpectedValues()
    {
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        DateTimeOffset expiresAt = createdAt.AddMinutes(10);

        TemporaryAutoAuthLink link = TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            "hashed-code",
            "user-1",
            "/advanced-input/voice",
            "advanced-input-voice",
            createdAt,
            expiresAt,
            "device");

        _ = link.CodeHash.Should().Be("hashed-code");
        _ = link.UserId.Should().Be("user-1");
        _ = link.DestinationPath.Should().Be("/advanced-input/voice");
        _ = link.Purpose.Should().Be("advanced-input-voice");
        _ = link.CreatedAt.Should().Be(createdAt);
        _ = link.ExpiresAt.Should().Be(expiresAt);
        _ = link.ConsumedAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_MissingCodeHash_Throws(string codeHash)
    {
        Action action = () => TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            codeHash,
            "user-1",
            "/destination",
            "advanced-input-voice",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(5));

        _ = action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_MissingUserId_Throws(string userId)
    {
        Action action = () => TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            "hashed-code",
            userId,
            "/destination",
            "advanced-input-voice",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(5));

        _ = action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_MissingDestinationPath_Throws(string destinationPath)
    {
        Action action = () => TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            "hashed-code",
            "user-1",
            destinationPath,
            "advanced-input-voice",
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(5));

        _ = action.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Create_MissingPurpose_Throws(string purpose)
    {
        Action action = () => TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            "hashed-code",
            "user-1",
            "/destination",
            purpose,
            DateTimeOffset.UtcNow,
            DateTimeOffset.UtcNow.AddMinutes(5));

        _ = action.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Create_InvalidExpirationState_Throws()
    {
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;

        Action action = () => TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            "hashed-code",
            "user-1",
            "/destination",
            "advanced-input-voice",
            createdAt,
            createdAt);

        _ = action.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void MarkConsumed_ValidTimestamp_SetsConsumedFields()
    {
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        TemporaryAutoAuthLink link = TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            "hashed-code",
            "user-1",
            "/destination",
            "advanced-input-voice",
            createdAt,
            createdAt.AddMinutes(5));
        DateTimeOffset consumedAt = createdAt.AddMinutes(1);

        link.MarkConsumed(consumedAt, "127.0.0.1", "agent");

        _ = link.ConsumedAt.Should().Be(consumedAt);
        _ = link.ConsumedByIp.Should().Be("127.0.0.1");
        _ = link.ConsumedByUserAgent.Should().Be("agent");
    }

    [Fact]
    public void MarkConsumed_TimestampEarlierThanCreatedAt_Throws()
    {
        DateTimeOffset createdAt = DateTimeOffset.UtcNow;
        TemporaryAutoAuthLink link = TemporaryAutoAuthLink.Create(
            Guid.NewGuid(),
            "hashed-code",
            "user-1",
            "/destination",
            "advanced-input-voice",
            createdAt,
            createdAt.AddMinutes(5));

        Action action = () => link.MarkConsumed(createdAt.AddSeconds(-1), "127.0.0.1", "agent");

        _ = action.Should().Throw<ArgumentOutOfRangeException>();
    }
}
