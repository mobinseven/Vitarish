using Accounts.Interface.Tests.TestInfrastructure;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Identity;
using MudBlazor;
using System.Collections;
using Vitarish.Accounts.Interface.Components.Admin;
using Vitarish.Users.Domain.Entities;
using Vitarish.Parties.Domain.Entities;

namespace Accounts.Interface.Tests.Admin;

public class UserRoleAssignmentsTests
{
    [Fact]
    public async Task RendersUsersAndRolesFromServices()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "1", UserName = "alice" };
        Guid tenantId = Guid.Parse("8aa2f5ae-6e6b-4f5b-b18a-d179d38cc0f3");

        _ = context.Mocks.Users
            .Setup(service => service.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Tenant
                {
                    Id = tenantId,
                    Name = "Primary",
                    ClientId = "primary-client",
                    OwnerId = Guid.NewGuid()
                }
            ]);
        _ = context.Mocks.Parties
            .Setup(service => service.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Owner?)null);
        _ = context.Mocks.ApiClients
            .Setup(service => service.GetApiClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vitarish.ApiClients.Domain.Entities.ApiClient?)null);
        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(new List<IdentityRole> { new("Admin") }.AsQueryable());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.GetRolesAsync(user))
            .ReturnsAsync([$"{tenantId}:Admin"]);

        IRenderedComponent<UserRoleAssignments> cut = RenderWithProviders(context);

        cut.WaitForAssertion(() => cut.Markup.Contains("alice", StringComparison.Ordinal));
        // The role should be displayed in the child rows which are initially collapsed
    }

    [Fact]
    public async Task UserRoleAssignments_LoadsAssignments()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "2", UserName = "bob" };
        Guid tenantId = Guid.Parse("8aa2f5ae-6e6b-4f5b-b18a-d179d38cc0f4");

        _ = context.Mocks.Users
            .Setup(service => service.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Tenant
                {
                    Id = tenantId,
                    Name = "Primary",
                    ClientId = "primary-client",
                    OwnerId = Guid.NewGuid()
                }
            ]);
        _ = context.Mocks.Parties
            .Setup(service => service.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Owner?)null);
        _ = context.Mocks.ApiClients
            .Setup(service => service.GetApiClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vitarish.ApiClients.Domain.Entities.ApiClient?)null);
        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(new List<IdentityRole> { new("Admin") }.AsQueryable());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.GetRolesAsync(user))
            .ReturnsAsync([$"{tenantId}:Admin"]);

        IRenderedComponent<UserRoleAssignments> cut = RenderWithProviders(context);

        cut.WaitForAssertion(() => cut.Markup.Contains("bob", StringComparison.Ordinal));
    }

    [Fact]
    public async Task UserRoleAssignments_OpensAssignmentDialog()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "4", UserName = "dora" };
        Guid tenantId = Guid.Parse("8aa2f5ae-6e6b-4f5b-b18a-d179d38cc0f5");
        string selectedRole = $"{tenantId}:Admin";

        _ = context.Mocks.Users
            .Setup(service => service.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Tenant
                {
                    Id = tenantId,
                    Name = "Primary",
                    ClientId = "primary-client",
                    OwnerId = Guid.NewGuid()
                }
            ]);
        _ = context.Mocks.Parties
            .Setup(service => service.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Owner?)null);
        _ = context.Mocks.ApiClients
            .Setup(service => service.GetApiClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vitarish.ApiClients.Domain.Entities.ApiClient?)null);
        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(new List<IdentityRole> { new(selectedRole) }.AsQueryable());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.GetRolesAsync(user))
            .ReturnsAsync([]);
        _ = context.Mocks.UserManager
            .Setup(manager => manager.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        _ = context.Mocks.UserManager
            .Setup(manager => manager.AddToRoleAsync(user, selectedRole))
            .ReturnsAsync(IdentityResult.Success);

        Mock<IDialogReference> dialogReference = new();
        dialogReference.SetupGet(reference => reference.Result)
            .Returns(Task.FromResult<DialogResult?>(DialogResult.Ok(selectedRole)));
        _ = context.Mocks.DialogService
            .Setup(service => service.ShowAsync<UserRoleAssignmentDialog>(
                It.IsAny<string>(),
                It.IsAny<DialogParameters<UserRoleAssignmentDialog>>(),
                It.IsAny<DialogOptions>()))
            .ReturnsAsync(dialogReference.Object);

        IRenderedComponent<UserRoleAssignments> cut = RenderWithProviders(context);
        cut.WaitForAssertion(() => cut.Markup.Contains("dora", StringComparison.Ordinal));

        object? entryList = ComponentTestHelper.GetFieldValue<object>(cut.Instance, "users", typeof(UserRoleAssignments));
        List<object> entries = ((IEnumerable)entryList!).Cast<object>().ToList();
        object entry = Assert.Single(entries);

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "OpenAssignRoleDialogAsync", typeof(UserRoleAssignments), entry));

        context.Mocks.UserManager.Verify(manager => manager.AddToRoleAsync(user, selectedRole), Times.Once);
    }

    [Fact]
    public async Task RemoveRole_RemovesRoleFromUser()
    {
        await using InterfaceTestContext context = new();
        User user = new() { Id = "3", UserName = "carol" };
        Guid tenantId = Guid.Parse("8ad35b4d-611d-4cff-8dcb-824c19a7b5d9");

        _ = context.Mocks.Users
            .Setup(service => service.GetUsersAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([user]);
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(
            [
                new Tenant
                {
                    Id = tenantId,
                    Name = "Primary",
                    ClientId = "primary-client",
                    OwnerId = Guid.NewGuid()
                }
            ]);
        _ = context.Mocks.Parties
            .Setup(service => service.GetOwnerAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Owner?)null);
        _ = context.Mocks.ApiClients
            .Setup(service => service.GetApiClientAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Vitarish.ApiClients.Domain.Entities.ApiClient?)null);
        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(new List<IdentityRole> { new("Admin") }.AsQueryable());
        _ = context.Mocks.UserManager
            .Setup(manager => manager.FindByIdAsync(user.Id))
            .ReturnsAsync(user);
        _ = context.Mocks.UserManager
            .Setup(manager => manager.GetRolesAsync(user))
            .ReturnsAsync([$"{tenantId}:Admin"]);
        _ = context.Mocks.UserManager
            .Setup(manager => manager.RemoveFromRoleAsync(user, $"{tenantId}:Admin"))
            .ReturnsAsync(IdentityResult.Success);
        _ = context.Mocks.DialogService
            .Setup(service => service.ShowMessageBox(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DialogOptions>()))
            .ReturnsAsync(true);

        IRenderedComponent<UserRoleAssignments> cut = RenderWithProviders(context);
        cut.WaitForAssertion(() => cut.Markup.Contains("carol", StringComparison.Ordinal));

        object? entryList = ComponentTestHelper.GetFieldValue<object>(cut.Instance, "users", typeof(UserRoleAssignments));
        List<object> entries = ((IEnumerable)entryList!).Cast<object>().ToList();
        object entry = Assert.Single(entries);

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "RemoveRoleAsync", typeof(UserRoleAssignments), entry, $"{tenantId}:Admin"));

        context.Mocks.UserManager.Verify(manager => manager.RemoveFromRoleAsync(user, $"{tenantId}:Admin"), Times.Once);
    }

    private static IRenderedComponent<UserRoleAssignments> RenderWithProviders(InterfaceTestContext context)
    {
        IRenderedComponent<ContainerFragment> renderResult = context.Render(builder =>
        {
            builder.OpenComponent<MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<MudDialogProvider>(1);
            builder.CloseComponent();
            builder.OpenComponent<UserRoleAssignments>(2);
            builder.CloseComponent();
        });

        return renderResult.FindComponent<UserRoleAssignments>();
    }
}
