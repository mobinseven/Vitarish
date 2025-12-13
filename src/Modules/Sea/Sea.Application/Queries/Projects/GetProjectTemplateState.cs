namespace Vitarish.Sea.Application.Queries.Projects;


public record GetProjectTemplateStateQuery(Guid Id) : IRequest<bool>;
