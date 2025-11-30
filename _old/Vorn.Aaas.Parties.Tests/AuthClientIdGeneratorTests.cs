namespace Vorn.Aaas.Parties.Tests;

using Vorn.Aaas.Parties.Domain.Enums;
using Vorn.Aaas.Parties.Infrastructure.Extensions;

/// <summary>
/// Tests for AuthClientIdGenerator deterministic behavior.
/// </summary>
public class AuthClientIdGeneratorTests
{
    [Fact]
    public void Generate_IsDeterministic()
    {
        var tenantId = Guid.NewGuid();
        var method = IndicatorMethod.Hostname;
        var value = "example.com";

        var id1 = AuthClientIdGenerator.Generate(tenantId, method, value);
        var id2 = AuthClientIdGenerator.Generate(tenantId, method, value);

        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Generate_StartsWithPrefix()
    {
        var id = AuthClientIdGenerator.Generate(Guid.NewGuid(), IndicatorMethod.Hostname, "example.com");

        Assert.StartsWith("ac:", id);
    }

    [Fact]
    public void Generate_HasCorrectLength()
    {
        var id = AuthClientIdGenerator.Generate(Guid.NewGuid(), IndicatorMethod.Hostname, "example.com");

        // ac: (3 chars) + 32 base32 chars = 35 total
        Assert.Equal(35, id.Length);
    }

    [Fact]
    public void Generate_IsLowercase()
    {
        var id = AuthClientIdGenerator.Generate(Guid.NewGuid(), IndicatorMethod.Hostname, "EXAMPLE.COM");

        Assert.Equal(id, id.ToLowerInvariant());
    }

    [Fact]
    public void Generate_DifferentForDifferentTenants()
    {
        var value = "example.com";
        var method = IndicatorMethod.Hostname;

        var id1 = AuthClientIdGenerator.Generate(Guid.NewGuid(), method, value);
        var id2 = AuthClientIdGenerator.Generate(Guid.NewGuid(), method, value);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Generate_DifferentForDifferentMethods()
    {
        var tenantId = Guid.NewGuid();
        var value = "example.com";

        var id1 = AuthClientIdGenerator.Generate(tenantId, IndicatorMethod.Hostname, value);
        var id2 = AuthClientIdGenerator.Generate(tenantId, IndicatorMethod.Subdomain, value);

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Generate_DifferentForDifferentValues()
    {
        var tenantId = Guid.NewGuid();
        var method = IndicatorMethod.Hostname;

        var id1 = AuthClientIdGenerator.Generate(tenantId, method, "example.com");
        var id2 = AuthClientIdGenerator.Generate(tenantId, method, "different.com");

        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Generate_NoCollisions()
    {
        // Test for collision resistance with multiple inputs
        var ids = new HashSet<string>();
        
        for (int i = 0; i < 1000; i++)
        {
            var tenantId = Guid.NewGuid();
            var value = $"tenant{i}.example.com";
            var id = AuthClientIdGenerator.Generate(tenantId, IndicatorMethod.Hostname, value);
            
            Assert.DoesNotContain(id, ids);
            ids.Add(id);
        }

        Assert.Equal(1000, ids.Count);
    }
}
