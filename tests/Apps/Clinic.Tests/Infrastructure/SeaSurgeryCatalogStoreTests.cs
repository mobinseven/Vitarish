using System.Text.Json;
using FluentAssertions;
using Moq;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Clinic.Infrastructure.Serializers;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinic.Tests.Infrastructure;

public sealed class SeaSurgeryCatalogStoreTests
{
    private const string TenantId = "22222222-2222-2222-2222-222222222222";
    private const string OtherTenantId = "88888888-8888-8888-8888-888888888888";
    private static readonly Guid TenantGuid = Guid.Parse(TenantId);

    [Fact]
    public async Task FindAsync_resolves_the_legacy_id_to_the_clinics_default_type()
    {
        Guid defaultId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        projectObjects.Setup(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Catalog(defaultId, TenantGuid, "General Surgery", isDefault: true));
        SeaSurgeryCatalogStore store = CreateStore(projectObjects.Object, TenantId);

        SurgeryCatalog? catalog = await store.FindAsync(SurgeryCatalogDefaults.LegacyGeneralSurgeryId, CancellationToken.None);

        catalog.Should().NotBeNull();
        catalog!.Id.Should().Be(defaultId.ToString("D"));
        catalog.Name.Should().Be("General Surgery");
    }

    [Fact]
    public async Task FindAsync_returns_null_for_another_owners_object()
    {
        Guid id = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        projectObjects.Setup(value => value.GetAsync(id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Catalog(id, Guid.NewGuid(), "Foreign Surgery", isDefault: false));
        SeaSurgeryCatalogStore store = CreateStore(projectObjects.Object, TenantId);

        SurgeryCatalog? catalog = await store.FindAsync(id.ToString("D"), CancellationToken.None);

        catalog.Should().BeNull();
    }

    [Fact]
    public async Task EnsureDefaultAsync_returns_the_existing_default_without_creating()
    {
        ProjectObjectReadDto existing = Catalog(Guid.NewGuid(), TenantGuid, "General Surgery", isDefault: true);
        Mock<IProjectObjectService> projectObjects = new();
        projectObjects.Setup(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        SeaSurgeryCatalogStore store = CreateStore(projectObjects.Object, TenantId);

        SurgeryCatalog catalog = await store.EnsureDefaultAsync(Defaults(), CancellationToken.None);

        catalog.Id.Should().Be(existing.Id.ToString("D"));
        projectObjects.Verify(value => value.CreateAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<JsonElement>(),
            It.IsAny<CancellationToken>(), It.IsAny<Guid?>()), Times.Never);
    }

    [Fact]
    public async Task EnsureDefaultAsync_creates_a_deterministic_id_per_tenant_when_missing()
    {
        Mock<IProjectObjectService> projectObjects = new();
        projectObjects.Setup(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectObjectReadDto?)null);
        List<Guid?> createdIds = [];
        projectObjects.Setup(value => value.CreateAsync(
                SurgeryObjectTypes.CatalogOwnerEntityType, It.IsAny<Guid>(), SurgeryObjectTypes.CatalogObjectType,
                It.IsAny<JsonElement>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()))
            .ReturnsAsync((string ownerEntityType, Guid ownerEntityId, string objectType, JsonElement data, CancellationToken _, Guid? id) =>
            {
                createdIds.Add(id);
                return new ProjectObjectReadDto(id!.Value, ownerEntityType, ownerEntityId, objectType, data, Guid.NewGuid(), DateTime.UtcNow, null, null, null, null, null);
            });

        SeaSurgeryCatalogStore firstTenant = CreateStore(projectObjects.Object, TenantId);
        SurgeryCatalog first = await firstTenant.EnsureDefaultAsync(Defaults(), CancellationToken.None);
        SurgeryCatalog second = await firstTenant.EnsureDefaultAsync(Defaults(), CancellationToken.None);

        first.Id.Should().Be(second.Id);
        createdIds.Should().HaveCount(2);
        createdIds.Select(id => id!.Value.ToString("D")).Should().OnlyContain(id => id == first.Id);

        SeaSurgeryCatalogStore otherTenant = CreateStore(projectObjects.Object, OtherTenantId);
        SurgeryCatalog other = await otherTenant.EnsureDefaultAsync(Defaults(), CancellationToken.None);

        other.Id.Should().NotBe(first.Id);
    }

    [Fact]
    public async Task DeleteAsync_refuses_to_delete_the_default_type()
    {
        Mock<IProjectObjectService> projectObjects = new();
        projectObjects.Setup(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectObjectReadDto?)null);
        projectObjects.Setup(value => value.CreateAsync(
                SurgeryObjectTypes.CatalogOwnerEntityType, It.IsAny<Guid>(), SurgeryObjectTypes.CatalogObjectType,
                It.IsAny<JsonElement>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()))
            .ReturnsAsync((string ownerEntityType, Guid ownerEntityId, string objectType, JsonElement data, CancellationToken _, Guid? id) =>
                new ProjectObjectReadDto(id!.Value, ownerEntityType, ownerEntityId, objectType, data, Guid.NewGuid(), DateTime.UtcNow, null, null, null, null, null));
        SeaSurgeryCatalogStore store = CreateStore(projectObjects.Object, TenantId);
        SurgeryCatalog defaultCatalog = await store.EnsureDefaultAsync(Defaults(), CancellationToken.None);
        Guid defaultId = Guid.Parse(defaultCatalog.Id);
        projectObjects.Setup(value => value.GetAsync(defaultId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Catalog(defaultId, TenantGuid, "General Surgery", isDefault: true));

        Func<Task> action = () => store.DeleteAsync(defaultId, Guid.NewGuid(), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>();
        projectObjects.Verify(value => value.DeleteAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CreateAsync_by_a_non_owner_throws_and_writes_nothing()
    {
        Mock<IProjectObjectService> projectObjects = new();
        SeaSurgeryCatalogStore store = CreateStore(projectObjects.Object, TenantId, canPlan: false);

        Func<Task> action = () => store.CreateAsync(new SurgeryCatalogWriteModel("General Surgery", [], [], []), CancellationToken.None);

        await action.Should().ThrowAsync<SurgeryPlanningForbiddenException>();
        projectObjects.Verify(value => value.CreateAsync(
            It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<JsonElement>(),
            It.IsAny<CancellationToken>(), It.IsAny<Guid?>()), Times.Never);
    }

    private static SeaSurgeryCatalogStore CreateStore(IProjectObjectService projectObjects, string tenantId, bool canPlan = true)
    {
        Mock<ISurgeryPlanningAuthorization> authorization = new();
        authorization.Setup(value => value.CanPlan()).Returns(canPlan);
        return new(projectObjects: projectObjects, authorization: authorization.Object, requestContext: new TestRequestContext(tenantId));
    }

    private static SurgeryCatalogWriteModel Defaults() => new("General Surgery", [], [], []);

    private static ProjectObjectReadDto Catalog(Guid id, Guid ownerId, string name, bool isDefault) =>
        new(id, SurgeryObjectTypes.CatalogOwnerEntityType, ownerId, SurgeryObjectTypes.CatalogObjectType,
            JsonSerializer.SerializeToElement(
                new SurgeryCatalogData(name, [], [], [], isDefault),
                SurgeryCatalogJsonSerializerContext.Default.SurgeryCatalogData),
            Guid.NewGuid(), DateTime.UtcNow, null, null, null, null, null);

    private sealed class TestRequestContext(string tenantId) : IRequestContext
    {
        public string Authority => "clinic.tests";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => "clinic-catalog-test";
        public string? AdditionalInfo => null;
        public System.Security.Claims.ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { RequestId = "clinic-catalog-test", Authority = Authority, TenantId = TenantId };
    }
}
