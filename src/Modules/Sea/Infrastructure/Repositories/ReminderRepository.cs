using System.Linq.Expressions;
using Vitarish.Sea.Infrastructure.Data;
using Vitarish.Sea.Infrastructure.Helpers;

namespace Vitarish.Sea.Infrastructure.Repositories;

[RegisterTransient]
internal class ReminderRepository(SeaDbContext dbContext)
    : EntityRepository<Reminder, ReminderDescriptor, SeaDbContext>(dbContext),
      IEntityRepository<Reminder, ReminderDescriptor>
{
    protected override IQueryable<Reminder> Apply(IQueryable<Reminder> query, ReminderDescriptor descriptor)
    {
        query = base.Apply(query, descriptor);

        if (descriptor.EventId is not null)
        {
            query = query.Where(r => r.EventId == descriptor.EventId);
        }

        if(descriptor.Kind is not null)
        {
            query = query.Where(r => r.Kind == descriptor.Kind);
        }

        if(!string.IsNullOrWhiteSpace(descriptor.Search))
        {
            IReadOnlyCollection<string> patterns = EntitySearchHelper.CreateSearchPatterns(descriptor.Search);

            if(patterns.Count > 0)
            {
                Expression<Func<Reminder, bool>> predicate = EntitySearchHelper.BuildSearchPredicate<Reminder>(
                    patterns,
                    r => r.Recipient,
                    r => r.Subject,
                    r => r.TextBody);

                query = query.Where(predicate);
            }
        }

        if(!string.IsNullOrWhiteSpace(descriptor.RecipientUserId))
        {
            query = query.Where(r => r.RecipientUserId == descriptor.RecipientUserId);
        }

        if(descriptor.RemindFrom is not null)
        {
            query = query.Where(r => r.RemindAt >= descriptor.RemindFrom);
        }

        if(descriptor.RemindTo is not null)
        {
            query = query.Where(r => r.RemindAt <= descriptor.RemindTo);
        }

        if(descriptor.SentFrom is not null)
        {
            query = query.Where(r => r.SentAt >= descriptor.SentFrom);
        }

        if(descriptor.SentTo is not null)
        {
            query = query.Where(r => r.SentAt <= descriptor.SentTo);
        }

        if(descriptor.IsSent is not null)
        {
            query = query.Where(r => r.SentAt != null == descriptor.IsSent);
        }

        return query;
    }

    protected override bool IsDuplicateKeyException(DbUpdateException exception)
    {
        return false;
    }
}

