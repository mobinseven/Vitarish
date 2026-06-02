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
            entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Key).HasColumnType("nvarchar(256)");
            _ = entity.Property(e => e.Currency).HasColumnType("nvarchar(3)");
            _ = entity.Property(e => e.OwnerScope).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(e => e.AccountClass).HasConversion<string>().HasMaxLength(32);
            _ = entity.HasIndex(e => e.Key).IsUnique();
            _ = entity.HasIndex(e => new { e.TenantId, e.OwnerScope });
        });

        _ = modelBuilder.Entity<LedgerEntry>(entity =>
        {
            entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Currency).HasColumnType("nvarchar(3)");
            _ = entity.Property(e => e.State).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(e => e.TemplateId).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.ReasonCode).HasConversion<string>().HasMaxLength(64);
            _ = entity.Property(e => e.Actor).HasColumnType("nvarchar(256)");
            _ = entity.Property(e => e.Source).HasColumnType("nvarchar(128)");
            _ = entity.Property(e => e.CorrelationId).HasColumnType("nvarchar(128)");
            _ = entity.Property(e => e.CausationId).HasColumnType("nvarchar(128)");
            _ = entity.Property(e => e.ProviderReference).HasColumnType("nvarchar(256)");
            _ = entity.HasMany(e => e.Lines)
                .WithOne()
                .HasForeignKey(l => l.LedgerEntryId)
                .OnDelete(DeleteBehavior.Cascade);
            _ = entity.HasIndex(e => e.ReversalOfEntryId);
            _ = entity.HasIndex(e => new { e.TenantId, e.State });
        });

        _ = modelBuilder.Entity<LedgerEntryLine>(entity =>
        {
            entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Side).HasConversion<string>().HasMaxLength(16);
            _ = entity.HasIndex(e => e.LedgerEntryId);
            _ = entity.HasIndex(e => e.AccountId);
        });

        _ = modelBuilder.Entity<IdempotencyRecord>(entity =>
        {
            entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Scope).HasColumnType("nvarchar(128)");
            _ = entity.Property(e => e.IdempotencyKey).HasColumnType("nvarchar(256)");
            _ = entity.Property(e => e.RequestHash).HasColumnType("nvarchar(128)");
            _ = entity.Property(e => e.ResponsePayload).HasColumnType("nvarchar(max)");
            _ = entity.HasIndex(e => new { e.Scope, e.IdempotencyKey }).IsUnique();
        });

        _ = modelBuilder.Entity<ReconciliationRecord>(entity =>
        {
            entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.ExternalReference).HasColumnType("nvarchar(256)");
            _ = entity.Property(e => e.Provider).HasColumnType("nvarchar(128)");
            _ = entity.Property(e => e.Authority).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(e => e.MismatchState).HasConversion<string>().HasMaxLength(32);
            _ = entity.Property(e => e.Currency).HasColumnType("nvarchar(3)");
            _ = entity.HasIndex(e => new { e.Provider, e.ExternalReference });
        });

        _ = modelBuilder.Entity<AdapterInboundEvent>(entity =>
        {
            entity.HasQueryFilter(e => e.DeletedAt == null);
            _ = entity.Property(e => e.Provider).HasColumnType("nvarchar(128)");
            _ = entity.Property(e => e.EventId).HasColumnType("nvarchar(256)");
            _ = entity.Property(e => e.DedupeKey).HasColumnType("nvarchar(256)");
            _ = entity.Property(e => e.SchemaVersion).HasColumnType("nvarchar(32)");
            _ = entity.Property(e => e.RawPayload).HasColumnType("nvarchar(max)");
            _ = entity.Property(e => e.NormalizedPayload).HasColumnType("nvarchar(max)");
            _ = entity.HasIndex(e => new { e.Provider, e.DedupeKey }).IsUnique();
        });
    }
}
