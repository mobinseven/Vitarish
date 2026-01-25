using System.Globalization;
using Vitarish.Accounts.Application.Abstractions;
using Vitarish.Interfaces.Base.Common;

namespace Vitarish.Main.Components;

public class PageBase : CommonBase
{
    [Inject] protected UserProfileCultureInfoProvider CultureProvider { get; set; } = default!;
    [Inject] protected IUserService UserService { get; set; } = default!;

    protected override async Task OnInitializedAsync()
    {
        await base.OnInitializedAsync();
        if (RequestContext.UserId is null)
        {
            return;
        }

        var cookieCulture = await CultureProvider.GetCultureFromCookieAsync();
        if (cookieCulture is not null)
        {
            if (await CultureProvider.TryApplyCultureAsync(cookieCulture.Name))
            {
                return;
            }
        }

        string? existingCulture = await UserService.GetCultureAsync(RequestContext.UserId);
        if (!string.IsNullOrWhiteSpace(existingCulture))
        {
            if (await CultureProvider.TryApplyCultureAsync(existingCulture))
            {
                NavigationManager.Refresh(forceReload: true);
                return;
            }
        }

    }
}
