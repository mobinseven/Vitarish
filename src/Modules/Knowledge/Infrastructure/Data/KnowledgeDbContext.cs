using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Knowledge.Domain.Entities;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Knowledge.Infrastructure.Data;

public sealed class KnowledgeDbContext : DbContext, IMultiTenantDbContext
{
    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly IRequestContext _requestContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public KnowledgeDbContext(
        DbContextOptions<KnowledgeDbContext> options,
        IEnumerable<IInterceptor> interceptors,
        IRequestContext requestContext,
        IHttpContextAccessor httpContextAccessor) : base(options)
    {
        _interceptors = interceptors;
        _requestContext = requestContext;
        _httpContextAccessor = httpContextAccessor;
        this.EnforceMultiTenantOnTracking();
    }

    public ITenantInfo? TenantInfo
    {
        get
        {
            string? tenantId = _requestContext.TenantId
                ?? _httpContextAccessor.HttpContext?.GetTenantId();
            return tenantId is null ? null : new TenantInfo { Id = tenantId, Identifier = tenantId, Name = tenantId };
        }
    }

    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;

    public DbSet<KnowledgeCorpusEntity> Corpora => Set<KnowledgeCorpusEntity>();
    public DbSet<KnowledgeSourceEntity> Sources => Set<KnowledgeSourceEntity>();
    public DbSet<KnowledgeDocumentEntity> Documents => Set<KnowledgeDocumentEntity>();
    public DbSet<KnowledgeChunkEntity> Chunks => Set<KnowledgeChunkEntity>();
    public DbSet<KnowledgeQuestionEntity> Questions => Set<KnowledgeQuestionEntity>();
    public DbSet<KnowledgeAskTraceEntity> AskTraces => Set<KnowledgeAskTraceEntity>();
    public DbSet<KnowledgeIndexRunEntity> IndexRuns => Set<KnowledgeIndexRunEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) => _ = optionsBuilder.AddInterceptors(_interceptors);

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        this.EnforceMultiTenant();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override Task<int> SaveChangesAsync(bool acceptAllChangesOnSuccess, CancellationToken cancellationToken = default)
    {
        this.EnforceMultiTenant();
        return base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema(nameof(Knowledge));
        _ = modelBuilder.ConfigureMultiTenant();

        modelBuilder.Entity<KnowledgeCorpusEntity>(entity =>
        {
            entity.ToTable("KnowledgeCorpora");
            entity.HasKey(corpus => corpus.Id);
            entity.Property(corpus => corpus.Name).HasMaxLength(512).IsRequired();
            entity.Property(corpus => corpus.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(corpus => corpus.Description).HasMaxLength(2048);
            entity.HasIndex(corpus => corpus.TenantId);
            _ = entity.IsMultiTenant();
        });

        modelBuilder.Entity<KnowledgeSourceEntity>(entity =>
        {
            entity.ToTable("KnowledgeSources");
            entity.HasKey(source => source.Id);
            entity.Property(source => source.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(source => source.Name).HasMaxLength(512).IsRequired();
            entity.Property(source => source.SourceType).HasMaxLength(64).IsRequired();
            entity.Property(source => source.OriginalLocation).HasMaxLength(2048);
            entity.Property(source => source.Version).HasMaxLength(128);
            entity.Property(source => source.Checksum).HasMaxLength(256);
            entity.HasIndex(source => source.CorpusId);
            entity.HasOne<KnowledgeCorpusEntity>().WithMany().HasForeignKey(source => source.CorpusId).OnDelete(DeleteBehavior.Cascade);
            _ = entity.IsMultiTenant();
        });

        modelBuilder.Entity<KnowledgeDocumentEntity>(entity =>
        {
            entity.ToTable("KnowledgeDocuments");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(document => document.Title).HasMaxLength(512).IsRequired();
            entity.Property(document => document.Text).IsRequired();
            entity.Property(document => document.Product).HasMaxLength(128);
            entity.Property(document => document.Version).HasMaxLength(128);
            entity.Property(document => document.Language).HasMaxLength(32);
            entity.HasIndex(document => document.CorpusId);
            entity.HasOne<KnowledgeSourceEntity>().WithMany().HasForeignKey(document => document.SourceId).OnDelete(DeleteBehavior.Cascade);
            _ = entity.IsMultiTenant();
        });

        modelBuilder.Entity<KnowledgeChunkEntity>(entity =>
        {
            entity.ToTable("KnowledgeChunks");
            entity.HasKey(chunk => chunk.Id);
            entity.Property(chunk => chunk.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(chunk => chunk.Text).IsRequired();
            entity.Property(chunk => chunk.Title).HasMaxLength(512);
            entity.Property(chunk => chunk.HeadingPath).HasMaxLength(2048);
            entity.HasIndex(chunk => new { chunk.CorpusId, chunk.SourceId, chunk.DocumentId, chunk.Order });
            entity.HasOne<KnowledgeDocumentEntity>().WithMany().HasForeignKey(chunk => chunk.DocumentId).OnDelete(DeleteBehavior.Cascade);
            _ = entity.IsMultiTenant();
        });

        modelBuilder.Entity<KnowledgeQuestionEntity>(entity =>
        {
            entity.ToTable("KnowledgeQuestions");
            entity.HasKey(question => question.Id);
            entity.Property(question => question.Question).HasMaxLength(2048).IsRequired();
            entity.Property(question => question.ExpectedAnswer).HasMaxLength(4096);
            entity.Property(question => question.ExpectedChunkIds)
                .HasColumnName("ExpectedChunkIdsJson")
                .IsRequired();
            entity.HasIndex(question => question.CorpusId);
            entity.HasOne<KnowledgeCorpusEntity>().WithMany().HasForeignKey(question => question.CorpusId).OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeIndexRunEntity>(entity =>
        {
            entity.ToTable("KnowledgeIndexRuns");
            entity.HasKey(run => run.Id);
            entity.Property(run => run.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(run => run.Scope).HasMaxLength(32).IsRequired();
            entity.HasIndex(run => new { run.TenantId, run.CorpusId, run.CreatedAt });
            entity.HasOne<KnowledgeCorpusEntity>().WithMany().HasForeignKey(run => run.CorpusId).OnDelete(DeleteBehavior.Cascade);
            _ = entity.IsMultiTenant();
        });

        modelBuilder.Entity<KnowledgeAskTraceEntity>(entity =>
        {
            entity.ToTable("KnowledgeAskTraces");
            entity.HasKey(trace => trace.Id);
            entity.Property(trace => trace.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(trace => trace.Question).HasMaxLength(2048).IsRequired();
            entity.Property(trace => trace.RetrievedChunkIdsJson).IsRequired();
            entity.Property(trace => trace.GeneratedAnswer).IsRequired();
            entity.Property(trace => trace.MissingInformation).HasMaxLength(4096);
            entity.Property(trace => trace.CitationsJson).IsRequired();
            entity.Property(trace => trace.TraceKind).HasMaxLength(64);
            entity.HasIndex(trace => new { trace.TenantId, trace.CorpusId, trace.CreatedAt });
            entity.HasOne<KnowledgeCorpusEntity>().WithMany().HasForeignKey(trace => trace.CorpusId).OnDelete(DeleteBehavior.Cascade);
            _ = entity.IsMultiTenant();
        });
    }
}
