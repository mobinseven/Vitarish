namespace Authentication.Tests.Infrastructure;

/// <summary>
/// The PostgreSQL-backed tests share one database and reset its tables, so they run one class at a time.
/// </summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class AuthenticationPostgresCollection
{
    public const string Name = "Authentication PostgreSQL";
}
