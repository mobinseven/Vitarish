using Vitarish.Prescription.Application.Abstractions;
using Vitarish.Prescription.Infrastructure.Providers;

namespace Modules.Prescription.Tests;

public sealed class InsuranceProviderRegistryTests
{
    [Fact]
    public void GetRegisteredProviderKeys_ReturnsOnlyRegisteredAdapters()
    {
        // Arrange
        var registry = new InsuranceProviderRegistry(
        [
            InsuranceProviderAdapterStub.Create("TAMIN"),
            InsuranceProviderAdapterStub.Create("SALAMAT")
        ]);

        // Act
        IReadOnlyCollection<string> providerKeys = registry.GetRegisteredProviderKeys();

        // Assert
        providerKeys.Should().BeEquivalentTo(["SALAMAT", "TAMIN"]);
        providerKeys.Should().NotContain("DITAS");
    }

    [Theory]
    [InlineData("TAMIN")]
    [InlineData("tamin")]
    [InlineData(" TAMIN ")]
    [InlineData("SALAMAT")]
    public void IsProviderAvailable_AcceptsRegisteredProviderKeyOnly(string providerKey)
    {
        // Arrange
        var registry = new InsuranceProviderRegistry(
        [
            InsuranceProviderAdapterStub.Create("TAMIN"),
            InsuranceProviderAdapterStub.Create("SALAMAT")
        ]);

        // Act
        bool isAvailable = registry.IsProviderAvailable(providerKey);

        // Assert
        isAvailable.Should().BeTrue();
    }

    [Theory]
    [InlineData("DITAS")]
    [InlineData("")]
    [InlineData(" ")]
    public void IsProviderAvailable_RejectsUnregisteredProviderKey(string providerKey)
    {
        // Arrange
        var registry = new InsuranceProviderRegistry(
        [
            InsuranceProviderAdapterStub.Create("TAMIN"),
            InsuranceProviderAdapterStub.Create("SALAMAT")
        ]);

        // Act
        bool isAvailable = registry.IsProviderAvailable(providerKey);

        // Assert
        isAvailable.Should().BeFalse();
    }

    private static class InsuranceProviderAdapterStub
    {
        public static IInsuranceProviderAdapter Create(string providerKey)
        {
            var adapter = new Mock<IInsuranceProviderAdapter>();
            _ = adapter.Setup(x => x.ProviderKey).Returns(providerKey);
            return adapter.Object;
        }
    }
}
