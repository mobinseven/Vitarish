using MediatR;
using Vitarish.Medical.Application.Abstractions;
using Vitarish.Modules.Base;

namespace Vitarish.Medical.Interface.Components.Base;

public class MedicalBase : TenantCommonBase
{
    [Inject] protected IMedicalQueryService QueryService { get; set; } = default!;
    [Inject] protected IMediator Mediator { get; set; } = default!;
}
