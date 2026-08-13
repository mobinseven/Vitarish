using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Intelligence.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class ClinicAssistantRuntime(
    ClinicAssistantConversationRunner conversationRunner,
    ClinicAssistantToolResolver toolResolver) : IAssistantRuntime, IAssistantGuardedToolResolver
{
    public const string ResultSchema = """
        {
          "type":"object",
          "additionalProperties":false,
          "required":["messages","actions"],
          "properties":{
            "messages":{"type":"array","items":{"type":"object","additionalProperties":false,"required":["kind","content"],"properties":{"kind":{"type":"string","enum":["assistantMessage","informationalToolResult","missingInformationRequest","failure"]},"content":{"type":"string"}}}},
            "actions":{"type":"array","items":{"type":"object","additionalProperties":false,"required":["capability","actionType","summary"],"properties":{"capability":{"type":"string"},"actionType":{"type":"string"},"summary":{"type":"string"}}}}
          }
        }
        """;

    public Task<AssistantResult> RunAsync(
        AssistantContext context,
        AssistantInput input,
        CancellationToken cancellationToken = default) =>
        conversationRunner.RunAsync(context, input, cancellationToken);

    public Task<AssistantToolResult> ResolveAsync(
        AssistantToolResolutionRequest request,
        CancellationToken cancellationToken = default) =>
        toolResolver.ResolveAsync(request, cancellationToken);
}

public static class ClinicAssistantCompositionServiceCollectionExtensions
{
    public static IServiceCollection AddClinicAssistantComposition(this IServiceCollection services)
    {
        _ = services.AddAgentComposition<ClinicAssistantCompositionContext>(
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.Appointments),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.AppointmentReads),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.Prescriptions),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.PrescriptionReads),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.PatientReminderReads),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.ClinicPatientReads),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.SeaPatientReads),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.ClinicalEvents),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.SeaManagement),
            new ClinicAssistantCapabilityPack(ClinicAssistantCapabilityKeys.Operations),
            new ClinicIntelligenceCapabilityPack());
        _ = services.AddTransient<ClinicAssistantExecutionContextFactory>();
        _ = services.AddTransient<ClinicAssistantManifestComposer>();
        _ = services.AddTransient<ClinicAssistantResultParser>();
        _ = services.AddTransient<ClinicAssistantConversationRunner>();
        _ = services.AddTransient<ClinicAssistantToolResolver>();
        _ = services.AddTransient<ClinicAssistantRuntime>();
        _ = services.AddTransient<IAssistantRuntime>(sp => sp.GetRequiredService<ClinicAssistantRuntime>());
        _ = services.AddTransient<IAssistantGuardedToolResolver>(sp => sp.GetRequiredService<ClinicAssistantRuntime>());
        return services;
    }
}
