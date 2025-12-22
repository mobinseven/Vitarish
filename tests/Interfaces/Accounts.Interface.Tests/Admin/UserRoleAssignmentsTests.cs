using System.Collections;
using Accounts.Interface.Tests.TestInfrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using MudBlazor;
using Vitarish.Accounts.Interface.Components.Admin;
using Vitarish.Users.Domain.Entities;

namespace Accounts.Interface.Tests.Admin;

public class UserRoleAssignmentsTests
{
    [Fact]
    public async Task RendersUsersAndRolesFromServices()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "1", UserName = "alice" };

        _ = context.Mocks.Users
            .Setup(service => service.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });
        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(new List<IdentityRole> { new("Admin") }.AsQueryable());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });

        IRenderedComponent<UserRoleAssignments> cut = RenderWithProviders(context);

        cut.WaitForAssertion(() => cut.Markup.Contains("alice", StringComparison.Ordinal));
        cut.Markup.Should().Contain("Admin");
    }

    [Fact]
    public async Task AssignRole_AddsRoleAndUpdatesEntry()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "2", UserName = "bob" };

        _ = context.Mocks.Users
            .Setup(service => service.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });
        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(new List<IdentityRole> { new("Editor") }.AsQueryable());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.AddToRoleAsync(user, "Editor"))
            .ReturnsAsync(IdentityResult.Success);

        IRenderedComponent<UserRoleAssignments> cut = RenderWithProviders(context);
        cut.WaitForAssertion(() => cut.Markup.Contains("bob", StringComparison.Ordinal));

        object? entryList = ComponentTestHelper.GetFieldValue<object>(cut.Instance, "users", typeof(UserRoleAssignments));
        List<object> entries = ((IEnumerable)entryList!).Cast<object>().ToList();
        object entry = Assert.Single(entries);
        entry.GetType().GetProperty("SelectedRole")!.SetValue(entry, "Editor");

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "AssignRoleAsync", typeof(UserRoleAssignments), entry));

        context.Mocks.UserManager.Verify(manager => manager.AddToRoleAsync(user, "Editor"), Times.Once);
        List<string>? assignedRoles = ComponentTestHelper.GetPropertyValue<List<string>>(entry, "AssignedRoles");
        assignedRoles.Should().Contain("Editor");
    }

    [Fact]
    public async Task RemoveRole_RemovesRoleFromUser()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "3", UserName = "carol" };

        _ = context.Mocks.Users
            .Setup(service => service.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });
        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(new List<IdentityRole> { new("Admin") }.AsQueryable());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "Admin" });
        _ = context.Mocks.UserManager
            .Setup(manager => manager.RemoveFromRoleAsync(user, "Admin"))
            .ReturnsAsync(IdentityResult.Success);

        IRenderedComponent<UserRoleAssignments> cut = RenderWithProviders(context);
        cut.WaitForAssertion(() => cut.Markup.Contains("carol", StringComparison.Ordinal));

        object? entryList = ComponentTestHelper.GetFieldValue<object>(cut.Instance, "users", typeof(UserRoleAssignments));
        List<object> entries = ((IEnumerable)entryList!).Cast<object>().ToList();
        object entry = Assert.Single(entries);

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "RemoveRoleAsync", typeof(UserRoleAssignments), entry, "Admin"));

        context.Mocks.UserManager.Verify(manager => manager.RemoveFromRoleAsync(user, "Admin"), Times.Once);
        List<string>? assignedRoles = ComponentTestHelper.GetPropertyValue<List<string>>(entry, "AssignedRoles");
        assignedRoles.Should().BeEmpty();
    }

    private static IRenderedComponent<UserRoleAssignments> RenderWithProviders(InterfaceTestContext context)
    {
        var renderResult = context.Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<UserRoleAssignments>(1);
            builder.CloseComponent();
        });

        return renderResult.FindComponent<UserRoleAssignments>();
    }
}
