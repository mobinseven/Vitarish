using FluentAssertions;
using Vitarish.Main.Infrastructure.Links;

namespace Main.Tests.Infrastructure.Links;

public class QrCodePngGeneratorTests
{
    [Fact]
    public void Generate_WithValidPayload_ReturnsPngBytes()
    {
        QrCodePngGenerator sut = new();

        byte[] bytes = sut.Generate("https://example.test/r/abc123");

        bytes.Should().NotBeEmpty();
        bytes.Length.Should().BeGreaterThan(8);
        bytes[0].Should().Be(0x89);
        bytes[1].Should().Be((byte)'P');
        bytes[2].Should().Be((byte)'N');
        bytes[3].Should().Be((byte)'G');
        bytes[4].Should().Be(0x0D);
        bytes[5].Should().Be(0x0A);
        bytes[6].Should().Be(0x1A);
        bytes[7].Should().Be(0x0A);
    }

    [Fact]
    public void Generate_WithEmptyPayload_Throws()
    {
        QrCodePngGenerator sut = new();

        Action act = () => sut.Generate("");

        act.Should().Throw<ArgumentException>();
    }
}
