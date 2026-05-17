using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Vitarish.Financial.Application.Abstractions;
using Vitarish.Financial.Application.Contracts;
using Vitarish.Financial.Domain.Entities;
using Vitarish.Financial.Domain.Enums;
using Vitarish.Financial.Infrastructure.Data;
using Vitarish.Financial.Infrastructure.DependencyInjection;

namespace Financial.Tests;

public sealed class FinancialLedgerServiceTests
{
    [Fact]
    public async Task PostAsync_ShouldRejectNonPositiveAmounts()
    {
        await using ServiceProvider provider = CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        FinancialDbContext db = scope.ServiceProvider.GetRequiredService<FinancialDbContext>();
        IFinancialLedgerCommandService service = scope.ServiceProvider.GetRequiredService<IFinancialLedgerCommandService>();

        LedgerAccount debit = CreateTenantAccount("tenant:a:asset", LedgerAccountClass.Asset, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        LedgerAccount credit = CreateTenantAccount("tenant:a:revenue", LedgerAccountClass.Revenue, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        db.LedgerAccounts.AddRange(debit, credit);
        await db.SaveChangesAsync();

        PostLedgerEntryCommand command = BuildCommand(
            tenantId: debit.TenantId,
            idempotencyKey: "k-1",
            debitAccountId: debit.Id,
            creditAccountId: credit.Id,
            amountMinorUnits: 0);

        FinancialCommandResult<PostLedgerEntryResponse> result = await service.PostAsync(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("greater than zero", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PostAsync_SameRequestWithSameKey_ShouldReplaySameResponse()
    {
        await using ServiceProvider provider = CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        FinancialDbContext db = scope.ServiceProvider.GetRequiredService<FinancialDbContext>();
        IFinancialLedgerCommandService service = scope.ServiceProvider.GetRequiredService<IFinancialLedgerCommandService>();

        LedgerAccount debit = CreateTenantAccount("tenant:a:asset", LedgerAccountClass.Asset, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        LedgerAccount credit = CreateTenantAccount("tenant:a:revenue", LedgerAccountClass.Revenue, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        db.LedgerAccounts.AddRange(debit, credit);
        await db.SaveChangesAsync();

        PostLedgerEntryCommand command = BuildCommand(debit.TenantId, "k-2", debit.Id, credit.Id, 250);

        FinancialCommandResult<PostLedgerEntryResponse> first = await service.PostAsync(command, CancellationToken.None);
        FinancialCommandResult<PostLedgerEntryResponse> second = await service.PostAsync(command, CancellationToken.None);

        first.Success.Should().BeTrue();
        second.Success.Should().BeTrue();
        second.IsReplay.Should().BeTrue();
        second.Value!.EntryId.Should().Be(first.Value!.EntryId);
    }

    [Fact]
    public async Task PostAsync_DifferentRequestWithSameKey_ShouldConflict()
    {
        await using ServiceProvider provider = CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        FinancialDbContext db = scope.ServiceProvider.GetRequiredService<FinancialDbContext>();
        IFinancialLedgerCommandService service = scope.ServiceProvider.GetRequiredService<IFinancialLedgerCommandService>();

        LedgerAccount debit = CreateTenantAccount("tenant:a:asset", LedgerAccountClass.Asset, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        LedgerAccount credit = CreateTenantAccount("tenant:a:revenue", LedgerAccountClass.Revenue, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        db.LedgerAccounts.AddRange(debit, credit);
        await db.SaveChangesAsync();

        PostLedgerEntryCommand firstCommand = BuildCommand(debit.TenantId, "k-3", debit.Id, credit.Id, 200);
        PostLedgerEntryCommand conflictingCommand = BuildCommand(debit.TenantId, "k-3", debit.Id, credit.Id, 300);

        FinancialCommandResult<PostLedgerEntryResponse> first = await service.PostAsync(firstCommand, CancellationToken.None);
        FinancialCommandResult<PostLedgerEntryResponse> second = await service.PostAsync(conflictingCommand, CancellationToken.None);

        first.Success.Should().BeTrue();
        second.Success.Should().BeFalse();
        second.IsConflict.Should().BeTrue();
    }

    [Fact]
    public async Task PostAsync_ShouldBlockCrossTenantPosting()
    {
        await using ServiceProvider provider = CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        FinancialDbContext db = scope.ServiceProvider.GetRequiredService<FinancialDbContext>();
        IFinancialLedgerCommandService service = scope.ServiceProvider.GetRequiredService<IFinancialLedgerCommandService>();

        LedgerAccount debit = CreateTenantAccount("tenant:a:asset", LedgerAccountClass.Asset, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        LedgerAccount credit = CreateTenantAccount("tenant:b:revenue", LedgerAccountClass.Revenue, Guid.Parse("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"));

        db.LedgerAccounts.AddRange(debit, credit);
        await db.SaveChangesAsync();

        PostLedgerEntryCommand command = BuildCommand(debit.TenantId, "k-4", debit.Id, credit.Id, 150);

        FinancialCommandResult<PostLedgerEntryResponse> result = await service.PostAsync(command, CancellationToken.None);

        result.Success.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("Cross-tenant", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task RebuildBalancesFromPostedHistory_ShouldMatchProjectedBalances()
    {
        await using ServiceProvider provider = CreateProvider();
        await using AsyncServiceScope scope = provider.CreateAsyncScope();
        FinancialDbContext db = scope.ServiceProvider.GetRequiredService<FinancialDbContext>();
        IFinancialLedgerCommandService commandService = scope.ServiceProvider.GetRequiredService<IFinancialLedgerCommandService>();
        IFinancialLedgerQueryService queryService = scope.ServiceProvider.GetRequiredService<IFinancialLedgerQueryService>();

        LedgerAccount debit = CreateTenantAccount("tenant:a:asset", LedgerAccountClass.Asset, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));
        LedgerAccount credit = CreateTenantAccount("tenant:a:revenue", LedgerAccountClass.Revenue, Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"));

        db.LedgerAccounts.AddRange(debit, credit);
        await db.SaveChangesAsync();

        FinancialCommandResult<PostLedgerEntryResponse> posted = await commandService.PostAsync(
            BuildCommand(debit.TenantId, "k-5", debit.Id, credit.Id, 500),
            CancellationToken.None);

        posted.Success.Should().BeTrue();

        IReadOnlyDictionary<Guid, long> rebuilt = await queryService.RebuildBalancesFromPostedHistoryAsync(CancellationToken.None);
        long? projectedDebit = await queryService.GetBalanceMinorUnitsAsync(debit.Id, CancellationToken.None);
        long? projectedCredit = await queryService.GetBalanceMinorUnitsAsync(credit.Id, CancellationToken.None);

        rebuilt[debit.Id].Should().Be(projectedDebit);
        rebuilt[credit.Id].Should().Be(projectedCredit);
    }

    private static ServiceProvider CreateProvider()
    {
        ServiceCollection services = new();
        _ = services.AddDbContext<FinancialDbContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        _ = services.AddFinancialInfrastructure();
        return services.BuildServiceProvider();
    }

    private static LedgerAccount CreateTenantAccount(string key, LedgerAccountClass accountClass, Guid tenantId)
        => new()
        {
            Id = Guid.CreateVersion7(),
            Key = key,
            OwnerScope = LedgerOwnerScope.Tenant,
            TenantId = tenantId,
            AccountClass = accountClass,
            Currency = "USD",
            Exponent = 2,
            BalanceMinorUnits = 0
        };

    private static PostLedgerEntryCommand BuildCommand(
        Guid? tenantId,
        string idempotencyKey,
        Guid debitAccountId,
        Guid creditAccountId,
        long amountMinorUnits)
        => new(
            tenantId,
            idempotencyKey,
            "USD",
            2,
            PostingTemplateId.PaymentCapture,
            LedgerReasonCode.Post,
            new FinancialMutationMetadata(
                Actor: "tester",
                Source: "internal",
                CorrelationId: "corr",
                CausationId: "cause"),
            [
                new PostLedgerLineRequest(debitAccountId, LedgerEntrySide.Debit, amountMinorUnits),
                new PostLedgerLineRequest(creditAccountId, LedgerEntrySide.Credit, amountMinorUnits)
            ]);
}
