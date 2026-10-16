using MediatR;
using Vitarish.Clinic.Application.Abstractions;
using Vitarish.Clinic.Application.Commands.Patients;
using Vitarish.Clinic.Application.Results;
using Vitarish.Clinic.Domain.Enums;
using Vitarish.Clinic.Domain.Models;

namespace Vitarish.Clinic.Application.Handlers;

public sealed class FindOrCreatePatientHandler(IPatientDirectory patientDirectory, ISender sender)
    : IRequestHandler<FindOrCreatePatientCommand, FindOrCreatePatientResult>
{
    public async Task<FindOrCreatePatientResult> Handle(
        FindOrCreatePatientCommand request,
        CancellationToken cancellationToken)
    {
        string identificationCode = request.IdentificationCode?.Trim() ?? string.Empty;
        if (identificationCode.Length == 0)
        {
            return Validation("Identification code is required.", "identificationCode");
        }

        if (identificationCode.Length > 256)
        {
            return Validation("Identification code must not exceed 256 characters.", "identificationCode");
        }

        PatientView? existing = await patientDirectory.FindByIdentificationCodeAsync(identificationCode, cancellationToken);
        if (existing is not null)
        {
            return new FindOrCreatePatientResult.Success(existing, false);
        }

        string firstName = request.FirstName?.Trim() ?? string.Empty;
        string lastName = request.LastName?.Trim() ?? string.Empty;
        Dictionary<string, string[]> validationErrors = [];
        if (firstName.Length == 0)
        {
            validationErrors["firstName"] = ["First name is required."];
        }

        if (lastName.Length == 0)
        {
            validationErrors["lastName"] = ["Last name is required."];
        }

        if (validationErrors.Count > 0)
        {
            return new FindOrCreatePatientResult.Failure(
                PatientCreationFailureReason.Validation,
                "Patient profile validation failed.",
                validationErrors);
        }

        PatientWriteDto patient = new()
        {
            IdentificationCode = identificationCode,
            Name = $"{firstName} {lastName}".Trim(),
            Description = string.Empty,
            Template = false,
            FirstName = firstName,
            LastName = lastName,
            Phone = NormalizeOptional(request.Phone),
            Email = NormalizeOptional(request.Email),
            DateOfBirth = request.DateOfBirth,
            Gender = request.Gender ?? Gender.Unspecified,
            PlaceOfBirth = NormalizeOptional(request.PlaceOfBirth),
            Address = NormalizeOptional(request.Address),
            Education = request.Education ?? Education.Unspecified,
            Occupation = NormalizeOptional(request.Occupation),
            FatherName = NormalizeOptional(request.FatherName),
            Height = request.Height ?? 0,
            Weight = request.Weight ?? 0,
            Referrer = NormalizeOptional(request.Referrer),
            InsuranceProvider = NormalizeOptional(request.InsuranceProvider),
            VisitReason = NormalizeOptional(request.VisitReason),
            Allergies = NormalizeOptional(request.Allergies),
            CurrentMedications = NormalizeOptional(request.CurrentMedications),
            ClinicalNotes = NormalizeOptional(request.ClinicalNotes),
            EmergencyContactName = NormalizeOptional(request.EmergencyContactName),
            EmergencyContactPhone = NormalizeOptional(request.EmergencyContactPhone),
            Tags = PatientTags.Normalize(request.Tags)
        };

        PatientCreationResult creation = await patientDirectory.CreateAsync(patient, cancellationToken);
        if (creation is PatientCreationResult.Success created)
        {
            try
            {
                await sender.Send(new PatientViewRefreshCommand(), cancellationToken);
                return new FindOrCreatePatientResult.Success(created.Patient, true);
            }
            catch (Exception exception) when (exception is InvalidOperationException or System.Data.Common.DbException)
            {
                return new FindOrCreatePatientResult.Failure(
                    PatientCreationFailureReason.ReadModelUnavailable,
                    "The patient was created but the patient list could not be refreshed.");
            }
        }

        PatientCreationResult.Failure failure = (PatientCreationResult.Failure)creation;
        if (failure.Reason == PatientCreationFailureReason.Conflict)
        {
            PatientView? concurrentlyCreated = await patientDirectory.FindByIdentificationCodeAsync(
                identificationCode,
                cancellationToken);
            if (concurrentlyCreated is not null)
            {
                return new FindOrCreatePatientResult.Success(concurrentlyCreated, false);
            }
        }

        return new FindOrCreatePatientResult.Failure(failure.Reason, failure.Message);
    }

    private static FindOrCreatePatientResult Validation(string message, string target) =>
        new FindOrCreatePatientResult.Failure(
            PatientCreationFailureReason.Validation,
            message,
            new Dictionary<string, string[]> { [target] = [message] });

    private static string? NormalizeOptional(string? value)
    {
        string? normalized = value?.Trim();
        return string.IsNullOrEmpty(normalized) ? null : normalized;
    }
}
