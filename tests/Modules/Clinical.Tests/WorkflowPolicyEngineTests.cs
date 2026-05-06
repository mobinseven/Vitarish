namespace Clinical.Tests;

public class WorkflowPolicyEngineTests
{
    private readonly WorkflowPolicyEngine _engine = new();

    [Fact]
    public async Task Evaluate_WhenActorHasRequiredRole_ReturnsNoErrors()
    {
        WorkflowPolicyDefinition policy = CreatePolicy(allowedRole: "Nurse");
        ClinicalWorkflow workflow = CreateWorkflow(VisitWorkflowStatus.CheckedIn);

        IReadOnlyList<WorkflowValidationResult> result = await _engine.EvaluateAsync(
            actorId: "actor-1",
            actorRoles: ["Nurse"],
            actionType: ClinicalActionType.RecordVitals,
            workflow: workflow,
            policy: policy);

        result.Where(r => r.Severity == WorkflowValidationSeverity.Error).Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_WhenActorLacksRequiredRole_ReturnsRoleDeniedError()
    {
        WorkflowPolicyDefinition policy = CreatePolicy(allowedRole: "Nurse");
        ClinicalWorkflow workflow = CreateWorkflow(VisitWorkflowStatus.CheckedIn);

        IReadOnlyList<WorkflowValidationResult> result = await _engine.EvaluateAsync(
            actorId: "actor-1",
            actorRoles: ["Receptionist"],
            actionType: ClinicalActionType.RecordVitals,
            workflow: workflow,
            policy: policy);

        result.Should().ContainSingle(r =>
            r.Severity == WorkflowValidationSeverity.Error &&
            r.Code == "ROLE_DENIED");
    }

    [Fact]
    public async Task Evaluate_WhenNoRolesConfiguredForAction_AllowsAnyActor()
    {
        // A policy with NO role restrictions for a specific action should allow any actor
        WorkflowPolicyDefinition policy = new()
        {
            AllowedRolesByActionType = new Dictionary<ClinicalActionType, string[]>()
        };
        ClinicalWorkflow workflow = CreateWorkflow(VisitWorkflowStatus.CheckedIn);

        IReadOnlyList<WorkflowValidationResult> result = await _engine.EvaluateAsync(
            actorId: "actor-1",
            actorRoles: ["UnknownRole"],
            actionType: ClinicalActionType.RecordVitals,
            workflow: workflow,
            policy: policy);

        result.Where(r => r.Severity == WorkflowValidationSeverity.Error).Should().BeEmpty();
    }

    [Fact]
    public async Task Evaluate_WhenWorkflowIsClosed_DocumentationActionIsRejected()
    {
        WorkflowPolicyDefinition policy = WorkflowPolicyDefinition.CreateDefault();
        ClinicalWorkflow workflow = CreateWorkflow(VisitWorkflowStatus.Completed);

        IReadOnlyList<WorkflowValidationResult> result = await _engine.EvaluateAsync(
            actorId: "actor-1",
            actorRoles: ["Clinician"],
            actionType: ClinicalActionType.AddDiagnosis,
            workflow: workflow,
            policy: policy);

        result.Should().ContainSingle(r =>
            r.Severity == WorkflowValidationSeverity.Error &&
            r.Code == "WORKFLOW_CLOSED");
    }

    [Fact]
    public async Task Evaluate_WhenReopenNotAllowed_CancelledWorkflowCannotBeReopened()
    {
        WorkflowPolicyDefinition policy = WorkflowPolicyDefinition.CreateDefault();
        policy = policy with { AllowReopenAfterCancel = false };
        ClinicalWorkflow workflow = CreateWorkflow(VisitWorkflowStatus.Cancelled);

        IReadOnlyList<WorkflowValidationResult> result = await _engine.EvaluateAsync(
            actorId: "actor-1",
            actorRoles: ["Admin"],
            actionType: ClinicalActionType.ReopenVisit,
            workflow: workflow,
            policy: policy);

        result.Should().ContainSingle(r =>
            r.Severity == WorkflowValidationSeverity.Error &&
            r.Code == "REOPEN_NOT_ALLOWED");
    }

    [Fact]
    public async Task Evaluate_WhenNoWorkflow_RegisterPatientWithValidRole_PassesPolicyCheck()
    {
        WorkflowPolicyDefinition policy = WorkflowPolicyDefinition.CreateDefault();

        // RegisterPatient requires Receptionist role — actor has the required role
        IReadOnlyList<WorkflowValidationResult> result = await _engine.EvaluateAsync(
            actorId: "actor-1",
            actorRoles: ["Receptionist"],
            actionType: ClinicalActionType.RegisterPatient,
            workflow: null,
            policy: policy);

        result.Where(r => r.Severity == WorkflowValidationSeverity.Error).Should().BeEmpty();
    }

    private static WorkflowPolicyDefinition CreatePolicy(string allowedRole) =>
        WorkflowPolicyDefinition.CreateDefault() with
        {
            AllowedRolesByActionType = new Dictionary<ClinicalActionType, string[]>
            {
                [ClinicalActionType.RecordVitals] = [allowedRole]
            }
        };

    private static ClinicalWorkflow CreateWorkflow(VisitWorkflowStatus status) => new()
    {
        PatientFhirReference = "Patient/test-1",
        VisitType = "routine",
        Status = status
    };
}
