namespace Vitarish.Clinic.Application.PatientAgent;

public sealed record PatientAgentResult(
    IReadOnlyList<PatientAgentMessage> Messages,
    IReadOnlyList<PatientAgentAction> Actions)
{
    public static PatientAgentResult Failure(string message) =>
        new([new PatientAgentMessage(PatientAgentMessageKind.Failure, message)], []);
}
