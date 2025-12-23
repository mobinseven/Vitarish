using System;
using System.Collections;
using Accounts.Interface.Tests.TestInfrastructure;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Identity;
using MudBlazor;
using Vitarish.Accounts.Interface.Components.Admin;
using Vitarish.Parties.Domain.Entities;
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
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tenant>
            {
                new()
                {
                    Id = Guid.Parse("8aa2f5ae-6e6b-4f5b-b18a-d179d38cc0f3"),
                    Name = "Primary",
                    ClientId = "primary-client"
                }
            });
        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(new List<IdentityRole> { new("Admin") }.AsQueryable());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "8aa2f5ae-6e6b-4f5b-b18a-d179d38cc0f3:Admin" });

        IRenderedComponent<UserRoleAssignments> cut = RenderWithProviders(context);

        cut.WaitForAssertion(() => cut.Markup.Contains("alice", StringComparison.Ordinal));
        cut.Markup.Should().Contain("8aa2f5ae-6e6b-4f5b-b18a-d179d38cc0f3:Admin");
    }

    [Fact]
    public async Task AssignRole_AddsRoleAndUpdatesEntry()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "2", UserName = "bob" };

        _ = context.Mocks.Users
            .Setup(service => service.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tenant>
            {
                new()
                {
                    Id = Guid.Parse("d1932be1-2135-4139-937e-361e5dc8332b"),
                    Name = "Primary",
                    ClientId = "primary-client"
                }
            });
        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(new List<IdentityRole> { new("Editor") }.AsQueryable());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.GetRolesAsync(user))
            .ReturnsAsync(new List<string>());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.AddToRoleAsync(user, "d1932be1-2135-4139-937e-361e5dc8332b:Editor"))
            .ReturnsAsync(IdentityResult.Success);

        IRenderedComponent<UserRoleAssignments> cut = RenderWithProviders(context);
        cut.WaitForAssertion(() => cut.Markup.Contains("bob", StringComparison.Ordinal));

        object? entryList = ComponentTestHelper.GetFieldValue<object>(cut.Instance, "users", typeof(UserRoleAssignments));
        List<object> entries = ((IEnumerable)entryList!).Cast<object>().ToList();
        object entry = Assert.Single(entries);
        entry.GetType().GetProperty("SelectedRole")!.SetValue(entry, "Editor");
        entry.GetType().GetProperty("SelectedTenantId")!.SetValue(entry, "d1932be1-2135-4139-937e-361e5dc8332b");

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "AssignRoleAsync", typeof(UserRoleAssignments), entry));

        context.Mocks.UserManager.Verify(manager => manager.AddToRoleAsync(user, "d1932be1-2135-4139-937e-361e5dc8332b:Editor"), Times.Once);
        List<string>? assignedRoles = ComponentTestHelper.GetPropertyValue<List<string>>(entry, "AssignedRoles");
        assignedRoles.Should().Contain("d1932be1-2135-4139-937e-361e5dc8332b:Editor");
    }

    [Fact]
    public async Task RemoveRole_RemovesRoleFromUser()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "3", UserName = "carol" };

        _ = context.Mocks.Users
            .Setup(service => service.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<User> { user });
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Tenant>
            {
                new()
                {
                    Id = Guid.Parse("8ad35b4d-611d-4cff-8dcb-824c19a7b5d9"),
                    Name = "Primary",
                    ClientId = "primary-client"
                }
            });
        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(new List<IdentityRole> { new("Admin") }.AsQueryable());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.GetRolesAsync(user))
            .ReturnsAsync(new List<string> { "8ad35b4d-611d-4cff-8dcb-824c19a7b5d9:Admin" });
        _ = context.Mocks.UserManager
            .Setup(manager => manager.RemoveFromRoleAsync(user, "8ad35b4d-611d-4cff-8dcb-824c19a7b5d9:Admin"))
            .ReturnsAsync(IdentityResult.Success);

        IRenderedComponent<UserRoleAssignments> cut = RenderWithProviders(context);
        cut.WaitForAssertion(() => cut.Markup.Contains("carol", StringComparison.Ordinal));

        object? entryList = ComponentTestHelper.GetFieldValue<object>(cut.Instance, "users", typeof(UserRoleAssignments));
        List<object> entries = ((IEnumerable)entryList!).Cast<object>().ToList();
        object entry = Assert.Single(entries);

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "RemoveRoleAsync", typeof(UserRoleAssignments), entry, "8ad35b4d-611d-4cff-8dcb-824c19a7b5d9:Admin"));

        context.Mocks.UserManager.Verify(manager => manager.RemoveFromRoleAsync(user, "8ad35b4d-611d-4cff-8dcb-824c19a7b5d9:Admin"), Times.Once);
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
