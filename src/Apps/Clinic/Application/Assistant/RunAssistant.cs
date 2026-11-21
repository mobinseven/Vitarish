using MediatR;
using Vitarish.Intelligence.Application.Agents;

namespace Vitarish.Clinic.Application.Assistant;

/// <param name="Observer">Reports a streamed turn; null for a buffered turn.</param>
public sealed record RunAssistant(AssistantContext Context, AssistantInput Input, IAgentTurnObserver? Observer = null)
    : IRequest<AssistantResult>;
