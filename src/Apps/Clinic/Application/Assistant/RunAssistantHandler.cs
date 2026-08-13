using MediatR;
using Vitarish.Clinic.Application.Assistant.Actions;

namespace Vitarish.Clinic.Application.Assistant;

public sealed class RunAssistantHandler(
    IAssistantRuntime runtime) : IRequestHandler<RunAssistant, AssistantResult>
{
    public async Task<AssistantResult> Handle(
        RunAssistant request,
        CancellationToken cancellationToken)
    {
        return await runtime.RunAsync(request.Context, request.Input, cancellationToken);
    }
}
