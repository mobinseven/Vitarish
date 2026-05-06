namespace Clinical.Tests;

public class ConflictHandlerServiceTests
{
    [Fact]
    public void IsRetriableSafe_Returns_True_ForIdempotentActions()
    {
        var handler = new ConflictHandlerService(null!);

        handler.IsRetriableSafe(ClinicalActionType.CheckInPatient).Should().BeTrue();
        handler.IsRetriableSafe(ClinicalActionType.CompleteVisit).Should().BeTrue();
        handler.IsRetriableSafe(ClinicalActionType.CancelVisit).Should().BeTrue();
        handler.IsRetriableSafe(ClinicalActionType.ReopenVisit).Should().BeTrue();
        handler.IsRetriableSafe(ClinicalActionType.SignNote).Should().BeTrue();
    }

    [Fact]
    public void IsRetriableSafe_Returns_False_ForNonIdempotentActions()
    {
        var handler = new ConflictHandlerService(null!);

        handler.IsRetriableSafe(ClinicalActionType.RecordVitals).Should().BeFalse();
        handler.IsRetriableSafe(ClinicalActionType.AddDiagnosis).Should().BeFalse();
        handler.IsRetriableSafe(ClinicalActionType.AddMedicationRequest).Should().BeFalse();
        handler.IsRetriableSafe(ClinicalActionType.AddClinicalNote).Should().BeFalse();
        handler.IsRetriableSafe(ClinicalActionType.RegisterPatient).Should().BeFalse();
        handler.IsRetriableSafe(ClinicalActionType.StartEncounter).Should().BeFalse();
    }
}
