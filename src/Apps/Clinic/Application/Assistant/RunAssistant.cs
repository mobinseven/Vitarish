using MediatR;

namespace Vitarish.Clinic.Application.Assistant;

public sealed record RunAssistant(AssistantContext Context, AssistantInput Input)
    : IRequest<AssistantResult>;
