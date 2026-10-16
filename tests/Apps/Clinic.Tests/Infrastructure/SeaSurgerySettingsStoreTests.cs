using System.Security.Claims;
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

public sealed class SeaSurgerySettingsStoreTests
{
    private const string TenantId = "33333333-3333-3333-3333-333333333333";
    private const string OtherTenantId = "77777777-7777-7777-7777-777777777777";
    private static readonly Guid TenantGuid = Guid.Parse(TenantId);

    [Fact]
    public async Task GetAsync_returns_null_when_the_clinic_has_no_settings()
    {
        Mock<IProjectObjectService> projectObjects = new();
        projectObjects.Setup(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((ProjectObjectReadDto?)null);
        SeaSurgerySettingsStore store = CreateStore(projectObjects.Object, TenantId);

        SurgerySettings? result = await store.GetAsync(CancellationToken.None);

        result.Should().BeNull();
    }

    [Fact]
    public async Task EnsureDefaultAsync_creates_once_with_a_deterministic_id_and_returns_the_existing_settings_unchanged()
    {
        Mock<IProjectObjectService> projectObjects = new();
        ProjectObjectReadDto? existing = null;
        projectObjects.Setup(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => existing);
        List<Guid?> createdIds = [];
        projectObjects.Setup(value => value.CreateAsync(
                SurgeryObjectTypes.CatalogOwnerEntityType, It.IsAny<Guid>(), SurgeryObjectTypes.SettingsObjectType,
                It.IsAny<JsonElement>(), It.IsAny<CancellationToken>(), It.IsAny<Guid?>()))
            .ReturnsAsync((string ownerEntityType, Guid ownerEntityId, string objectType, JsonElement data, CancellationToken _, Guid? id) =>
            {
                createdIds.Add(id);
                existing = new ProjectObjectReadDto(id!.Value, ownerEntityType, ownerEntityId, objectType, data, Guid.NewGuid(), DateTime.UtcNow, null, null, null, null, null);
                return existing;
            });

        SeaSurgerySettingsStore store = CreateStore(projectObjects.Object, TenantId);
        SurgerySettings first = await store.EnsureDefaultAsync(Defaults(), CancellationToken.None);
        SurgerySettings second = await store.EnsureDefaultAsync(Defaults(), CancellationToken.None);

        createdIds.Should().ContainSingle();
        createdIds[0].Should().NotBeNull();
        first.Should().Be(second);

        existing = null;
        SeaSurgerySettingsStore otherTenant = CreateStore(projectObjects.Object, OtherTenantId);
        await otherTenant.EnsureDefaultAsync(Defaults(), CancellationToken.None);

        createdIds.Should().HaveCount(2);
        createdIds.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task UpdateAsync_validates_the_settings_and_returns_the_updated_value()
    {
        Guid settingsId = Guid.NewGuid();
        Mock<IProjectObjectService> projectObjects = new();
        projectObjects.Setup(value => value.GetAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(Settings(settingsId, TenantGuid, "Old subject", "Old body"));
        projectObjects.Setup(value => value.UpdateAsync(
                It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(),
                It.IsAny<JsonElement>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Guid id, string ownerEntityType, Guid ownerEntityId, string objectType, JsonElement data, Guid token, CancellationToken _) =>
                new ProjectObjectReadDto(id, ownerEntityType, ownerEntityId, objectType, data, Guid.NewGuid(), DateTime.UtcNow, null, null, null, null, null));

        SeaSurgerySettingsStore store = CreateStore(projectObjects.Object, TenantId);

        Func<Task> blank = () => store.UpdateAsync(new SurgerySettingsWriteModel(" ", "Body"), Guid.NewGuid(), CancellationToken.None);
        await blank.Should().ThrowAsync<ArgumentException>();
        projectObjects.Verify(value => value.UpdateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<JsonElement>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);

        SurgerySettings? updated = await store.UpdateAsync(new SurgerySettingsWriteModel("Subject", "Body"), Guid.NewGuid(), CancellationToken.None);

        updated.Should().NotBeNull();
        updated!.CancellationSubject.Should().Be("Subject");
        updated.CancellationTextBody.Should().Be("Body");
    }

    [Fact]
    public async Task UpdateAsync_by_a_non_owner_throws_and_writes_nothing()
    {
        Mock<IProjectObjectService> projectObjects = new();
        SeaSurgerySettingsStore store = CreateStore(projectObjects.Object, TenantId, canPlan: false);

        Func<Task> action = () => store.UpdateAsync(new SurgerySettingsWriteModel("Subject", "Body"), Guid.NewGuid(), CancellationToken.None);

        await action.Should().ThrowAsync<SurgeryPlanningForbiddenException>();
        projectObjects.Verify(value => value.UpdateAsync(
            It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Guid>(), It.IsAny<string>(),
            It.IsAny<JsonElement>(), It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    private static SeaSurgerySettingsStore CreateStore(IProjectObjectService projectObjects, string tenantId, bool canPlan = true)
    {
        Mock<ISurgeryPlanningAuthorization> authorization = new();
        authorization.Setup(value => value.CanPlan()).Returns(canPlan);
        return new(projectObjects: projectObjects, authorization: authorization.Object, requestContext: new TestRequestContext(tenantId));
    }

    private static SurgerySettingsWriteModel Defaults() => new("Clinic cancellation", "The clinic cancelled this surgery.");

    private static ProjectObjectReadDto Settings(Guid id, Guid ownerId, string subject, string body) =>
        new(id, SurgeryObjectTypes.CatalogOwnerEntityType, ownerId, SurgeryObjectTypes.SettingsObjectType,
            JsonSerializer.SerializeToElement(
                new SurgerySettingsData(subject, body),
                SurgerySettingsJsonSerializerContext.Default.SurgerySettingsData),
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
        public string? RequestId => "clinic-settings-test";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { RequestId = "clinic-settings-test", Authority = Authority, TenantId = TenantId };
    }
}
