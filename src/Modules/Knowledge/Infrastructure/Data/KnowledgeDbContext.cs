using Microsoft.EntityFrameworkCore;
using Vitarish.Knowledge.Domain.Entities;

namespace Vitarish.Knowledge.Infrastructure.Data;

public sealed class KnowledgeDbContext(DbContextOptions<KnowledgeDbContext> options) : DbContext(options)
{
    public DbSet<KnowledgeCorpusEntity> Corpora => Set<KnowledgeCorpusEntity>();
    public DbSet<KnowledgeSourceEntity> Sources => Set<KnowledgeSourceEntity>();
    public DbSet<KnowledgeDocumentEntity> Documents => Set<KnowledgeDocumentEntity>();
    public DbSet<KnowledgeChunkEntity> Chunks => Set<KnowledgeChunkEntity>();
    public DbSet<KnowledgeQuestionEntity> Questions => Set<KnowledgeQuestionEntity>();
    public DbSet<KnowledgeAskTraceEntity> AskTraces => Set<KnowledgeAskTraceEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(nameof(Knowledge));

        modelBuilder.Entity<KnowledgeCorpusEntity>(entity =>
        {
            entity.ToTable("KnowledgeCorpora");
            entity.HasKey(corpus => corpus.Id);
            entity.Property(corpus => corpus.Id).HasMaxLength(128);
            entity.Property(corpus => corpus.Name).HasMaxLength(512).IsRequired();
            entity.Property(corpus => corpus.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(corpus => corpus.Description).HasMaxLength(2048);
            entity.HasIndex(corpus => corpus.TenantId);
        });

        modelBuilder.Entity<KnowledgeSourceEntity>(entity =>
        {
            entity.ToTable("KnowledgeSources");
            entity.HasKey(source => source.Id);
            entity.Property(source => source.Id).HasMaxLength(64);
            entity.Property(source => source.CorpusId).HasMaxLength(128).IsRequired();
            entity.Property(source => source.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(source => source.Name).HasMaxLength(512).IsRequired();
            entity.Property(source => source.SourceType).HasMaxLength(64).IsRequired();
            entity.Property(source => source.OriginalLocation).HasMaxLength(2048);
            entity.Property(source => source.Version).HasMaxLength(128);
            entity.Property(source => source.Checksum).HasMaxLength(256);
            entity.HasIndex(source => source.CorpusId);
            entity.HasOne<KnowledgeCorpusEntity>()
                .WithMany()
                .HasForeignKey(source => source.CorpusId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeDocumentEntity>(entity =>
        {
            entity.ToTable("KnowledgeDocuments");
            entity.HasKey(document => document.Id);
            entity.Property(document => document.Id).HasMaxLength(64);
            entity.Property(document => document.CorpusId).HasMaxLength(128).IsRequired();
            entity.Property(document => document.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(document => document.SourceId).HasMaxLength(64).IsRequired();
            entity.Property(document => document.Title).HasMaxLength(512).IsRequired();
            entity.Property(document => document.Text).IsRequired();
            entity.Property(document => document.Product).HasMaxLength(128);
            entity.Property(document => document.Version).HasMaxLength(128);
            entity.Property(document => document.Language).HasMaxLength(32);
            entity.HasIndex(document => document.CorpusId);
            entity.HasOne<KnowledgeSourceEntity>()
                .WithMany()
                .HasForeignKey(document => document.SourceId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeChunkEntity>(entity =>
        {
            entity.ToTable("KnowledgeChunks");
            entity.HasKey(chunk => chunk.Id);
            entity.Property(chunk => chunk.Id).HasMaxLength(64);
            entity.Property(chunk => chunk.CorpusId).HasMaxLength(128).IsRequired();
            entity.Property(chunk => chunk.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(chunk => chunk.SourceId).HasMaxLength(64).IsRequired();
            entity.Property(chunk => chunk.DocumentId).HasMaxLength(64).IsRequired();
            entity.Property(chunk => chunk.Text).IsRequired();
            entity.Property(chunk => chunk.Title).HasMaxLength(512);
            entity.Property(chunk => chunk.HeadingPath).HasMaxLength(2048);
            entity.HasIndex(chunk => new { chunk.CorpusId, chunk.SourceId, chunk.DocumentId, chunk.Order });
            entity.HasOne<KnowledgeDocumentEntity>()
                .WithMany()
                .HasForeignKey(chunk => chunk.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
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
            entity.HasOne<KnowledgeCorpusEntity>()
                .WithMany()
                .HasForeignKey(question => question.CorpusId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<KnowledgeAskTraceEntity>(entity =>
        {
            entity.ToTable("KnowledgeAskTraces");
            entity.HasKey(trace => trace.Id);
            entity.Property(trace => trace.Id).HasMaxLength(64);
            entity.Property(trace => trace.CorpusId).HasMaxLength(128).IsRequired();
            entity.Property(trace => trace.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(trace => trace.Question).HasMaxLength(2048).IsRequired();
            entity.Property(trace => trace.RetrievedChunkIdsJson).IsRequired();
            entity.Property(trace => trace.GeneratedAnswer).IsRequired();
            entity.Property(trace => trace.MissingInformation).HasMaxLength(4096);
            entity.Property(trace => trace.CitationsJson).IsRequired();
            entity.Property(trace => trace.TraceKind).HasMaxLength(64);
            entity.HasIndex(trace => new { trace.TenantId, trace.CorpusId, trace.CreatedAt });
            entity.HasOne<KnowledgeCorpusEntity>()
                .WithMany()
                .HasForeignKey(trace => trace.CorpusId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
