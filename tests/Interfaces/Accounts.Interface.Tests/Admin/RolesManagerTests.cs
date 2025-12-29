using Accounts.Interface.Tests.TestInfrastructure;
using Bunit;
using Bunit.Rendering;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using System.Linq;
using Vitarish.Accounts.Interface.Components.Admin;
using Vitarish.Parties.Domain.Entities;

namespace Accounts.Interface.Tests.Admin;

public class RolesManagerTests
{
    [Fact]
    public async Task RolesManager_LoadsRoles()
    {
        await using InterfaceTestContext context = new();
        List<IdentityRole> roles = [new IdentityRole("tenant-1:Admin")];
        TestAsyncEnumerable<IdentityRole> roleQueryable = new(roles);

        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(roleQueryable);
        _ = context.Mocks.RoleManager
            .Setup(manager => manager.GetClaimsAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(new List<System.Security.Claims.Claim>());
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Tenant { Id = Guid.NewGuid(), Name = "Tenant", ClientId = "client-1" }]);

        IRenderedComponent<RolesManager> cut = RenderWithProviders(context);

        cut.WaitForAssertion(() => Assert.Contains("tenant-1:Admin", cut.Markup, StringComparison.Ordinal));
    }

    [Fact]
    public async Task RolesManager_CanOpenCreateDialog()
    {
        await using InterfaceTestContext context = new();
        Guid tenantId = Guid.NewGuid();
        IdentityRole createdRole = new($"{tenantId}:Editor");

        TestAsyncEnumerable<IdentityRole> roleQueryable = new(new List<IdentityRole>());

        _ = context.Mocks.RoleManager
            .SetupGet(manager => manager.Roles)
            .Returns(roleQueryable);
        _ = context.Mocks.RoleManager
            .Setup(manager => manager.GetClaimsAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(new List<System.Security.Claims.Claim>());
        _ = context.Mocks.Parties
            .Setup(service => service.GetTenantsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([new Tenant { Id = tenantId, Name = "Tenant", ClientId = "client-1" }]);
        _ = context.Mocks.RoleManager
            .Setup(manager => manager.RoleExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);
        _ = context.Mocks.RoleManager
            .Setup(manager => manager.CreateAsync(It.IsAny<IdentityRole>()))
            .ReturnsAsync(IdentityResult.Success);
        _ = context.Mocks.RoleManager
            .Setup(manager => manager.FindByNameAsync(createdRole.Name!))
            .ReturnsAsync(createdRole);

        IRenderedComponent<RolesManager> cut = RenderWithProviders(context);

        ComponentTestHelper.SetField(cut.Instance, "newRoleName", "Editor", typeof(RolesManager));
        ComponentTestHelper.SetField(cut.Instance, "selectedTenantId", tenantId.ToString(), typeof(RolesManager));

        await cut.InvokeAsync(() => ComponentTestHelper.InvokeAsync(cut.Instance, "CreateRoleAsync", typeof(RolesManager)));

        context.Mocks.RoleManager.Verify(manager => manager.CreateAsync(It.IsAny<IdentityRole>()), Times.Once);
    }

    private sealed class TestAsyncQueryProvider<TEntity>(IQueryProvider inner) : IAsyncQueryProvider
    {
        public IQueryable CreateQuery(Expression expression) => new TestAsyncEnumerable<TEntity>(expression);

        public IQueryable<TElement> CreateQuery<TElement>(Expression expression) => new TestAsyncEnumerable<TElement>(expression);

        public object? Execute(Expression expression) => inner.Execute(expression);

        public TResult Execute<TResult>(Expression expression) => inner.Execute<TResult>(expression);

        public TResult ExecuteAsync<TResult>(Expression expression, CancellationToken cancellationToken)
            => Execute<TResult>(expression);
    }

    private sealed class TestAsyncEnumerable<T> : EnumerableQuery<T>, IAsyncEnumerable<T>, IQueryable<T>
    {
        public TestAsyncEnumerable(IEnumerable<T> enumerable) : base(enumerable) { }

        public TestAsyncEnumerable(Expression expression) : base(expression) { }

        public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            => new TestAsyncEnumerator<T>(this.AsEnumerable().GetEnumerator());

        IQueryProvider IQueryable.Provider => new TestAsyncQueryProvider<T>(this);
    }

    private sealed class TestAsyncEnumerator<T>(IEnumerator<T> inner) : IAsyncEnumerator<T>
    {
        public T Current => inner.Current;

        public ValueTask DisposeAsync()
        {
            inner.Dispose();
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> MoveNextAsync() => new(inner.MoveNext());
    }

    private static IRenderedComponent<RolesManager> RenderWithProviders(InterfaceTestContext context)
    {
        IRenderedComponent<ContainerFragment> renderResult = context.Render(builder =>
        {
            builder.OpenComponent<MudBlazor.MudPopoverProvider>(0);
            builder.CloseComponent();
            builder.OpenComponent<MudBlazor.MudDialogProvider>(1);
            builder.CloseComponent();
            builder.OpenComponent<RolesManager>(2);
            builder.CloseComponent();
        });

        return renderResult.FindComponent<RolesManager>();
    }
}
