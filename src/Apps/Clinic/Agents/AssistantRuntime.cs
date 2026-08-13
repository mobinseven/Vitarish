using Microsoft.Extensions.DependencyInjection;
using Vitarish.Clinic.Application.Assistant;
using Vitarish.Intelligence.Agents;

namespace Vitarish.Clinic.Agents;

public sealed class AssistantRuntime(
    AssistantConversationRunner conversationRunner,
    AssistantToolResolver toolResolver) : IAssistantRuntime, IAssistantGuardedToolResolver
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

public static class AssistantCompositionServiceCollectionExtensions
{
    public static IServiceCollection AddAssistantComposition(this IServiceCollection services)
    {
        _ = services.AddAgentComposition<AssistantCompositionContext>(
            new AssistantCapabilityPack(AssistantCapabilityKeys.Appointments),
            new AssistantCapabilityPack(AssistantCapabilityKeys.AppointmentReads),
            new AssistantCapabilityPack(AssistantCapabilityKeys.Prescriptions),
            new AssistantCapabilityPack(AssistantCapabilityKeys.PrescriptionReads),
            new AssistantCapabilityPack(AssistantCapabilityKeys.PatientReminderReads),
            new AssistantCapabilityPack(AssistantCapabilityKeys.ClinicPatientReads),
            new AssistantCapabilityPack(AssistantCapabilityKeys.SeaPatientReads),
            new AssistantCapabilityPack(AssistantCapabilityKeys.ClinicalEvents),
            new AssistantCapabilityPack(AssistantCapabilityKeys.SeaManagement),
            new AssistantCapabilityPack(AssistantCapabilityKeys.Operations),
            new ClinicIntelligenceCapabilityPack());
        _ = services.AddTransient<AssistantExecutionContextFactory>();
        _ = services.AddTransient<AssistantManifestComposer>();
        _ = services.AddTransient<AssistantResultParser>();
        _ = services.AddTransient<AssistantConversationRunner>();
        _ = services.AddTransient<AssistantToolResolver>();
        _ = services.AddTransient<AssistantRuntime>();
        _ = services.AddTransient<IAssistantRuntime>(sp => sp.GetRequiredService<AssistantRuntime>());
        _ = services.AddTransient<IAssistantGuardedToolResolver>(sp => sp.GetRequiredService<AssistantRuntime>());
        return services;
    }
}
