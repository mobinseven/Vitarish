using Microsoft.EntityFrameworkCore;

namespace Vitarish.Knowledge.Infrastructure.Data;

public sealed class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options)
{
    public DbSet<KnowledgeSourceEntity> Sources => Set<KnowledgeSourceEntity>();
    public DbSet<KnowledgeDocumentEntity> Documents => Set<KnowledgeDocumentEntity>();
    public DbSet<KnowledgeChunkEntity> Chunks => Set<KnowledgeChunkEntity>();
    public DbSet<KnowledgeQuestionEntity> Questions => Set<KnowledgeQuestionEntity>();
    public DbSet<KnowledgeAskTraceEntity> AskTraces => Set<KnowledgeAskTraceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<KnowledgeSourceEntity>(entity =>
        {
            entity.ToTable("KnowledgeSources");
            entity.HasKey(source => source.Id);
            entity.Property(source => source.Id).HasMaxLength(64);
            entity.Property(source => source.CorpusId).HasMaxLength(128).IsRequired();
            entity.Property(source => source.TenantId).HasMaxLength(128);
            entity.Property(source => source.Name).HasMaxLength(512).IsRequired();
            entity.Property(source => source.SourceType).HasMaxLength(64).IsRequired();
            entity.Property(source => source.OriginalLocation).HasMaxLength(2048);
            entity.Property(source => source.Version).HasMaxLength(128);
            entity.Property(source => source.Checksum).HasMaxLength(256);
            entity.HasIndex(source => source.CorpusId);
        });

        modelBuilder.Entity<KnowledgeDocumentEntity>(entity =>
        {
            entity.ToTable("KnowledgeDocuments");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.Id).HasMaxLength(64);
            entity.Property(document => document.CorpusId).HasMaxLength(128).IsRequired();
            entity.Property(document => document.TenantId).HasMaxLength(128);
            entity.Property(document => document.SourceId).HasMaxLength(64).IsRequired();
            entity.Property(document => document.Title).HasMaxLength(512).IsRequired();
            entity.Property(document => document.Text).IsRequired();
            entity.Property(document => document.Product).HasMaxLength(128);
            entity.Property(document => document.Version).HasMaxLength(128);
            entity.Property(document => document.Language).HasMaxLength(32);
            entity.HasIndex(document => document.CorpusId);
        });

        modelBuilder.Entity<KnowledgeChunkEntity>(entity =>
        {
            entity.ToTable("KnowledgeChunks");
            entity.HasKey(chunk => chunk.Id);
            entity.Property(chunk => chunk.Id).HasMaxLength(64);
            entity.Property(chunk => chunk.CorpusId).HasMaxLength(128).IsRequired();
            entity.Property(chunk => chunk.TenantId).HasMaxLength(128);
            entity.Property(chunk => chunk.SourceId).HasMaxLength(64).IsRequired();
            entity.Property(chunk => chunk.DocumentId).HasMaxLength(64).IsRequired();
            entity.Property(chunk => chunk.Text).IsRequired();
            entity.Property(chunk => chunk.Title).HasMaxLength(512);
            entity.Property(chunk => chunk.HeadingPath).HasMaxLength(2048);
            entity.HasIndex(chunk => new { chunk.CorpusId, chunk.Order });
        });

        modelBuilder.Entity<KnowledgeQuestionEntity>(entity =>
        {
            entity.ToTable("KnowledgeQuestions");
            entity.HasKey(question => question.Id);
            entity.Property(question => question.Id).HasMaxLength(64);
            entity.Property(question => question.CorpusId).HasMaxLength(128).IsRequired();
            entity.Property(question => question.Question).HasMaxLength(2048).IsRequired();
            entity.Property(question => question.ExpectedAnswer).HasMaxLength(4096);
            entity.Property(question => question.ExpectedChunkIdsJson).IsRequired();
            entity.HasIndex(question => question.CorpusId);
        });

        modelBuilder.Entity<KnowledgeAskTraceEntity>(entity =>
        {
            entity.ToTable("KnowledgeAskTraces");
            entity.HasKey(trace => trace.Id);
            entity.Property(trace => trace.Id).HasMaxLength(64);
            entity.Property(trace => trace.CorpusId).HasMaxLength(128).IsRequired();
            entity.Property(trace => trace.Question).HasMaxLength(2048).IsRequired();
            entity.Property(trace => trace.RetrievedChunkIdsJson).IsRequired();
            entity.Property(trace => trace.GeneratedAnswer).IsRequired();
            entity.HasIndex(trace => new { trace.CorpusId, trace.CreatedAt });
        });
    }
}
