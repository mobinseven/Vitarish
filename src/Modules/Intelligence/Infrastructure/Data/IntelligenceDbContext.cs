using Finbuckle.MultiTenant;
using Finbuckle.MultiTenant.Abstractions;
using Finbuckle.MultiTenant.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Vitarish.Entities.Server.Infrastructure;
using Vitarish.Requests.Application.Abstractions;

namespace Vitarish.Intelligence.Infrastructure.Data;

public sealed class IntelligenceDbContext : DbContext, IMultiTenantDbContext
{
    private readonly IEnumerable<IInterceptor> _interceptors;
    private readonly IRequestContext _requestContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public IntelligenceDbContext(
        DbContextOptions<IntelligenceDbContext> options,
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
            string? tenantId = _requestContext.TenantId ?? _httpContextAccessor.HttpContext?.GetTenantId();
            return tenantId is null ? null : new TenantInfo { Id = tenantId, Identifier = tenantId, Name = tenantId };
        }
    }

    public TenantMismatchMode TenantMismatchMode { get; set; } = TenantMismatchMode.Throw;
    public TenantNotSetMode TenantNotSetMode { get; set; } = TenantNotSetMode.Throw;
    public DbSet<AgentConversationEntity> AgentConversations => Set<AgentConversationEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder) =>
        _ = optionsBuilder.AddInterceptors(_interceptors);

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
        modelBuilder.HasDefaultSchema(nameof(Intelligence));
        _ = modelBuilder.ConfigureMultiTenant();
        modelBuilder.Entity<AgentConversationEntity>(entity =>
        {
            entity.ToTable("AgentConversations");
            entity.HasKey(conversation => conversation.Id);
            entity.Property(conversation => conversation.TenantId).HasMaxLength(128).IsRequired();
            entity.Property(conversation => conversation.CreatingUserId).HasMaxLength(128).IsRequired();
            entity.Property(conversation => conversation.AgentKey).HasMaxLength(128).IsRequired();
            entity.Property(conversation => conversation.SubjectKind).HasMaxLength(64).IsRequired();
            entity.Property(conversation => conversation.SubjectId).HasMaxLength(128).IsRequired();
            entity.Property(conversation => conversation.ProtectedHistory);
            entity.Property(conversation => conversation.ProtectedPresentationTranscript);
            entity.Property(conversation => conversation.Title).HasMaxLength(120);
            entity.Property(conversation => conversation.LatestTextPreview).HasMaxLength(240);
            entity.Property(conversation => conversation.HistoryFormat).HasMaxLength(128).IsRequired();
            entity.Property(conversation => conversation.Revision).IsConcurrencyToken();
            entity.HasIndex(conversation => new
            {
                conversation.TenantId,
                conversation.CreatingUserId,
                conversation.AgentKey,
                conversation.SubjectKind,
                conversation.SubjectId
            });
            _ = entity.IsMultiTenant();
        });
    }
}
