namespace Vitarish.Sea.Application.Abstractions;

using System.Threading;
using System.Threading.Tasks;
using Vitarish.Sea.Domain.Dto;

public interface IGodViewService
{
    Task<GodViewPage> GetProjectsAsync(GodViewPageRequest request, CancellationToken cancellationToken);

    Task<GodViewPage> GetProceduresAsync(GodViewPageRequest request, CancellationToken cancellationToken);

    Task<GodViewPage> GetEventsAsync(GodViewPageRequest request, CancellationToken cancellationToken);

    Task<GodViewPage> GetAttachmentsAsync(GodViewPageRequest request, CancellationToken cancellationToken);

    Task<GodViewPage> GetRemindersAsync(GodViewPageRequest request, CancellationToken cancellationToken);
}
