using Accounts.Interface.Tests.TestInfrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Vitarish.Accounts.Interface.Components.Admin;

namespace Accounts.Interface.Tests.Admin;

public class RoleEditorTests
{
    [Fact]
    public async Task RoleEditor_SubmitValid_SavesRole()
    {
        await using InterfaceTestContext context = new();
        string? readValue = null;

        IRenderedComponent<RoleEditor> cut = context.Render<RoleEditor>(parameters => parameters
            .Add(p => p.Roles, new[] { "Admin" })
            .Add(p => p.ReadChanged, EventCallback.Factory.Create<string?>(this, value => readValue = value)));

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "ToggleRead", typeof(RoleEditor), "Admin", true));

        Assert.Equal("Admin", readValue);
    }

    [Fact]
    public async Task RoleEditor_SubmitInvalid_ShowsErrors()
    {
        await using InterfaceTestContext context = new();

        IRenderedComponent<RoleEditor> cut = context.Render<RoleEditor>(parameters => parameters
            .Add(p => p.Roles, Array.Empty<string>()));

        Assert.Contains("NoRoles", cut.Markup, StringComparison.Ordinal);
    }
}
