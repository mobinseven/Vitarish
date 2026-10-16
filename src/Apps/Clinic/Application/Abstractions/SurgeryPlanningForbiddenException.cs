namespace Vitarish.Clinic.Application.Abstractions;

/// <summary>Thrown when the current caller is not allowed to plan or manage surgeries.</summary>
public sealed class SurgeryPlanningForbiddenException() : Exception("Only the clinic owner can plan and manage surgeries.");
