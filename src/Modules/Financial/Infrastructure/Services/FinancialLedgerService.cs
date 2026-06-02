namespace Vitarish.Financial.Infrastructure.Services;

[RegisterTransient]
internal sealed class FinancialLedgerService(FinancialDbContext dbContext)
    : IFinancialLedgerCommandService, IFinancialLedgerQueryService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public Task<FinancialCommandResult<PostLedgerEntryResponse>> PostAsync(PostLedgerEntryCommand command, CancellationToken cancellationToken)
        => ExecuteIdempotentAsync("post", command.IdempotencyKey, command, async () =>
        {
            FinancialCommandResult<PostLedgerEntryResponse> validation = ValidatePostCommand(command);
            if (!validation.Success)
            {
                return validation;
            }

            List<LedgerAccount> accounts = await LoadAccountsAsync(command, cancellationToken);

            LedgerEntry entry = BuildDraftEntry(command);
            entry.Post(DateTimeOffset.UtcNow);

            foreach (PostLedgerLineRequest line in command.Lines)
            {
                LedgerAccount account = accounts.Single(a => a.Id == line.AccountId);
                long delta = FinancialPostingRules.GetBalanceDelta(account.AccountClass, line.Side, line.AmountMinorUnits);
                account.BalanceMinorUnits += delta;
                account.Sequence += 1;

                entry.Lines.Add(new LedgerEntryLine
                {
                    Id = Guid.CreateVersion7(),
                    LedgerEntryId = entry.Id,
                    AccountId = line.AccountId,
                    Side = line.Side,
                    AmountMinorUnits = line.AmountMinorUnits
                });
            }

            _ = await dbContext.LedgerEntries.AddAsync(entry, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            return FinancialCommandResult<PostLedgerEntryResponse>.Ok(
                new PostLedgerEntryResponse(entry.Id, entry.State, entry.PostedAt, command.IdempotencyKey));
        }, cancellationToken);

    public Task<FinancialCommandResult<PostLedgerEntryResponse>> ReverseAsync(ReverseLedgerEntryCommand command, CancellationToken cancellationToken)
        => ExecuteIdempotentAsync("reverse", command.IdempotencyKey, command, async () =>
        {
            FinancialCommandResult<PostLedgerEntryResponse> metadataValidation = ValidateMetadata(command.Metadata, command.ReasonCode, command.TemplateId);
            if (!metadataValidation.Success)
            {
                return metadataValidation;
            }

            LedgerEntry? original = await dbContext.LedgerEntries
                .Include(e => e.Lines)
                .SingleOrDefaultAsync(e => e.Id == command.EntryId, cancellationToken);

            if (original is null)
            {
                return FinancialCommandResult<PostLedgerEntryResponse>.Validation("Original entry was not found.");
            }

            if (original.State != LedgerEntryState.Posted)
            {
                return FinancialCommandResult<PostLedgerEntryResponse>.Validation("Only posted entries can be reversed.");
            }

            PostLedgerEntryCommand reverseCommand = new(
                original.TenantId,
                command.IdempotencyKey,
                original.Currency,
                original.Exponent,
                command.TemplateId,
                command.ReasonCode,
                command.Metadata,
                original.Lines.Select(l => new PostLedgerLineRequest(
                    l.AccountId,
                    l.Side == LedgerEntrySide.Debit ? LedgerEntrySide.Credit : LedgerEntrySide.Debit,
                    l.AmountMinorUnits)).ToArray());

            FinancialCommandResult<PostLedgerEntryResponse> result = await PostAsync(reverseCommand, cancellationToken);
            if (!result.Success || result.Value is null)
            {
                return result;
            }

            LedgerEntry? reversedEntry = await dbContext.LedgerEntries.SingleAsync(e => e.Id == result.Value.EntryId, cancellationToken);
            reversedEntry.ReversalOfEntryId = original.Id;
            _ = await dbContext.SaveChangesAsync(cancellationToken);

            return result;
        }, cancellationToken);

    public async Task<FinancialCommandResult<Guid>> RecordMismatchAsync(ReconciliationMismatchCommand command, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(command.Provider) || string.IsNullOrWhiteSpace(command.ExternalReference))
        {
            return FinancialCommandResult<Guid>.Validation("Provider and external reference are required.");
        }

        if (command.Exponent is < 0 or > 9)
        {
            return FinancialCommandResult<Guid>.Validation("Exponent must be between 0 and 9.");
        }

        ReconciliationRecord record = new()
        {
            Id = Guid.CreateVersion7(),
            Provider = command.Provider.Trim(),
            ExternalReference = command.ExternalReference.Trim(),
            Authority = command.Authority,
            MismatchState = command.MismatchState,
            Currency = command.Currency.Trim().ToUpperInvariant(),
            Exponent = command.Exponent,
            InternalMinorUnits = command.InternalMinorUnits,
            ExternalMinorUnits = command.ExternalMinorUnits,
            RelatedEntryId = command.RelatedEntryId
        };

        _ = await dbContext.ReconciliationRecords.AddAsync(record, cancellationToken);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return FinancialCommandResult<Guid>.Ok(record.Id);
    }

    public async Task<long?> GetBalanceMinorUnitsAsync(Guid accountId, CancellationToken cancellationToken)
        => await dbContext.LedgerAccounts
            .Where(a => a.Id == accountId)
            .Select(a => (long?)a.BalanceMinorUnits)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, long>> RebuildBalancesFromPostedHistoryAsync(CancellationToken cancellationToken)
    {
        List<LedgerAccount> accounts = await dbContext.LedgerAccounts.OrderBy(a => a.Id).ToListAsync(cancellationToken);
        Dictionary<Guid, long> rebuilt = accounts.ToDictionary(a => a.Id, _ => 0L);

        List<LedgerEntry> posted = await dbContext.LedgerEntries
            .AsNoTracking()
            .Where(e => e.State == LedgerEntryState.Posted)
            .Include(e => e.Lines)
            .ToListAsync(cancellationToken);

        foreach (LedgerEntry entry in posted)
        {
            foreach (LedgerEntryLine line in entry.Lines)
            {
                LedgerAccount? account = accounts.SingleOrDefault(a => a.Id == line.AccountId);
                if (account is null)
                {
                    continue;
                }

                rebuilt[line.AccountId] += FinancialPostingRules.GetBalanceDelta(account.AccountClass, line.Side, line.AmountMinorUnits);
            }
        }

        return rebuilt;
    }

    private async Task<FinancialCommandResult<PostLedgerEntryResponse>> ExecuteIdempotentAsync(
        string scope,
        string idempotencyKey,
        object request,
        Func<Task<FinancialCommandResult<PostLedgerEntryResponse>>> action,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(idempotencyKey))
        {
            return FinancialCommandResult<PostLedgerEntryResponse>.Validation("Idempotency key is required.");
        }

        string normalizedScope = $"financial:{scope}";
        string hash = ComputeHash(request);

        await using IDbContextTransaction? transaction = dbContext.Database.IsRelational()
            ? await dbContext.Database.BeginTransactionAsync(cancellationToken)
            : null;

        IdempotencyRecord? record = await dbContext.IdempotencyRecords
            .SingleOrDefaultAsync(r => r.Scope == normalizedScope && r.IdempotencyKey == idempotencyKey, cancellationToken);

        bool createdRecord = false;

        if (record is not null)
        {
            if (!string.Equals(record.RequestHash, hash, StringComparison.Ordinal))
            {
                return FinancialCommandResult<PostLedgerEntryResponse>.Conflict("Idempotency key was already used with a different request.");
            }

            if (record.IsCompleted && !string.IsNullOrWhiteSpace(record.ResponsePayload))
            {
                PostLedgerEntryResponse? replay = JsonSerializer.Deserialize<PostLedgerEntryResponse>(record.ResponsePayload, JsonOptions);
                if (replay is not null)
                {
                    return FinancialCommandResult<PostLedgerEntryResponse>.Ok(replay, isReplay: true);
                }
            }
        }
        else
        {
            record = new IdempotencyRecord
            {
                Id = Guid.CreateVersion7(),
                Scope = normalizedScope,
                IdempotencyKey = idempotencyKey,
                RequestHash = hash,
                IsCompleted = false
            };

            _ = await dbContext.IdempotencyRecords.AddAsync(record, cancellationToken);
            _ = await dbContext.SaveChangesAsync(cancellationToken);
            createdRecord = true;
        }

        FinancialCommandResult<PostLedgerEntryResponse> result;
        try
        {
            result = await action();
        }
        catch (ArgumentException ex)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            return FinancialCommandResult<PostLedgerEntryResponse>.Validation(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            return FinancialCommandResult<PostLedgerEntryResponse>.Validation(ex.Message);
        }

        if (!result.Success || result.Value is null)
        {
            if (transaction is not null)
            {
                await transaction.RollbackAsync(cancellationToken);
            }
            else if (createdRecord)
            {
                _ = dbContext.IdempotencyRecords.Remove(record);
                _ = await dbContext.SaveChangesAsync(cancellationToken);
            }
            return result;
        }

        record.IsCompleted = true;
        record.ResponsePayload = JsonSerializer.Serialize(result.Value, JsonOptions);

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        if (transaction is not null)
        {
            await transaction.CommitAsync(cancellationToken);
        }

        return result;
    }

    private async Task<List<LedgerAccount>> LoadAccountsAsync(PostLedgerEntryCommand command, CancellationToken cancellationToken)
    {
        Guid[] accountIds = command.Lines.Select(l => l.AccountId).Distinct().OrderBy(id => id).ToArray();

        List<LedgerAccount> accounts = await dbContext.LedgerAccounts
            .Where(a => accountIds.Contains(a.Id))
            .OrderBy(a => a.Id)
            .ToListAsync(cancellationToken);

        if (accounts.Count != accountIds.Length)
        {
            throw new InvalidOperationException("One or more ledger accounts were not found.");
        }

        foreach (LedgerAccount account in accounts)
        {
            account.ValidateOwnership();

            if (!string.Equals(account.Currency, command.Currency, StringComparison.OrdinalIgnoreCase) || account.Exponent != command.Exponent)
            {
                throw new InvalidOperationException("Account currency/exponent does not match entry currency/exponent.");
            }

            if (command.TenantId is null)
            {
                if (account.OwnerScope != LedgerOwnerScope.Platform)
                {
                    throw new InvalidOperationException("Platform command cannot post to tenant-scoped account.");
                }
            }
            else
            {
                if (account.OwnerScope != LedgerOwnerScope.Tenant || account.TenantId != command.TenantId)
                {
                    throw new InvalidOperationException("Cross-tenant posting is not allowed.");
                }
            }
        }

        return accounts;
    }

    private static FinancialCommandResult<PostLedgerEntryResponse> ValidatePostCommand(PostLedgerEntryCommand command)
    {
        FinancialCommandResult<PostLedgerEntryResponse> metadataValidation = ValidateMetadata(command.Metadata, command.ReasonCode, command.TemplateId);
        if (!metadataValidation.Success)
        {
            return metadataValidation;
        }

        _ = Money.Create(1, command.Currency, command.Exponent);

        if (command.Lines.Count < 2)
        {
            return FinancialCommandResult<PostLedgerEntryResponse>.Validation("At least two lines are required for a balanced entry.");
        }

        if (command.Lines.Any(l => l.AmountMinorUnits <= 0))
        {
            return FinancialCommandResult<PostLedgerEntryResponse>.Validation("Line amounts must be greater than zero.");
        }

        long debits = command.Lines.Where(l => l.Side == LedgerEntrySide.Debit).Sum(l => l.AmountMinorUnits);
        long credits = command.Lines.Where(l => l.Side == LedgerEntrySide.Credit).Sum(l => l.AmountMinorUnits);

        return debits <= 0 || credits <= 0 || debits != credits
            ? FinancialCommandResult<PostLedgerEntryResponse>.Validation("Entry lines must be balanced.")
            : FinancialCommandResult<PostLedgerEntryResponse>.Ok(new PostLedgerEntryResponse(Guid.Empty, LedgerEntryState.Draft, null, command.IdempotencyKey));
    }

    private static FinancialCommandResult<PostLedgerEntryResponse> ValidateMetadata(
        FinancialMutationMetadata metadata,
        LedgerReasonCode reasonCode,
        PostingTemplateId templateId) => string.IsNullOrWhiteSpace(metadata.Actor)
            || string.IsNullOrWhiteSpace(metadata.Source)
            || string.IsNullOrWhiteSpace(metadata.CorrelationId)
            || string.IsNullOrWhiteSpace(metadata.CausationId)
            ? FinancialCommandResult<PostLedgerEntryResponse>.Validation("Actor, source, correlation id, and causation id are required.")
            : reasonCode == LedgerReasonCode.Post && templateId == PostingTemplateId.Unknown
            ? FinancialCommandResult<PostLedgerEntryResponse>.Validation("Template id is required for posting operations.")
            : string.Equals(metadata.Source, "provider", StringComparison.OrdinalIgnoreCase)
            && string.IsNullOrWhiteSpace(metadata.ProviderReference)
            ? FinancialCommandResult<PostLedgerEntryResponse>.Validation("Provider reference is required for provider-sourced mutations.")
            : FinancialCommandResult<PostLedgerEntryResponse>.Ok(new PostLedgerEntryResponse(Guid.Empty, LedgerEntryState.Draft, null, string.Empty));

    private static LedgerEntry BuildDraftEntry(PostLedgerEntryCommand command)
        => new()
        {
            Id = Guid.CreateVersion7(),
            TenantId = command.TenantId,
            Currency = command.Currency.Trim().ToUpperInvariant(),
            Exponent = command.Exponent,
            TemplateId = command.TemplateId,
            ReasonCode = command.ReasonCode,
            Actor = command.Metadata.Actor.Trim(),
            Source = command.Metadata.Source.Trim(),
            CorrelationId = command.Metadata.CorrelationId.Trim(),
            CausationId = command.Metadata.CausationId.Trim(),
            ProviderReference = string.IsNullOrWhiteSpace(command.Metadata.ProviderReference) ? null : command.Metadata.ProviderReference.Trim(),
            State = LedgerEntryState.Draft
        };

    private static string ComputeHash(object request)
    {
        string payload = JsonSerializer.Serialize(request, JsonOptions);
        byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(payload));
        return Convert.ToHexString(bytes);
    }
}
