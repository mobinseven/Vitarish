namespace Vitarish.Clinic.Application.Abstractions;

/// <summary>
/// Whether the current caller may plan and manage surgeries for the current clinic.
/// Only a platform administrator, or a principal holding the current tenant's TenantOwner role or access claim,
/// may create, edit, complete, cancel, or manage surgeries and their types and settings.
/// </summary>
public interface ISurgeryPlanningAuthorization
{
    bool CanPlan();
}
