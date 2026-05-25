using Vitarish.Entities.Server.Models;

namespace Vitarish.Medical.Interface.Components.CodeSystems;

public partial class CodeSystemForm : MedicalBase
{
    [Parameter] public EventCallback<Guid> OnSaved { get; set; }
    [Parameter] public EventCallback OnCancelled { get; set; }

    private CodeSystemWriteDto writeDto = new();

    private async Task SaveAsync()
    {
        EntityResult<Guid> result = await Mediator.Send(new RegisterCodeSystemCommand(
            writeDto.SystemKey,
            writeDto.Name,
            writeDto.Authority,
            writeDto.Category,
            writeDto.CanonicalUri,
            writeDto.Description));

        if (result.IsSuccess)
        {
            await OnSaved.InvokeAsync(result.Value);
        }
    }

    private sealed class CodeSystemWriteDto
    {
        public string SystemKey { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Authority { get; set; }
        public CodeSystemCategory Category { get; set; }
        public string? CanonicalUri { get; set; }
        public string? Description { get; set; }
    }
}
