using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Vitarish.Clinic.Domain.Surgery;
using Vitarish.Clinic.Infrastructure.Handlers;
using Vitarish.Requests.Application.Abstractions;
using Vitarish.Requests.Domain.Entities;

namespace Vitarish.Clinic.Tests.Infrastructure;

public sealed class SurgeryTenantObjectsTests
{
    private const string TenantId = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";

    [Theory]
    [InlineData(SurgeryObjectTypes.CatalogObjectType)]
    [InlineData(SurgeryObjectTypes.SettingsObjectType)]
    public void Default_object_ids_match_the_ids_already_stored(string objectType)
        => SurgeryTenantObjects.DefaultObjectId(objectType, TenantId).Should().Be(StoredDefaultId(objectType + ".default:", TenantId));

    [Fact]
    public void Default_object_ids_differ_by_tenant_and_type()
    {
        Guid catalog = SurgeryTenantObjects.DefaultObjectId(SurgeryObjectTypes.CatalogObjectType, TenantId);

        catalog.Should().NotBe(SurgeryTenantObjects.DefaultObjectId(SurgeryObjectTypes.SettingsObjectType, TenantId));
        catalog.Should().NotBe(SurgeryTenantObjects.DefaultObjectId(SurgeryObjectTypes.CatalogObjectType, Guid.NewGuid().ToString()));
    }

    [Fact]
    public async Task CreateSingletonAsync_returns_the_stored_value_when_a_concurrent_create_wins()
    {
        object stored = new();

        object result = await SurgeryTenantObjects.CreateSingletonAsync<object>(
            () => throw new DbUpdateException("duplicate key"),
            () => Task.FromResult<object?>(stored),
            "not created");

        result.Should().BeSameAs(stored);
    }

    [Fact]
    public async Task CreateSingletonAsync_throws_when_nothing_was_stored()
    {
        Func<Task> action = () => SurgeryTenantObjects.CreateSingletonAsync<object>(
            () => throw new DbUpdateException("duplicate key"),
            () => Task.FromResult<object?>(null),
            "not created");

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("not created");
    }

    [Theory]
    [InlineData(null)]
    [InlineData(" ")]
    [InlineData("not-a-guid")]
    public void RequireTenant_rejects_a_missing_or_non_guid_tenant(string? tenantId)
    {
        Action action = () => SurgeryTenantObjects.RequireTenant(new TestRequestContext(tenantId));

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void RequireTenant_returns_the_tenant_and_its_owner_id()
        => SurgeryTenantObjects.RequireTenant(new TestRequestContext(TenantId)).Should().Be((TenantId, Guid.Parse(TenantId)));

    /// <summary>The derivation the stores used before it moved, kept here to pin compatibility.</summary>
    private static Guid StoredDefaultId(string prefix, string tenantId)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(prefix + tenantId));
        Span<byte> bytes = stackalloc byte[16];
        hash.AsSpan(0, 16).CopyTo(bytes);
        bytes[6] = (byte)((bytes[6] & 0x0F) | 0x80);
        bytes[8] = (byte)((bytes[8] & 0x3F) | 0x80);
        return new Guid(bytes, bigEndian: true);
    }

    private sealed class TestRequestContext(string? tenantId) : IRequestContext
    {
        public string Authority => "clinic.tests";
        public string? ClientId => null;
        public string? OwnerId => null;
        public string? UserId => null;
        public string? TenantId => tenantId;
        public string? ResourceId => null;
        public string? TraceId => null;
        public string? RequestId => "surgery-tenant-objects-test";
        public string? AdditionalInfo => null;
        public ClaimsPrincipal? User => null;
        public Request GetRequest() => new() { RequestId = RequestId, Authority = Authority, TenantId = TenantId };
    }
}
