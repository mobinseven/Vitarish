using Microsoft.EntityFrameworkCore;
using Vitarish.Modules.Base.Core.Abstractions;
using Vitarish.Platform.Infrastructure.Data;

namespace Vitarish.Platform.Infrastructure.Services;

public sealed class ValidLocaleProvider(PlatformDbContext dbContext) : IValidLocaleProvider
{
    public async Task<IReadOnlyList<string>> GetValidLocalesAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.LocaleNotificationRecords
            .AsNoTracking()
            .Select(e => e.NormalizedLocale)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> IsValidLocaleAsync(string locale, CancellationToken cancellationToken = default)
    {
        return await dbContext.LocaleNotificationRecords
            .AsNoTracking()
            .AnyAsync(e => e.NormalizedLocale == locale, cancellationToken);
    }
}
