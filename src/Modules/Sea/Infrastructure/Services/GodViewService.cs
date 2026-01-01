using System.Globalization;

namespace Vitarish.Sea.Infrastructure.Services;

[RegisterTransient]
internal sealed class GodViewService(SeaDbContext dbContext) : IGodViewService
{
    public async Task<GodViewPage> GetProjectsAsync(GodViewPageRequest request, CancellationToken cancellationToken)
    {
        IQueryable<GodViewRowDto> query = dbContext.Projects
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Select(project => new
            {
                project.Name,
                project.Description,
                project.CreatedAt,
                project.CreatedBy,
                project.UpdatedAt,
                project.UpdatedBy,
                project.DeletedAt,
                project.DeletedBy,
                BaseLastActivity = project.UpdatedAt.HasValue && project.UpdatedAt.Value > project.CreatedAt
                    ? project.UpdatedAt.Value
                    : project.CreatedAt
            })
            .Select(project => new GodViewRowDto(
                project.Name,
                null,
                project.Description,
                project.CreatedAt,
                project.CreatedBy,
                project.UpdatedAt,
                project.UpdatedBy,
                project.DeletedAt,
                project.DeletedBy,
                project.DeletedAt.HasValue && project.DeletedAt.Value > project.BaseLastActivity
                    ? project.DeletedAt.Value
                    : project.BaseLastActivity));

        return await ExecuteAsync(query, request, cancellationToken);
    }

    public async Task<GodViewPage> GetProceduresAsync(GodViewPageRequest request, CancellationToken cancellationToken)
    {
        IQueryable<GodViewRowDto> query = dbContext.Procedures
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Join(
                dbContext.Projects.IgnoreQueryFilters().AsNoTracking(),
                procedure => procedure.ProjectId,
                project => project.Id,
                (procedure, project) => new
                {
                    procedure.Name,
                    ProjectName = project.Name,
                    procedure.CreatedAt,
                    procedure.CreatedBy,
                    procedure.UpdatedAt,
                    procedure.UpdatedBy,
                    procedure.DeletedAt,
                    procedure.DeletedBy,
                    BaseLastActivity = procedure.UpdatedAt.HasValue && procedure.UpdatedAt.Value > procedure.CreatedAt
                        ? procedure.UpdatedAt.Value
                        : procedure.CreatedAt
                })
            .Select(item => new GodViewRowDto(
                item.Name,
                item.ProjectName,
                null,
                item.CreatedAt,
                item.CreatedBy,
                item.UpdatedAt,
                item.UpdatedBy,
                item.DeletedAt,
                item.DeletedBy,
                item.DeletedAt.HasValue && item.DeletedAt.Value > item.BaseLastActivity
                    ? item.DeletedAt.Value
                    : item.BaseLastActivity));

        return await ExecuteAsync(query, request, cancellationToken);
    }

    public async Task<GodViewPage> GetEventsAsync(GodViewPageRequest request, CancellationToken cancellationToken)
    {
        IQueryable<GodViewRowDto> query = dbContext.Events
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Join(
                dbContext.Procedures.IgnoreQueryFilters().AsNoTracking(),
                @event => @event.ProcedureId,
                procedure => procedure.Id,
                (@event, procedure) => new { @event, procedure })
            .Join(
                dbContext.Projects.IgnoreQueryFilters().AsNoTracking(),
                pair => pair.@event.ProjectId,
                project => project.Id,
                (pair, project) => new
                {
                    pair.@event.Title,
                    pair.@event.Description,
                    ProjectName = project.Name,
                    ProcedureName = pair.procedure.Name,
                    pair.@event.CreatedAt,
                    pair.@event.CreatedBy,
                    pair.@event.UpdatedAt,
                    pair.@event.UpdatedBy,
                    pair.@event.DeletedAt,
                    pair.@event.DeletedBy,
                    pair.@event.CommittedAt,
                    BaseLastActivity = pair.@event.UpdatedAt.HasValue && pair.@event.UpdatedAt.Value > pair.@event.CreatedAt
                        ? pair.@event.UpdatedAt.Value
                        : pair.@event.CreatedAt
                })
            .Select(item => new GodViewRowDto(
                item.Title,
                item.ProjectName + " / " + item.ProcedureName,
                item.Description,
                item.CreatedAt,
                item.CreatedBy,
                item.UpdatedAt,
                item.UpdatedBy,
                item.DeletedAt,
                item.DeletedBy,
                item.CommittedAt.HasValue && item.CommittedAt.Value >
                    (item.DeletedAt.HasValue && item.DeletedAt.Value > item.BaseLastActivity
                        ? item.DeletedAt.Value
                        : item.BaseLastActivity)
                    ? item.CommittedAt.Value
                    : (item.DeletedAt.HasValue && item.DeletedAt.Value > item.BaseLastActivity
                        ? item.DeletedAt.Value
                        : item.BaseLastActivity)));

        return await ExecuteAsync(query, request, cancellationToken);
    }

    public async Task<GodViewPage> GetAttachmentsAsync(GodViewPageRequest request, CancellationToken cancellationToken)
    {
        GodViewPageRequest normalized = GodViewPageRequest.Create(request.Page, request.PageSize, request.SearchTerms);

        IQueryable<AttachmentRow> query = dbContext.Attachments
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Join(
                dbContext.Events.IgnoreQueryFilters().AsNoTracking(),
                attachment => attachment.EventId,
                @event => @event.Id,
                (attachment, @event) => new { attachment, @event })
            .Join(
                dbContext.Procedures.IgnoreQueryFilters().AsNoTracking(),
                pair => pair.@event.ProcedureId,
                procedure => procedure.Id,
                (pair, procedure) => new { pair.attachment, pair.@event, procedure })
            .Join(
                dbContext.Projects.IgnoreQueryFilters().AsNoTracking(),
                pair => pair.@event.ProjectId,
                project => project.Id,
                (pair, project) => new
                {
                    pair.attachment.ContentType,
                    ProjectName = project.Name,
                    ProcedureName = pair.procedure.Name,
                    EventTitle = pair.@event.Title,
                    pair.attachment.Size,
                    pair.attachment.CreatedAt,
                    pair.attachment.CreatedBy,
                    pair.attachment.UpdatedAt,
                    pair.attachment.UpdatedBy,
                    pair.attachment.DeletedAt,
                    pair.attachment.DeletedBy,
                    BaseLastActivity = pair.attachment.UpdatedAt.HasValue && pair.attachment.UpdatedAt.Value > pair.attachment.CreatedAt
                        ? pair.attachment.UpdatedAt.Value
                        : pair.attachment.CreatedAt
                })
            .Select(item => new AttachmentRow(
                item.ContentType,
                item.ProjectName,
                item.ProcedureName,
                item.EventTitle,
                item.Size,
                item.CreatedAt,
                item.CreatedBy,
                item.UpdatedAt,
                item.UpdatedBy,
                item.DeletedAt,
                item.DeletedBy,
                item.DeletedAt.HasValue && item.DeletedAt.Value > item.BaseLastActivity
                    ? item.DeletedAt.Value
                    : item.BaseLastActivity));

        query = ApplySearch(query, normalized.SearchTerms);

        int totalCount = await query.CountAsync(cancellationToken);

        int skip = normalized.Page * normalized.PageSize;
        List<AttachmentRow> rows = await query
            //.OrderByDescending(row => row.LastActivityAt)
            .Skip(skip)
            .Take(normalized.PageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyList<GodViewRowDto> items = rows
            .Select(row => new GodViewRowDto(
                row.ContentType,
                row.ProjectName + " / " + row.ProcedureName + " / " + row.EventTitle,
                FormatSize(row.Size),
                row.CreatedAt,
                row.CreatedBy,
                row.UpdatedAt,
                row.UpdatedBy,
                row.DeletedAt,
                row.DeletedBy,
                row.LastActivityAt))
            .ToList();

        return new GodViewPage(items, totalCount);
    }

    public async Task<GodViewPage> GetRemindersAsync(GodViewPageRequest request, CancellationToken cancellationToken)
    {
        IQueryable<GodViewRowDto> query = dbContext.Reminders
            .IgnoreQueryFilters()
            .AsNoTracking()
            .Join(
                dbContext.Events.IgnoreQueryFilters().AsNoTracking(),
                reminder => reminder.EventId,
                @event => @event.Id,
                (reminder, @event) => new { reminder, @event })
            .Join(
                dbContext.Procedures.IgnoreQueryFilters().AsNoTracking(),
                pair => pair.@event.ProcedureId,
                procedure => procedure.Id,
                (pair, procedure) => new { pair.reminder, pair.@event, procedure })
            .Join(
                dbContext.Projects.IgnoreQueryFilters().AsNoTracking(),
                pair => pair.@event.ProjectId,
                project => project.Id,
                (pair, project) => new
                {
                    pair.reminder.Subject,
                    ProjectName = project.Name,
                    ProcedureName = pair.procedure.Name,
                    EventTitle = pair.@event.Title,
                    pair.reminder.Recipient,
                    pair.reminder.CreatedAt,
                    pair.reminder.CreatedBy,
                    pair.reminder.UpdatedAt,
                    pair.reminder.UpdatedBy,
                    pair.reminder.DeletedAt,
                    pair.reminder.DeletedBy,
                    pair.reminder.SentAt,
                    BaseLastActivity = pair.reminder.UpdatedAt.HasValue && pair.reminder.UpdatedAt.Value > pair.reminder.CreatedAt
                        ? pair.reminder.UpdatedAt.Value
                        : pair.reminder.CreatedAt
                })
            .Select(item => new GodViewRowDto(
                item.Subject,
                item.ProjectName + " / " + item.ProcedureName + " / " + item.EventTitle,
                item.Recipient,
                item.CreatedAt,
                item.CreatedBy,
                item.UpdatedAt,
                item.UpdatedBy,
                item.DeletedAt,
                item.DeletedBy,
                item.SentAt.HasValue && item.SentAt.Value >
                    (item.DeletedAt.HasValue && item.DeletedAt.Value > item.BaseLastActivity
                        ? item.DeletedAt.Value
                        : item.BaseLastActivity)
                    ? item.SentAt.Value
                    : (item.DeletedAt.HasValue && item.DeletedAt.Value > item.BaseLastActivity
                        ? item.DeletedAt.Value
                        : item.BaseLastActivity)));

        return await ExecuteAsync(query, request, cancellationToken);
    }

    private static async Task<GodViewPage> ExecuteAsync(
        IQueryable<GodViewRowDto> source,
        GodViewPageRequest request,
        CancellationToken cancellationToken)
    {
        GodViewPageRequest normalized = GodViewPageRequest.Create(request.Page, request.PageSize, request.SearchTerms);
        IQueryable<GodViewRowDto> filtered = ApplySearch(source, normalized.SearchTerms);

        int totalCount = await filtered.CountAsync(cancellationToken);

        int skip = normalized.Page * normalized.PageSize;
        List<GodViewRowDto> items = await filtered
            //.OrderByDescending(row => row.LastActivityAt)
            .Skip(skip)
            .Take(normalized.PageSize)
            .ToListAsync(cancellationToken);

        return new GodViewPage(items, totalCount);
    }

    private static IQueryable<GodViewRowDto> ApplySearch(IQueryable<GodViewRowDto> query, IReadOnlyList<string> terms)
    {
        if (terms.Count == 0)
        {
            return query;
        }

        foreach (string term in terms)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                continue;
            }

            string normalizedTerm = term.ToLowerInvariant();
            query = query.Where(row =>
                (row.Primary != null && row.Primary.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || (row.Context != null && row.Context.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || (row.Details != null && row.Details.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || (row.CreatedBy != null && row.CreatedBy.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || (row.UpdatedBy != null && row.UpdatedBy.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || (row.DeletedBy != null && row.DeletedBy.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase)));
        }

        return query;
    }

    private static IQueryable<AttachmentRow> ApplySearch(IQueryable<AttachmentRow> query, IReadOnlyList<string> terms)
    {
        if (terms.Count == 0)
        {
            return query;
        }

        foreach (string term in terms)
        {
            if (string.IsNullOrWhiteSpace(term))
            {
                continue;
            }

            string normalizedTerm = term.ToLowerInvariant();
            query = query.Where(row =>
                (row.ContentType != null && row.ContentType.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || (row.ProjectName != null && row.ProjectName.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || (row.ProcedureName != null && row.ProcedureName.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || (row.EventTitle != null && row.EventTitle.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || (row.CreatedBy != null && row.CreatedBy.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || (row.UpdatedBy != null && row.UpdatedBy.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || (row.DeletedBy != null && row.DeletedBy.Contains(normalizedTerm, StringComparison.CurrentCultureIgnoreCase))
                || row.Size.ToString().Contains(normalizedTerm));
        }

        return query;
    }

    private static string FormatSize(long size)
    {
        if (size < 1024)
        {
            return string.Format(CultureInfo.CurrentCulture, "{0} B", size);
        }

        double value = size;
        string[] suffixes = ["KB", "MB", "GB", "TB"];
        int suffixIndex = 0;

        while (value >= 1024 && suffixIndex < suffixes.Length - 1)
        {
            value /= 1024;
            suffixIndex++;
        }

        return string.Format(CultureInfo.CurrentCulture, "{0:F2} {1}", value, suffixes[suffixIndex]);
    }

    private sealed record AttachmentRow(
        string ContentType,
        string ProjectName,
        string ProcedureName,
        string EventTitle,
        long Size,
        DateTimeOffset CreatedAt,
        string? CreatedBy,
        DateTimeOffset? UpdatedAt,
        string? UpdatedBy,
        DateTimeOffset? DeletedAt,
        string? DeletedBy,
        DateTimeOffset LastActivityAt);
}
