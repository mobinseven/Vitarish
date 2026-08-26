namespace Vitarish.Sea.Application.Abstractions;

public interface IProjectObjectMaterializedViewRefresher
{
    string ObjectType { get; }

    Task RefreshAsync(string tenantId, CancellationToken cancellationToken);
}
