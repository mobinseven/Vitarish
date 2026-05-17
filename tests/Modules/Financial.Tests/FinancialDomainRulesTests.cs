using FluentAssertions;
using Vitarish.Financial.Domain.Entities;
using Vitarish.Financial.Domain.Enums;
using Vitarish.Financial.Domain.Models;
using Xunit;

namespace Financial.Tests;

public sealed class FinancialDomainRulesTests
{
    [Fact]
    public void Money_Create_ShouldRejectInvalidExponent()
    {
        Action act = () => Money.Create(1, "USD", -1);

        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void LedgerEntry_PostedEntryCannotBeVoided()
    {
        LedgerEntry entry = new()
        {
            Id = Guid.CreateVersion7(),
            Currency = "USD",
            Exponent = 2,
            TemplateId = PostingTemplateId.ManualAdjustment,
            ReasonCode = LedgerReasonCode.Post,
            Actor = "system",
            Source = "internal",
            CorrelationId = "corr-1",
            CausationId = "cause-1"
        };

        entry.Lines.Add(new LedgerEntryLine
        {
            Id = Guid.CreateVersion7(),
            LedgerEntryId = entry.Id,
            AccountId = Guid.NewGuid(),
            Side = LedgerEntrySide.Debit,
            AmountMinorUnits = 100
        });
        entry.Lines.Add(new LedgerEntryLine
        {
            Id = Guid.CreateVersion7(),
            LedgerEntryId = entry.Id,
            AccountId = Guid.NewGuid(),
            Side = LedgerEntrySide.Credit,
            AmountMinorUnits = 100
        });

        entry.Post(DateTimeOffset.UtcNow);
        Action act = () => entry.VoidDraft();

        act.Should().Throw<InvalidOperationException>();
    }
}
