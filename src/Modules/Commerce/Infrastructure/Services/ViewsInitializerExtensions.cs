using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Logging;
using System.Globalization;
using System.Text;

namespace Vitarish.Commerce.Infrastructure.Services;

public static class ViewsInitializerExtensions
{
    public static async Task InitializeViews(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();

        CommerceDbContext db = scope.ServiceProvider.GetRequiredService<CommerceDbContext>();
        ILogger<CommerceDbContext> logger = scope.ServiceProvider.GetRequiredService<ILogger<CommerceDbContext>>();

        object? resource = Resources.ResourceManager.GetObject("Views", CultureInfo.InvariantCulture);
        if (resource is null)
        {
            logger.LogInformation("No SQL resource found: {ResourceName}:{Key}.", "Resources", "Views");
            return;
        }

        string sql = resource switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),

            UnmanagedMemoryStream ums => await new StreamReader(
                    ums, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, leaveOpen: false
                ).ReadToEndAsync(),

            string s => s,

            _ => throw new InvalidOperationException(
                $"Resource 'Views' was type '{resource.GetType().FullName}', expected byte[] or stream."
            )
        };

        if (string.IsNullOrWhiteSpace(sql))
        {
            logger.LogInformation("SQL resource {ResourceName}:{Key} was empty.", "Resources", "Views");
            return;
        }

        logger.LogInformation("Executing SQL script from resource {ResourceName}:{Key}.", "Resources", "Views");

        try
        {
            _ = await db.Database.ExecuteSqlRawAsync(sql);
            logger.LogInformation("Commerce views SQL executed successfully.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Failed to execute Views SQL from resource {ResourceName}:{Key}.", "Resources", "Views");
            throw;
        }
    }
}
