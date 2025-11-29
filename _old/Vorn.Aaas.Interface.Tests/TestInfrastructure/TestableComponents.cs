using System.Reflection;
using Microsoft.AspNetCore.Components;
using Vorn.Aaas.Interface.Presentation.Pages.Account;

namespace Vorn.Aaas.Interface.Tests.TestInfrastructure;

internal class TestableLogout : Logout
{
#pragma warning disable BL0007
    [Parameter]
    public string? LogoutIdParameter
    {
        get => (string?)typeof(Logout)
            .GetProperty("logoutId", BindingFlags.Instance | BindingFlags.NonPublic)?
            .GetValue(this);
        set => typeof(Logout)
            .GetProperty("logoutId", BindingFlags.Instance | BindingFlags.NonPublic)?
            .SetValue(this, value);
    }
#pragma warning restore BL0007
}
