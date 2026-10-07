using MediatR;
using Moq;
using Vitarish.Entities.Contracts.Models;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Entities;
using Vitarish.Sea.Domain.Enums;
using Vitarish.Sea.Infrastructure.Services;

namespace Vitarish.Sea.Tests;

public sealed class AttachmentFileContentTests
{
    private const string InlineExecutable = "data:application/x-msdownload;base64,TVqQAAMAAAAEAAAA";

    [Theory]
    [InlineData(InlineExecutable)]
    [InlineData("https://example.test/report.pdf")]
    [InlineData("")]
    [InlineData(null)]
    public void New_file_content_must_reference_an_uploaded_file(string? content)
    {
        EntityResult result = Attachment.ValidateNewContent(AttachmentKind.File, content);

        result.IsFailure.Should().BeTrue();
        result.Errors.Should().ContainSingle(error => error.Target == nameof(Attachment.Content));
    }

    [Fact]
    public void New_file_content_accepts_an_uploaded_file_identifier_and_text_stays_free_form()
    {
        Attachment.ValidateNewContent(AttachmentKind.File, Guid.NewGuid().ToString()).IsSuccess.Should().BeTrue();
        Attachment.ValidateNewContent(AttachmentKind.Text, "a clinical note").IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Existing_inline_file_content_can_still_be_rebuilt_for_integrity_checks()
    {
        Attachment.Create(Guid.NewGuid(), InlineExecutable, "application/pdf", null, 12, AttachmentKind.File, "report.pdf")
            .IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Adding_a_file_attachment_with_inline_content_is_rejected_before_it_is_stored()
    {
        Mock<IMediator> mediator = new(MockBehavior.Strict);
        AttachmentsService service = new(mediator.Object);

        EntityResult<AttachmentReadDto> single = await service.AddAsync(
            Guid.NewGuid(), InlineExecutable, "application/pdf", null, 12, null, AttachmentKind.File, CancellationToken.None);
        EntityResult<IReadOnlyList<AttachmentReadDto>> batch = await service.AddBatchAsync(
            [new AttachmentWriteDto(Guid.NewGuid(), InlineExecutable, "application/pdf", null, 12, AttachmentKind.File, "report.pdf")],
            CancellationToken.None);

        single.IsFailure.Should().BeTrue();
        batch.IsFailure.Should().BeTrue();
        mediator.VerifyNoOtherCalls();
    }
}
