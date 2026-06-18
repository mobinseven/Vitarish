namespace Vitarish.Clinic.Interface.Models;

internal static class PatientDisplayText
{
    public static string GetPatientHelperText(ProjectReadDto project, string identificationCodeLabel)
    {
        PatientReadDto patient = project.ToPatientRead();
        List<string> parts = [];

        if(!string.IsNullOrWhiteSpace(project.IdentificationCode))
        {
            parts.Add($"{identificationCodeLabel}: {project.IdentificationCode}");
        }

        if(patient.DateOfBirth.HasValue)
        {
            parts.Add(patient.DateOfBirth.Value.ToShortDateString());
        }

        if(!string.IsNullOrWhiteSpace(patient.Phone))
        {
            parts.Add(patient.Phone);
        }

        if(!string.IsNullOrWhiteSpace(patient.Email))
        {
            parts.Add(patient.Email);
        }

        return string.Join(" · ", parts);
    }
}
