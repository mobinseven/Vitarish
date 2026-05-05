using Vitarish.Clinical.Interface.Components;
using Vitarish.Medical.Interface.Plugins;
using Vitarish.Sea.Domain.Dto;

namespace Vitarish.Clinical.Interface.Plugins;

public sealed class ClinicalPatientSectionPlugin : IMedicalPatientSectionPlugin
{
    public string Id => "clinical.fhir-links";
    public string Label => "FHIR Resources";

    public RenderFragment Render(ProjectReadDto patient) => builder =>
    {
        builder.OpenComponent<PatientClinicalLinksPanel>(0);
        builder.AddComponentParameter(1, nameof(PatientClinicalLinksPanel.PatientProjectId), patient.Id);
        builder.CloseComponent();
    };
}
