using Vorn.Entities;
using Vorn.Entities.Server.SignalR;
using Vitarish.Sea.Application.Abstractions;
using Vitarish.Sea.Domain.Dto;
using Vitarish.Sea.Domain.Enums;

namespace Vitarish.Sea.Hosting.Hubs;

public sealed class ProjectsHub(IProjectsService service, IEntityUserService entityUserService)
    : EntityServer<ProjectDto, ProjectDescriptorDto, IProjectsService>(service, entityUserService);

public sealed class ProceduresHub(IProceduresService service, IEntityUserService entityUserService)
    : EntityServer<ProcedureDto, ProcedureDescriptorDto, IProceduresService>(service, entityUserService);

public sealed class EventsHub(IEventsService service, IEntityUserService entityUserService)
    : EntityServer<EventDto, EventDescriptorDto, IEventsService>(service, entityUserService);

public sealed class AttachmentsHub(IAttachmentsService service, IEntityUserService entityUserService)
    : EntityServer<AttachmentDto, AttachmentDescriptorDto, IAttachmentsService>(service, entityUserService);

public sealed class EventRolesHub(IEventRolesService service, IEntityUserService entityUserService)
    : EntityServer<EventRoleDto, EventRoleDescriptorDto, IEventRolesService>(service, entityUserService);

public sealed class RolesHub(IRolesService service, IEntityUserService entityUserService)
    : EntityServer<RoleDto, RoleDescriptorDto, IRolesService>(service, entityUserService);

public sealed class UserRolesHub(IUserRolesService service, IEntityUserService entityUserService)
    : EntityServer<UserRoleDto, UserRoleDescriptorDto, IUserRolesService>(service, entityUserService);

public sealed class ReceiptsHub(IReceiptsService service, IEntityUserService entityUserService)
    : EntityServer<ReceiptDto, ReceiptDescriptorDto, IReceiptsService>(service, entityUserService);

public sealed class RemindersHub(IRemindersService service, IEntityUserService entityUserService)
    : EntityServer<ReminderDto, ReminderDescriptorDto, IRemindersService>(service, entityUserService);

public sealed class UsageConfigurationHub(IUsageConfigurationService service, IEntityUserService entityUserService)
    : EntityServer<UsageConfigurationDto, UsageConfigurationDescriptorDto, IUsageConfigurationService>(service, entityUserService);
