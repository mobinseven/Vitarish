using System.Runtime.CompilerServices;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage;
using Vitarish.Users.Domain.Entities;

namespace Vitarish.Users.Infrastructure.Data;

/// <summary>PostgreSQL row locking; the in-memory host serializes with the same scoped lifetime.</summary>
internal static class UserAccountWriteLock
{
    private static readonly SemaphoreSlim InMemoryGate = new(1, 1);

    internal static async Task<IAsyncDisposable?> AcquireInMemoryAsync(UsersDbContext db, CancellationToken ct)
    {
        if (db.Database.IsRelational()) return null;
        await InMemoryGate.WaitAsync(ct);
        return new ReleaseGate();
    }

    internal static Task<User?> ReadForUpdateAsync(UsersDbContext db, string id, CancellationToken ct)
    {
        if (!db.Database.IsRelational())
            return db.Users.IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(user => user.Id == id, ct);

        // Identifiers come from the EF model; the caller's ID remains a SQL parameter.
        IEntityType entity = db.Model.FindEntityType(typeof(User))!;
        StoreObjectIdentifier table = StoreObjectIdentifier.Table(entity.GetTableName()!, entity.GetSchema());
        ISqlGenerationHelper sql = db.GetService<ISqlGenerationHelper>();
        string tableName = sql.DelimitIdentifier(table.Name, table.Schema);
        string idColumn = sql.DelimitIdentifier(entity.FindProperty(nameof(User.Id))!.GetColumnName(table)!);
        FormattableString query = FormattableStringFactory.Create($"SELECT * FROM {tableName} WHERE {idColumn} = {{0}} FOR UPDATE", id);
        return db.Users.FromSqlInterpolated(query)
            .IgnoreQueryFilters().AsNoTracking().SingleOrDefaultAsync(ct);
    }

    private sealed class ReleaseGate : IAsyncDisposable
    {
        public ValueTask DisposeAsync() { InMemoryGate.Release(); return ValueTask.CompletedTask; }
    }
}
