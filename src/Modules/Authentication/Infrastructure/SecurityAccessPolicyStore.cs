using Microsoft.EntityFrameworkCore;
using Vitarish.Authentication.Application.Abstractions;
using Vitarish.Authentication.Domain.Methods;
using Vitarish.Authentication.Domain.Security;
using Vitarish.Entities.Server.Data;

namespace Vitarish.Authentication.Infrastructure;

public sealed class SecurityAccessPolicyStore(
    AuthenticationDbContext dbContext,
    AuthenticationProfilePolicy? profilePolicy = null) :
    IAuthenticationSecurityPolicyStore,
    IAuthenticationSecurityPolicyProvider
{
    private readonly AuthenticationProfilePolicy effectiveProfilePolicy = profilePolicy ?? AuthenticationProfilePolicy.Default;

    public async Task<SecurityAccessPolicyState> GetStateAsync(CancellationToken cancellationToken)
    {
        SecurityAccessPolicyRecord? record = await dbContext.SecurityAccessPolicies
            .AsNoTracking()
            .SingleOrDefaultAsync(policy => policy.Id == SecurityAccessPolicyRecord.SingletonId, cancellationToken);
        return record is null
            ? new SecurityAccessPolicyState(SecurityAccessPolicy.Default, 0)
            : record.ToState();
    }

    public Task<SecurityAccessPolicyState> SaveAsync(SecurityAccessPolicy policy, long expectedVersion, CancellationToken cancellationToken) =>
        SaveAsync(dbContext, policy, expectedVersion, cancellationToken);

    internal static async Task<SecurityAccessPolicyState> SaveAsync(
        AuthenticationDbContext dbContext,
        SecurityAccessPolicy policy,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        policy.ValidateConfiguration();
        if (expectedVersion < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedVersion), "Expected policy version cannot be negative.");

        SecurityAccessPolicyRecord? record = null;
        try
        {
            record = await dbContext.SecurityAccessPolicies
                .SingleOrDefaultAsync(existing => existing.Id == SecurityAccessPolicyRecord.SingletonId, cancellationToken);
            if (record is null)
            {
                if (expectedVersion != 0)
                    throw ConcurrentUpdate();
                record = SecurityAccessPolicyRecord.FromDomain(policy);
                _ = dbContext.SecurityAccessPolicies.Add(record);
            }
            else
            {
                if (record.Version != expectedVersion)
                    throw ConcurrentUpdate();
                record.Apply(policy);
            }
            _ = await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw ConcurrentUpdate(exception);
        }
        catch (DbUpdateException exception) when (DatabaseExceptions.IsUniqueConstraintViolation(exception))
        {
            if (record is not null)
                dbContext.Entry(record).State = EntityState.Detached;
            throw ConcurrentUpdate(exception);
        }
        catch (SecurityAccessPolicyConcurrencyException)
        {
            throw;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new SecurityAccessPolicyPersistenceException("The security access policy could not be saved.", exception);
        }

        return record.ToState();
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        const int maxInitializationAttempts = 3;
        for (int attempt = 1; attempt <= maxInitializationAttempts; attempt++)
        {
            SecurityAccessPolicyRecord? existing = await dbContext.SecurityAccessPolicies
                .SingleOrDefaultAsync(item => item.Id == SecurityAccessPolicyRecord.SingletonId, cancellationToken);
            if (existing is not null)
                return;

            SecurityAccessPolicyRecord record = SecurityAccessPolicyRecord.FromDomain(SecurityAccessPolicy.Default);
            _ = dbContext.SecurityAccessPolicies.Add(record);
            try
            {
                await dbContext.SaveChangesAsync(cancellationToken);
                return;
            }
            catch (DbUpdateException exception) when (DatabaseExceptions.IsUniqueConstraintViolation(exception))
            {
                dbContext.Entry(record).State = EntityState.Detached;
                if (attempt == maxInitializationAttempts)
                    throw ConcurrentUpdate(exception);
            }
        }

        throw ConcurrentUpdate();
    }

    async Task<SecurityAccessPolicy> IAuthenticationSecurityPolicyProvider.GetAsync(CancellationToken cancellationToken) =>
        (await GetStateAsync(cancellationToken)).Policy.ReconcileProfile(effectiveProfilePolicy);

    private static SecurityAccessPolicyConcurrencyException ConcurrentUpdate(Exception? innerException = null) =>
        new("The security access policy changed concurrently; retry from a fresh read.", innerException);

}

public sealed class SecurityAccessPolicyRecord
{
    public const int SingletonId = 1;
    public const string SchemaName = "Authentication";

    public int Id { get; set; }
    public int MinimumPasswordLength { get; set; }
    public bool RequirePasswordLowercase { get; set; }
    public bool RequirePasswordUppercase { get; set; }
    public bool RequirePasswordNumber { get; set; }
    public bool RequirePasswordSpecialCharacter { get; set; }
    public int FailedAttemptThreshold { get; set; }
    public int FailedAttemptDenialDurationSeconds { get; set; }
    public FailedAttemptResponse FailedAttemptResponse { get; set; }
    public string EnabledAuthenticationMechanisms { get; set; } = string.Empty;
    public string PermittedPreAuthenticationActions { get; set; } = string.Empty;
    public int MaximumConcurrentSessions { get; set; } = 1;
    public bool ConcurrentSessionsFunctionallyRequired { get; set; }
    public int DefaultInactivityTimeoutMinutes { get; set; } = 30;
    // The persisted column name remains stable while its values now contain normalized CIDRs.
    public string? AllowedSessionStartIpAddresses { get; set; }
    public long Version { get; set; }

    public static SecurityAccessPolicyRecord FromDomain(SecurityAccessPolicy policy) => new()
    {
        Id = SingletonId,
        Version = 0,
        MinimumPasswordLength = policy.PasswordPolicy.MinimumLength,
        RequirePasswordLowercase = policy.PasswordPolicy.RequireLowercase,
        RequirePasswordUppercase = policy.PasswordPolicy.RequireUppercase,
        RequirePasswordNumber = policy.PasswordPolicy.RequireNumber,
        RequirePasswordSpecialCharacter = policy.PasswordPolicy.RequireSpecialCharacter,
        FailedAttemptThreshold = policy.FailedAttemptThreshold,
        FailedAttemptDenialDurationSeconds = checked((int)policy.FailedAttemptDenialDuration.TotalSeconds),
        FailedAttemptResponse = policy.FailedAttemptResponse,
        EnabledAuthenticationMechanisms = Serialize(policy.EnabledAuthenticationMechanisms),
        PermittedPreAuthenticationActions = Serialize(policy.PermittedPreAuthenticationActions),
        MaximumConcurrentSessions = policy.MaximumConcurrentSessions,
        ConcurrentSessionsFunctionallyRequired = policy.ConcurrentSessionsFunctionallyRequired,
        DefaultInactivityTimeoutMinutes = policy.DefaultInactivityTimeoutMinutes,
        AllowedSessionStartIpAddresses = policy.AllowedSessionStartCidrs is null
            ? null
            : Serialize(policy.AllowedSessionStartCidrs)
    };

    public void Apply(SecurityAccessPolicy policy)
    {
        Version++;
        MinimumPasswordLength = policy.PasswordPolicy.MinimumLength;
        RequirePasswordLowercase = policy.PasswordPolicy.RequireLowercase;
        RequirePasswordUppercase = policy.PasswordPolicy.RequireUppercase;
        RequirePasswordNumber = policy.PasswordPolicy.RequireNumber;
        RequirePasswordSpecialCharacter = policy.PasswordPolicy.RequireSpecialCharacter;
        FailedAttemptThreshold = policy.FailedAttemptThreshold;
        FailedAttemptDenialDurationSeconds = checked((int)policy.FailedAttemptDenialDuration.TotalSeconds);
        FailedAttemptResponse = policy.FailedAttemptResponse;
        EnabledAuthenticationMechanisms = Serialize(policy.EnabledAuthenticationMechanisms);
        PermittedPreAuthenticationActions = Serialize(policy.PermittedPreAuthenticationActions);
        MaximumConcurrentSessions = policy.MaximumConcurrentSessions;
        ConcurrentSessionsFunctionallyRequired = policy.ConcurrentSessionsFunctionallyRequired;
        DefaultInactivityTimeoutMinutes = policy.DefaultInactivityTimeoutMinutes;
        AllowedSessionStartIpAddresses = policy.AllowedSessionStartCidrs is null
            ? null
            : Serialize(policy.AllowedSessionStartCidrs);
    }

    public SecurityAccessPolicyState ToState() => new(ToDomain(), Version);

    public SecurityAccessPolicy ToDomain() => new(
        new PasswordPolicy(MinimumPasswordLength, RequirePasswordLowercase, RequirePasswordUppercase, RequirePasswordNumber, RequirePasswordSpecialCharacter),
        FailedAttemptThreshold,
        TimeSpan.FromSeconds(FailedAttemptDenialDurationSeconds),
        FailedAttemptResponse,
        Deserialize<AuthenticationMethod>(EnabledAuthenticationMechanisms),
        Deserialize<PreAuthenticationAction>(PermittedPreAuthenticationActions),
        MaximumConcurrentSessions,
        ConcurrentSessionsFunctionallyRequired,
        DefaultInactivityTimeoutMinutes,
        DeserializeStringSet(AllowedSessionStartIpAddresses));

    private static string Serialize<T>(IEnumerable<T> values) where T : notnull => string.Join(',', values.OrderBy(value => value).Select(value => value.ToString()));

    private static IReadOnlySet<T> Deserialize<T>(string value) where T : struct, Enum
    {
        string[] items = value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return items
            .Select(item => Enum.TryParse(item, ignoreCase: false, out T parsed)
                ? parsed
                : throw new InvalidOperationException($"Unknown {typeof(T).Name} value in the security access policy."))
            .ToHashSet();
    }

    private static IReadOnlySet<string>? DeserializeStringSet(string? value) =>
        value?.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToHashSet(StringComparer.Ordinal);

}
