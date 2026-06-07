using System.Linq.Expressions;

namespace Vitarish.Communications.Infrastructure.Repositories;

[RegisterTransient]
internal class ReminderRepository(CommunicationsDbContext dbContext)
    : EntityRepository<Reminder, ReminderDescriptor, CommunicationsDbContext>(dbContext),
      IEntityRepository<Reminder, ReminderDescriptor>
{
    private static readonly EntitySearchTextNormalizer SearchTextNormalizer = new();
    private static readonly EntitySearchPatternFactory SearchPatternFactory = new(SearchTextNormalizer);
    private static readonly EntitySearchPredicateBuilder SearchPredicateBuilder = new(SearchTextNormalizer);

    protected override IQueryable<Reminder> Apply(IQueryable<Reminder> query, ReminderDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.SourceId is not null)
        {
            query = query.Where(r => r.SourceId == descriptor.SourceId);
        }

        if (descriptor.Kind is not null)
        {
            query = query.Where(r => r.Kind == descriptor.Kind);
        }

        if (!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = SearchPatternFactory.CreateSearchPatterns(descriptor.Search);

            if (patterns.Count > 0)
            {
                Expression<Func<Reminder, bool>> predicate = SearchPredicateBuilder.BuildSearchPredicate<Reminder>(
                    patterns,
                    r => r.Recipient,
                    r => r.Subject,
                    r => r.TextBody);

                query = query.Where(predicate);
            }
        }

        if (!string.IsNullOrWhiteSpace(descriptor.RecipientUserId))
        {
            query = query.Where(r => r.RecipientUserId == descriptor.RecipientUserId);
        }

        if (descriptor.RemindFrom is not null)
        {
            query = query.Where(r => r.RemindAt >= descriptor.RemindFrom);
        }

        if (descriptor.RemindTo is not null)
        {
            query = query.Where(r => r.RemindAt <= descriptor.RemindTo);
        }

        if (descriptor.SentFrom is not null)
        {
            query = query.Where(r => r.SentAt >= descriptor.SentFrom);
        }

        if (descriptor.SentTo is not null)
        {
            query = query.Where(r => r.SentAt <= descriptor.SentTo);
        }

        if (descriptor.IsSent is not null)
        {
            query = query.Where(r => r.SentAt != null == descriptor.IsSent);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception) => false;
}
