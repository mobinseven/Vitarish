using System.Reflection;
using Microsoft.AspNetCore.Components;
using Vitarish.Interface.Presentation.Pages.Account;

namespace Vitarish.Interface.Tests.TestInfrastructure;

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
