using Microsoft.AspNetCore.Components;
using Vitarish.Web.Admins.Client.Models;

namespace Vitarish.Web.Admins.Client.Components;

public partial class AccessUsersMatrix : ComponentBase
{
    [Parameter] public IReadOnlyList<AdminUserModel>? Rows { get; set; }

    private List<string> roles = [];

    protected override void OnParametersSet()
    {
        if(Rows is null)
        {
            roles = [];
            return;
        }

        roles = Rows
            .SelectMany(r => r.Access.Readers.Concat(r.Access.Writers))
            .Where(r => !string.IsNullOrWhiteSpace(r))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order()
            .ToList();
    }
}
