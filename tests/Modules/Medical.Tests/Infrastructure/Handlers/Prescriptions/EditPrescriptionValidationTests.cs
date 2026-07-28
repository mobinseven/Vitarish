using FluentAssertions;
using Vitarish.Medical.Application.Commands.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;
using Vitarish.Medical.Infrastructure.Handlers.Prescriptions;

namespace Medical.Tests.Infrastructure.Handlers.Prescriptions;

public sealed class EditPrescriptionValidationTests
{
    [Fact]
    public void Accepts_a_bounded_well_formed_amendment()
    {
        EditPrescriptionHandler.IsValidEditRequest(Command("""{"dose":"daily"}"""))
            .Should().BeTrue();
    }

    [Fact]
    public void Rejects_oversized_or_malformed_amendments()
    {
        EditPrescriptionHandler.IsValidEditRequest(Command("{"))
            .Should().BeFalse();

        EditPrescriptionHandler.IsValidEditRequest(
                Command(null) with { EditReason = new string('x', 1025) })
            .Should().BeFalse();

        EditPrescriptionHandler.IsValidEditRequest(
                Command(null) with
                {
                    Items = Enumerable.Range(0, 101)
                        .Select(_ => Item())
                        .ToArray()
                })
            .Should().BeFalse();
    }

    private static EditPrescriptionCommand Command(string? dosageJson) => new()
    {
        PrescriptionId = Guid.NewGuid(),
        CorrelationId = "correlation",
        ExpectedVersion = 1,
        EditReason = "Clinical correction",
        Items = [Item(dosageJson)]
    };

    private static PrescriptionEditItem Item(string? dosageJson = null) => new(
        PrescriptionItemCategory.Drug,
        "reference",
        "Display name",
        dosageJson,
        1,
        7,
        null);
}
