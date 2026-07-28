using MediatR;
using Moq;
using FluentAssertions;
using Vitarish.Medical.Application.AgentCapabilities.Prescriptions;
using Vitarish.Medical.Application.Queries.Prescriptions;
using Vitarish.Medical.Domain.Enums.Prescriptions;
using Vitarish.Medical.Interface.AgentCapabilities.Prescriptions;

namespace Vitarish.Medical.Tests.AgentCapabilities.Prescriptions;

public sealed class PrescriptionAgentWorkflowTests
{
    [Fact]
    public async Task Search_returns_exact_template_match()
    {
        Mock<ISender> sender = SenderReturning([
            Template("Pain plan"), Template("Pain plan extended")
        ]);

        PrescriptionTemplateSearchResult result = await new SearchPrescriptionTemplatesForAgent(sender.Object)
            .ExecuteAsync("tenant", "pain PLAN", default);

        result.Status.Should().Be(PrescriptionTemplateSearchStatus.ExactMatch);
        result.Matches.Should().ContainSingle(x => x.Name == "Pain plan");
    }

    [Fact]
    public async Task Search_reports_ambiguous_template_candidates()
    {
        Mock<ISender> sender = SenderReturning([Template("Pain adult"), Template("Pain child")]);

        PrescriptionTemplateSearchResult result = await new SearchPrescriptionTemplatesForAgent(sender.Object)
            .ExecuteAsync("tenant", "Pain", default);

        result.Status.Should().Be(PrescriptionTemplateSearchStatus.Ambiguous);
        result.Matches.Should().HaveCount(2);
    }

    [Fact]
    public async Task Requirements_identify_missing_drug_dosage_frequency_route_duration_and_instructions()
    {
        Mock<ISender> sender = new();
        sender.Setup(x => x.Send(It.IsAny<GetPrescriptionTemplateQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PrescriptionTemplateDto { Id = Guid.NewGuid(), Name = "Template", Status = PrescriptionTemplateStatus.Active, CreatedAt = DateTime.UtcNow,
                Items = [new PrescriptionTemplateItemDto { Category = PrescriptionItemCategory.Drug, DisplayName = "Clinician selected drug", Quantity = 1 }] });

        PrescriptionTemplateRequirements? result = await new GetPrescriptionTemplateRequirements(sender.Object)
            .ExecuteAsync(Guid.NewGuid(), "tenant", default);

        result!.MissingFields.Should().BeEquivalentTo(["dosage", "frequency", "route", "instructions", "duration"]);
    }

    [Fact]
    public async Task Prepare_rejects_missing_explicit_insurance_before_creating_a_draft()
    {
        Mock<ISender> sender = new();
        var requirements = new GetPrescriptionTemplateRequirements(sender.Object);
        var prepare = new PreparePrescriptionDraftFromTemplate(sender.Object, requirements, new InMemoryPrescriptionAgentReviewStore());
        var scope = new PrescriptionAgentScope("tenant", Guid.NewGuid(), "national-id", Guid.NewGuid(), Guid.NewGuid(), "clinician");
        var provenance = new PrescriptionAgentProvenance("AgentAssisted", "run", "tool", "clinician", null, "version");

        PrescriptionDraftPreparationResult result = await prepare.ExecuteAsync(Guid.NewGuid(), null, scope, provenance, default);

        result.Status.Should().Be(PrescriptionDraftPreparationStatus.MissingInsuranceCoverage);
        sender.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Prepare_rejects_missing_authoritative_patient_insurance_identity()
    {
        Mock<ISender> sender = new();
        var prepare = new PreparePrescriptionDraftFromTemplate(sender.Object, new GetPrescriptionTemplateRequirements(sender.Object), new InMemoryPrescriptionAgentReviewStore());
        var scope = new PrescriptionAgentScope("tenant", Guid.NewGuid(), null, Guid.NewGuid(), Guid.NewGuid(), "clinician");
        var provenance = new PrescriptionAgentProvenance("AgentAssisted", "run", "tool", "clinician", null, "version");

        PrescriptionDraftPreparationResult result = await prepare.ExecuteAsync(Guid.NewGuid(), "named-coverage", scope, provenance, default);

        result.Status.Should().Be(PrescriptionDraftPreparationStatus.MissingInformation);
        result.MissingFields.Should().ContainSingle("patient insurance identifier");
        sender.VerifyNoOtherCalls();
    }

    private static Mock<ISender> SenderReturning(IReadOnlyList<PrescriptionTemplateSummaryDto> templates)
    {
        Mock<ISender> sender = new();
        sender.Setup(x => x.Send(It.IsAny<GetPrescriptionTemplatesQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(templates);
        return sender;
    }

    private static PrescriptionTemplateSummaryDto Template(string name) => new() { Id = Guid.NewGuid(), Name = name, Status = PrescriptionTemplateStatus.Active, ItemCount = 1, CreatedAt = DateTime.UtcNow };
}
