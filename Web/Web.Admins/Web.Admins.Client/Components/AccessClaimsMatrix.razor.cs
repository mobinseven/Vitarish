using Microsoft.AspNetCore.Components;
using Vitarish.Web.Admins.Client.Models;

namespace Vitarish.Web.Admins.Client.Components;

public partial class AccessClaimsMatrix : ComponentBase
{
    [Parameter] public IReadOnlyList<AccessClaimModel>? Claims { get; set; }

    private List<string> roles = [];

    protected override void OnParametersSet()
    {
        UpdateRoles();
    }

    private void UpdateRoles()
    {
        if(Claims is null)
        {
            roles = [];
            return;
        }

        roles = Claims
            .SelectMany(r => r.Access.Readers.Concat(r.Access.Writers))
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();
    }
}
