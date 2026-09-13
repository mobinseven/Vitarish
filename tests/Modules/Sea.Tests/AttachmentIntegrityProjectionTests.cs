using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Enums;
using Vitarish.Sea.Domain.Models;

namespace Vitarish.Sea.Tests;

public sealed class AttachmentIntegrityProjectionTests
{
    [Fact]
    public void ComputeDigest_is_stable_for_unchanged_attachment()
    {
        Attachment attachment = CreateAttachment();

        string first = AttachmentIntegrityProjection.ComputeDigest(attachment);
        string second = AttachmentIntegrityProjection.ComputeDigest(attachment);

        second.Should().Be(first);
    }

    [Fact]
    public void ComputeDigest_changes_when_owned_content_changes()
    {
        Attachment attachment = CreateAttachment();
        string original = AttachmentIntegrityProjection.ComputeDigest(attachment);

        attachment.Content = "changed attachment text";

        AttachmentIntegrityProjection.ComputeDigest(attachment).Should().NotBe(original);
    }

    [Fact]
    public void ComputeDigest_changes_when_file_reference_metadata_changes()
    {
        Attachment attachment = CreateAttachment();
        string original = AttachmentIntegrityProjection.ComputeDigest(attachment);

        attachment.FileName = "updated-report.txt";

        AttachmentIntegrityProjection.ComputeDigest(attachment).Should().NotBe(original);
    }

    private static Attachment CreateAttachment()
    {
        Attachment attachment = Attachment.Create(
            Guid.Parse("8a1b21cf-7afd-4ff8-95ae-768b2fb7a0c8"),
            "attachment text",
            "text/plain",
            "thumbnail reference",
            15,
            AttachmentKind.File,
            "report.txt").EnsureValue();
        attachment.Id = Guid.Parse("da6864d7-50fc-4334-a5a0-716da2e6a475");
        return attachment;
    }
}
