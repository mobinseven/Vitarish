namespace Vitarish.Financial.Infrastructure.Data;

public sealed class FinancialDbContext(DbContextOptions<FinancialDbContext> options, IEnumerable<IInterceptor> interceptors) : DbContext(options)
{
    private readonly IEnumerable<IInterceptor> _interceptors = interceptors;

    public DbSet<LedgerAccount> LedgerAccounts => Set<LedgerAccount>();

    public DbSet<LedgerEntry> LedgerEntries => Set<LedgerEntry>();

    public DbSet<LedgerEntryLine> LedgerEntryLines => Set<LedgerEntryLine>();

    public DbSet<IdempotencyRecord> IdempotencyRecords => Set<IdempotencyRecord>();

    public DbSet<ReconciliationRecord> ReconciliationRecords => Set<ReconciliationRecord>();

    public DbSet<AdapterInboundEvent> AdapterInboundEvents => Set<AdapterInboundEvent>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        => _ = optionsBuilder.AddInterceptors(_interceptors);

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        _ = modelBuilder.Entity<LedgerAccount>(entity =>
        {
            _ = entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Key).HasMaxLength(256);
            _ = entity.Property(e => e.Currency).HasMaxLength(3);
            _ = entity.Property(e => e.OwnerScope).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(e => e.AccountClass).HasConversion<string>().HasMaxLength(32);
            _ = entity.HasIndex(e => e.Key).IsUnique();
            _ = entity.HasIndex(e => new { e.TenantId, e.OwnerScope });
        });

        _ = modelBuilder.Entity<LedgerEntry>(entity =>
        {
            _ = entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Currency).HasMaxLength(3);
            _ = entity.Property(e => e.State).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(e => e.TemplateId).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.ReasonCode).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Actor).HasMaxLength(256);
            _ = entity.Property(e => e.Source).HasMaxLength(128);
            _ = entity.Property(e => e.CorrelationId).HasMaxLength(128);
            _ = entity.Property(e => e.CausationId).HasMaxLength(128);
            _ = entity.Property(e => e.ProviderReference).HasMaxLength(256);
            _ = entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.LedgerEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            _ = entity.HasIndex(e => e.ReversalOfEntryId);
            _ = entity.HasIndex(e => new { e.TenantId, e.State });
        });

        _ = modelBuilder.Entity<LedgerEntryLine>(entity =>
        {
            _ = entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(16);
            _ = entity.HasIndex(e => e.LedgerEntryId);
            _ = entity.HasIndex(e => e.AccountId);
        });

        _ = modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            _ = entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Scope).HasMaxLength(128);
            _ = entity.Property(e => e.IdempotencyKey).HasMaxLength(256);
            _ = entity.Property(e => e.RequestHash).HasMaxLength(128);
            _ = entity.Property(e => e.ResponsePayload);
            _ = entity.HasIndex(e => new { e.Scope, e.IdempotencyKey }).IsUnique();
        });

        _ = modelBuilder.Entity<ReconciliationRecord>(entity =>
        {
            _ = entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.ExternalReference).HasMaxLength(256);
            _ = entity.Property(e => e.Provider).HasMaxLength(128);
            _ = entity.Property(e => e.Authority).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(e => e.MismatchState).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(e => e.Currency).HasMaxLength(3);
            _ = entity.HasIndex(e => new { e.Provider, e.ExternalReference });
        });

        _ = modelBuilder.Entity<AdapterInboundEvent>(entity =>
        {
            _ = entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Provider).HasMaxLength(128);
            _ = entity.Property(e => e.EventId).HasMaxLength(256);
            _ = entity.Property(e => e.DedupeKey).HasMaxLength(256);
            _ = entity.Property(e => e.SchemaVersion).HasMaxLength(32);
            _ = entity.Property(e => e.RawPayload);
            _ = entity.Property(e => e.NormalizedPayload);
            _ = entity.HasIndex(e => new { e.Provider, e.DedupeKey }).IsUnique();
        });
    }
}
