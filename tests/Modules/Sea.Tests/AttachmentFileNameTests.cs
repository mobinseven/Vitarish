using FluentAssertions;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Tests;

public sealed class AttachmentFileNameTests
{
    [Fact]
    public void FileName_accepts_base_display_metadata_and_null()
    {
        var attachment = new Attachment { FileName = "clinical report.pdf" };
        attachment.FileName.Should().Be("clinical report.pdf");
        attachment.FileName = null;
        attachment.FileName.Should().BeNull();
    }

    [Theory]
    [InlineData("folder/report.pdf")]
    [InlineData("folder\\report.pdf")]
    [InlineData("report\u0000.pdf")]
    public void FileName_rejects_paths_and_control_characters(string fileName)
    {
        Action assign = () => new Attachment { FileName = fileName };
        assign.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FileName_rejects_values_over_the_display_limit()
    {
        Action assign = () => new Attachment { FileName = new string('a', Attachment.MaximumFileNameLength + 1) };
        assign.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void New_text_and_file_attachments_accept_their_owned_filename_shapes()
    {
        Attachment.Create(Guid.NewGuid(), "note", "text/plain", null, 4, AttachmentKind.Text, null).IsSuccess.Should().BeTrue();
        Attachment.Create(Guid.NewGuid(), "file-ref", "text/plain", null, 4, AttachmentKind.File, "report.txt").IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(AttachmentKind.File, null)]
    [InlineData(AttachmentKind.File, "")]
    [InlineData(AttachmentKind.Text, "note.txt")]
    [InlineData(AttachmentKind.File, "folder/report.pdf")]
    [InlineData(AttachmentKind.File, "folder\\report.pdf")]
    [InlineData(AttachmentKind.File, "report\u0001.pdf")]
    public void New_attachment_validation_returns_validation_failure(AttachmentKind kind, string? fileName)
    {
        var result = Attachment.Create(Guid.NewGuid(), "content", "text/plain", null, 4, kind, fileName);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Code == Vitarish.Entities.Contracts.Models.EntityErrorCode.Validation);
    }

    [Fact]
    public void Legacy_file_with_null_filename_can_be_materialized()
    {
        var attachment = new Attachment { Kind = AttachmentKind.File, FileName = null };

        attachment.Kind.Should().Be(AttachmentKind.File);
        attachment.FileName.Should().BeNull();
    }
}
