namespace Vitarish.Clinic.Application.PatientAgent.Policies;

public sealed class PatientAgentApprovalPolicy(IReadOnlySet<string>? toolsRequiringApproval = null)
{
    private readonly IReadOnlySet<string> _toolsRequiringApproval = toolsRequiringApproval ?? new HashSet<string>(StringComparer.Ordinal);

    public bool RequiresApproval(string toolName) => _toolsRequiringApproval.Contains(toolName);
}
