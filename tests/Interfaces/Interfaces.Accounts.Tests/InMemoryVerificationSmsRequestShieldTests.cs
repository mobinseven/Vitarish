using FluentAssertions;
using Microsoft.Extensions.Options;
using Vitarish.Interfaces.Accounts.Services;

namespace Interfaces.Accounts.Tests;

public sealed class InMemoryVerificationSmsRequestShieldTests
{
    private static readonly DateTimeOffset StartTime = new(2026, 6, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void TryAcquireSendSlot_FirstRequest_AllowsSms()
    {
        InMemoryVerificationSmsRequestShield shield = CreateShield();

        VerificationSmsShieldDecision decision = shield.TryAcquireSendSlot("login:+32400000000");

        decision.CanSendSms.Should().BeTrue();
        decision.RemainingCooldown.Should().Be(TimeSpan.Zero);
    }

    [Fact]
    public void TryAcquireSendSlot_DuplicateWithinCooldown_ShieldsSms()
    {
        InMemoryVerificationSmsRequestShield shield = CreateShield();

        _ = shield.TryAcquireSendSlot("login:+32400000000");
        VerificationSmsShieldDecision decision = shield.TryAcquireSendSlot("login:+32400000000");

        decision.CanSendSms.Should().BeFalse();
        decision.RemainingCooldown.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public void TryAcquireSendSlot_RequestAfterCooldown_AllowsSmsAgain()
    {
        AdjustableTimeProvider timeProvider = new(StartTime);
        InMemoryVerificationSmsRequestShield shield = CreateShield(timeProvider);

        _ = shield.TryAcquireSendSlot("login:+32400000000");
        timeProvider.Advance(TimeSpan.FromSeconds(61));
        VerificationSmsShieldDecision decision = shield.TryAcquireSendSlot("login:+32400000000");

        decision.CanSendSms.Should().BeTrue();
    }

    [Fact]
    public void TryAcquireSendSlot_WhenCleanupIsDue_RemovesExpiredEntries()
    {
        AdjustableTimeProvider timeProvider = new(StartTime);
        InMemoryVerificationSmsRequestShield shield = CreateShield(timeProvider);

        _ = shield.TryAcquireSendSlot("login:+32400000000");
        _ = shield.TryAcquireSendSlot("login:+32400000001");
        timeProvider.Advance(TimeSpan.FromSeconds(61));
        _ = shield.TryAcquireSendSlot("login:+32400000002");

        shield.RecentRequestCount.Should().Be(1);
    }

    [Fact]
    public void TryAcquireSendSlot_DifferentKeys_DoNotBlockEachOther()
    {
        InMemoryVerificationSmsRequestShield shield = CreateShield();

        _ = shield.TryAcquireSendSlot("login:+32400000000");
        VerificationSmsShieldDecision decision = shield.TryAcquireSendSlot("signup:+32400000000");

        decision.CanSendSms.Should().BeTrue();
    }

    private static InMemoryVerificationSmsRequestShield CreateShield(TimeProvider? timeProvider = null) =>
        new(
            Options.Create(new VerificationSmsShieldOptions
            {
                VerificationSmsCooldownSeconds = 60,
                VerificationSmsRecentRequestCleanupIntervalSeconds = 30
            }),
            timeProvider ?? new AdjustableTimeProvider(StartTime));
}
