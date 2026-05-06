using MediatR;
using Vitarish.Clinical.Domain.Models;

namespace Vitarish.Clinical.Application.Commands;

public sealed record SignClinicalNoteCommand(
    Guid WorkflowId,
    string NoteDocumentRef,
    string ActorId) : IRequest<ClinicalActionResult>;
