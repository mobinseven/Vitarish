namespace Vorn.Aaas.Client.Api.Options;

public class CacheOptions
{
    public string ConnectionString { get; set; }
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = "CacheTable";
}
