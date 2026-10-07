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
    public void Uploads_are_capped_at_512_megabytes_per_file()
    {
        FileTransferPolicy.MaximumContentBytes.Should().Be(536_870_912);
        FileTransferPolicy.MaximumMultipartRequestBytes.Should().BeGreaterThan(FileTransferPolicy.MaximumContentBytes);
    }

    [Theory]
    [InlineData("image/jpeg", "photo.JPG")]
    [InlineData("image/png", "scan.png")]
    [InlineData("image/webp", "image.webp")]
    [InlineData("image/heic", "IMG_0001.HEIC")]
    [InlineData("image/heif", "image.heif")]
    [InlineData("video/mp4", "walk.mp4")]
    [InlineData("video/quicktime", "walk.mov")]
    [InlineData("video/webm", "walk.webm")]
    [InlineData("application/pdf", "report.pdf")]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "letter.docx")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "sheet.xlsx")]
    public void Upload_formats_accept_the_agreed_media_types_with_their_extensions(string mediaType, string fileName)
    {
        FileTransferPolicy.TryResolveUploadFormat(mediaType, fileName, out FileUploadFormat format).Should().BeTrue();

        format.MediaType.Should().Be(mediaType);
    }

    [Theory]
    [InlineData("text/plain", "notes.txt")]
    [InlineData("application/x-msdownload", "setup.exe")]
    [InlineData("image/svg+xml", "logo.svg")]
    [InlineData("application/msword", "letter.doc")]
    [InlineData("image/png", "photo.jpg")]
    [InlineData("image/png", "photo.png.exe")]
    [InlineData("image/png", "photo")]
    [InlineData("image/png", null)]
    public void Upload_formats_reject_other_types_and_mismatched_extensions(string mediaType, string? fileName)
    {
        FileTransferPolicy.TryResolveUploadFormat(mediaType, fileName, out _).Should().BeFalse();
    }

    public static TheoryData<string, byte[]> MatchingSignatures => new()
    {
        { "image/jpeg", [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10, 0x4A, 0x46] },
        { "image/png", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00] },
        { "image/webp", [.. "RIFF"u8, 0x24, 0x00, 0x00, 0x00, .. "WEBPVP8 "u8] },
        { "image/heic", [0x00, 0x00, 0x00, 0x18, .. "ftypheic"u8, 0x00, 0x00, 0x00, 0x00] },
        { "video/mp4", [0x00, 0x00, 0x00, 0x20, .. "ftypisom"u8, 0x00, 0x00, 0x02, 0x00] },
        { "video/quicktime", [0x00, 0x00, 0x00, 0x14, .. "ftypqt  "u8, 0x00, 0x00, 0x00, 0x00] },
        { "video/quicktime", [0x00, 0x00, 0x00, 0x08, .. "wide"u8, 0x00, 0x00, 0x00, 0x00] },
        { "video/webm", [0x1A, 0x45, 0xDF, 0xA3, 0x9F, 0x42, 0x86, 0x81] },
        { "application/pdf", [.. "%PDF-1.7"u8] },
        { "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00] }
    };

    [Theory]
    [MemberData(nameof(MatchingSignatures))]
    public void Signature_check_accepts_content_that_starts_like_its_format(string mediaType, byte[] header)
    {
        FileTransferPolicy.MatchesSignature(FormatOf(mediaType), header).Should().BeTrue();
    }

    public static TheoryData<string, byte[]> MismatchedSignatures => new()
    {
        { "image/png", [.. "MZ"u8, 0x90, 0x00, 0x03, 0x00, 0x00, 0x00] },
        { "image/jpeg", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A] },
        { "application/pdf", [.. "<html>"u8] },
        { "video/mp4", [0x00, 0x00, 0x00, 0x18, .. "ftypheic"u8, 0x00, 0x00, 0x00, 0x00] },
        { "image/heic", [0x00, 0x00, 0x00, 0x20, .. "ftypisom"u8, 0x00, 0x00, 0x02, 0x00] },
        { "application/vnd.openxmlformats-officedocument.wordprocessingml.document", [0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1] },
        { "image/webp", [.. "RIFF"u8] },
        { "image/png", [] }
    };

    [Theory]
    [MemberData(nameof(MismatchedSignatures))]
    public void Signature_check_rejects_content_of_another_kind_or_too_short_to_tell(string mediaType, byte[] header)
    {
        FileTransferPolicy.MatchesSignature(FormatOf(mediaType), header).Should().BeFalse();
    }

    private static FileUploadFormat FormatOf(string mediaType) =>
        FileTransferPolicy.UploadFormats.Single(format => format.MediaType == mediaType);

    [Fact]
    public void Storage_keys_are_unique_per_tenant_even_when_the_file_id_matches()
    {
        Guid fileId = Guid.Parse("ba4b64fd-dbf9-4fd2-b186-da0e8d6fd41e");

        FileStorageKey.For("tenant-a", fileId).Should().NotBe(FileStorageKey.For("tenant-b", fileId));
    }
}
