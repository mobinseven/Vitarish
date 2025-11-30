using Vitarish.Base.Domain.Primitives;

namespace Base.Tests.Domain.Primitives;

public class ValueObjectTests
{
    private sealed class Address : ValueObject
    {
        public string Street { get; }
        public string City { get; }
        public string ZipCode { get; }

        public Address(string street, string city, string zipCode)
        {
            Street = street;
            City = city;
            ZipCode = zipCode;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Street;
            yield return City;
            yield return ZipCode;
        }
    }

    private sealed class Money : ValueObject
    {
        public decimal Amount { get; }
        public string Currency { get; }

        public Money(decimal amount, string currency)
        {
            Amount = amount;
            Currency = currency;
        }

        protected override IEnumerable<object?> GetEqualityComponents()
        {
            yield return Amount;
            yield return Currency;
        }
    }

    [Fact]
    public void ValueObject_WithSameValues_AreEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", "City", "12345");
        var address2 = new Address("123 Main St", "City", "12345");

        // Assert
        address1.Should().Be(address2);
        (address1 == address2).Should().BeTrue();
        address1.GetHashCode().Should().Be(address2.GetHashCode());
    }

    [Fact]
    public void ValueObject_WithDifferentValues_AreNotEqual()
    {
        // Arrange
        var address1 = new Address("123 Main St", "City", "12345");
        var address2 = new Address("456 Oak Ave", "City", "12345");

        // Assert
        address1.Should().NotBe(address2);
        (address1 != address2).Should().BeTrue();
    }

    [Fact]
    public void ValueObject_ComparedToNull_ReturnsNotEqual()
    {
        // Arrange
        var address = new Address("123 Main St", "City", "12345");

        // Assert
        address.Equals(null).Should().BeFalse();
        (address == null).Should().BeFalse();
        (address != null).Should().BeTrue();
    }

    [Fact]
    public void ValueObject_ComparedToSelf_ReturnsEqual()
    {
        // Arrange
        var address = new Address("123 Main St", "City", "12345");

        // Assert
        address.Equals(address).Should().BeTrue();
    }

    [Fact]
    public void ValueObject_DifferentTypes_AreNotEqual()
    {
        // Arrange
        var address = new Address("123", "City", "12345");
        var money = new Money(123, "USD");

        // Assert
        address.Equals(money).Should().BeFalse();
    }

    [Fact]
    public void ValueObject_NullValues_AreHandledCorrectly()
    {
        // Arrange
        ValueObject? address1 = null;
        ValueObject? address2 = null;
        var address3 = new Address("123 Main St", "City", "12345");

        // Assert
        (address1 == address2).Should().BeTrue();
        (address1 == address3).Should().BeFalse();
        (address3 == address1).Should().BeFalse();
    }
}
