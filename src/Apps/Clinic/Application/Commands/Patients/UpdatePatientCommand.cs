using MediatR;
using Vitarish.Clinic.Application.Results;
using Vitarish.Clinic.Domain.Enums;

namespace Vitarish.Clinic.Application.Commands.Patients;

public sealed record UpdatePatientCommand(
    Guid PatientId,
    string? IdentificationCode,
    string? FirstName,
    string? LastName,
    string? Phone,
    string? Email,
    DateTime? DateOfBirth,
    Gender? Gender,
    string? PlaceOfBirth,
    string? Address,
    Education? Education,
    string? Occupation,
    string? FatherName,
    int? Height,
    int? Weight,
    string? Referrer,
    string? InsuranceProvider,
    string? VisitReason,
    string? Allergies,
    string? CurrentMedications,
    string? ClinicalNotes,
    string? EmergencyContactName,
    string? EmergencyContactPhone,
    IReadOnlyList<string>? Tags = null) : IRequest<PatientUpdateResult>;
