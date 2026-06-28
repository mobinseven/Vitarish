using System.Globalization;
using Vitarish.Modules.Base.Interface.Common;

namespace Vitarish.Main.Components;

public class PageBase : CommonBase
{
    [Inject] protected IHttpClientFactory HttpClientFactory { get; set; } = default!;
    [Inject] protected IHttpContextAccessor HttpContextAccessor { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (RequestContext.UserId is null)
        {
            return;
        }

        try
        {
            HttpClient client = HttpClientFactory.CreateClient();
            string? culture = await client.GetStringAsync($"/api/modules/users/{RequestContext.UserId}/culture");
            if (!string.IsNullOrWhiteSpace(culture))
            {
                CultureInfo cultureInfo = new(culture);
                CultureInfo.DefaultThreadCurrentCulture = cultureInfo;
                CultureInfo.DefaultThreadCurrentUICulture = cultureInfo;
            }
        }
        catch
        {
            // Culture resolution is best-effort
        }
    }
}
