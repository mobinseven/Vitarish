using Vitarish.Files.Domain.Models;

namespace Files.Tests.Domain;

public sealed class FileTransferPolicyTests
{
    [Theory]
    [InlineData("application/pdf", "application/pdf")]
    [InlineData("Application/Pdf; charset=utf-8", "application/pdf")]
    public void NormalizeContentType_accepts_a_media_type_and_discards_parameters(string value, string expected)
    {
        FileTransferPolicy.TryNormalizeContentType(value, out string normalized).Should().BeTrue();

        normalized.Should().Be(expected);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("not a media type")]
    public void NormalizeContentType_rejects_invalid_input(string value)
    {
        FileTransferPolicy.TryNormalizeContentType(value, out _).Should().BeFalse();
    }

    [Fact]
    public void Storage_keys_are_unique_per_tenant_even_when_the_file_id_matches()
    {
        Guid fileId = Guid.Parse("ba4b64fd-dbf9-4fd2-b186-da0e8d6fd41e");

        FileStorageKey.For("tenant-a", fileId).Should().NotBe(FileStorageKey.For("tenant-b", fileId));
    }
}
