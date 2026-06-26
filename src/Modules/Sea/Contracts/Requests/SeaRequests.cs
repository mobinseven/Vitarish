using Vitarish.Sea.Contracts.Responses;

namespace Vitarish.Sea.Contracts.Requests;

public sealed record SeaIdRequest(Guid Id);
public sealed record SeaProjectRequest(ProjectWriteDto Project);
public sealed record SeaProcedureRequest(ProcedureWriteDto Procedure);
public sealed record SeaEventRequest(EventWriteDto Event);
public sealed record SeaAttachmentRequest(AttachmentWriteDto Attachment);
public sealed record SeaRoleRequest(RoleWriteDto Role);
public sealed record SeaUserRoleRequest(UserRoleWriteDto UserRole);
public sealed record SeaEventRoleRequest(EventRoleWriteDto EventRole);
public sealed record SeaReceiptRequest(ReceiptWriteDto Receipt);
public sealed record SeaBatchAttachmentRequest(IReadOnlyList<AttachmentWriteDto> Attachments);
