import type {
  AttachmentReadDto,
  DatabaseStatisticsReadDto,
  EventReadDto,
  ProcedureReadDto,
  ProjectReadDto,
  RoleReadDto,
  SeaGetAttachmentCount200,
  StatsReadDto,
  UserRoleReadDto,
} from "../api/generated/sea/model";
import type {
  SeaAttachmentReadDto,
  SeaEventReadDto,
  SeaMetadata,
  SeaProcedureReadDto,
  SeaProjectReadDto,
  SeaRoleReadDto,
  SeaStatisticsDto,
  SeaStatsReadDto,
  SeaTrackedReadFields,
  SeaUserRoleReadDto,
} from "../models/seaStatModels";

type IntegerLike = number | string | null | undefined;
type OptionalString = string | null | undefined;

function requireString(value: OptionalString, field: string): string {
  if (typeof value !== "string") {
    throw new Error(`SEA contract violation: ${field} is required.`);
  }

  return value;
}

function requireBoolean(value: boolean | null | undefined, field: string): boolean {
  if (typeof value !== "boolean") {
    throw new Error(`SEA contract violation: ${field} is required.`);
  }

  return value;
}

function optionalString(value: OptionalString): string | null {
  return value ?? null;
}

function optionalMetadata(value: SeaMetadata | null | undefined): SeaMetadata | null {
  return value ?? null;
}

export function mapSeaInteger(value: IntegerLike, field: string): number {
  if (typeof value !== "number" && typeof value !== "string") {
    throw new Error(`SEA contract violation: ${field} is required.`);
  }

  if (typeof value === "string" && value.trim() === "") {
    throw new Error(`SEA contract violation: ${field} must be a valid integer string.`);
  }

  const parsed = typeof value === "number" ? value : Number(value);
  if (!Number.isSafeInteger(parsed)) {
    throw new Error(`SEA contract violation: ${field} must be an integer.`);
  }

  return parsed;
}

function mapStats(dto: StatsReadDto | null | undefined, field: string): SeaStatsReadDto | null {
  if (dto == null) {
    return null;
  }

  return {
    state: dto.state ?? null,
    procedures: mapSeaInteger(dto.procedures, `${field}.procedures`),
    events: mapSeaInteger(dto.events, `${field}.events`),
    committedEvents: mapSeaInteger(dto.committedEvents, `${field}.committedEvents`),
    eventItems: mapSeaInteger(dto.eventItems, `${field}.eventItems`),
    newItems: mapSeaInteger(dto.newItems, `${field}.newItems`),
    unread: requireBoolean(dto.unread, `${field}.unread`),
    lastUpdate: optionalString(dto.lastUpdate),
  };
}

function mapTrackedFields(dto: {
  id?: string;
  createdAt?: string;
  createdBy?: OptionalString;
  updatedAt?: OptionalString;
  updatedBy?: OptionalString;
  deletedAt?: OptionalString;
  deletedBy?: OptionalString;
  isDeleted?: boolean;
  metadata?: SeaMetadata | null;
  metadataValues?: SeaMetadata | null;
} | null | undefined, field: string): SeaTrackedReadFields {
  if (!dto) {
    throw new Error(`SEA contract violation: ${field} DTO is required.`);
  }

  return {
    id: requireString(dto.id, `${field}.id`),
    createdAt: requireString(dto.createdAt, `${field}.createdAt`),
    createdBy: optionalString(dto.createdBy),
    updatedAt: optionalString(dto.updatedAt),
    updatedBy: optionalString(dto.updatedBy),
    deletedAt: optionalString(dto.deletedAt),
    deletedBy: optionalString(dto.deletedBy),
    isDeleted: requireBoolean(dto.isDeleted, `${field}.isDeleted`),
    metadata: optionalMetadata(dto.metadata),
    metadataValues: optionalMetadata(dto.metadataValues),
  };
}

export function mapSeaProject(dto: ProjectReadDto): SeaProjectReadDto {
  return {
    ...mapTrackedFields(dto, "project"),
    name: requireString(dto.name, "project.name"),
    description: requireString(dto.description, "project.description"),
    avatarFileId: optionalString(dto.avatarFileId),
    identificationCode: optionalString(dto.identificationCode),
    template: requireBoolean(dto.template, "project.template"),
    stats: mapStats(dto.stats, "project.stats"),
    userIds: dto.userIds ?? [],
  };
}

export function mapSeaProcedure(dto: ProcedureReadDto): SeaProcedureReadDto {
  return {
    ...mapTrackedFields(dto, "procedure"),
    projectId: requireString(dto.projectId, "procedure.projectId"),
    name: requireString(dto.name, "procedure.name"),
    description: optionalString(dto.description),
    stats: mapStats(dto.stats, "procedure.stats"),
  };
}

export function mapSeaEvent(dto: EventReadDto): SeaEventReadDto {
  return {
    ...mapTrackedFields(dto, "event"),
    projectId: requireString(dto.projectId, "event.projectId"),
    procedureId: requireString(dto.procedureId, "event.procedureId"),
    previousEventId: optionalString(dto.previousEventId),
    committedAt: optionalString(dto.committedAt),
    committedBy: requireString(dto.committedBy, "event.committedBy"),
    title: requireString(dto.title, "event.title"),
    description: requireString(dto.description, "event.description"),
    isCommitted: requireBoolean(dto.isCommitted, "event.isCommitted"),
    stats: mapStats(dto.stats, "event.stats"),
  };
}

export function mapSeaAttachment(dto: AttachmentReadDto): SeaAttachmentReadDto {
  return {
    ...mapTrackedFields(dto, "attachment"),
    eventId: requireString(dto.eventId, "attachment.eventId"),
    content: requireString(dto.content, "attachment.content"),
    contentType: requireString(dto.contentType, "attachment.contentType"),
    contentThumbnail: optionalString(dto.contentThumbnail),
    size: mapSeaInteger(dto.size, "attachment.size"),
  };
}

export function mapSeaRole(dto: RoleReadDto): SeaRoleReadDto {
  return {
    ...mapTrackedFields(dto, "role"),
    name: requireString(dto.name, "role.name"),
  };
}

export function mapSeaUserRole(dto: UserRoleReadDto): SeaUserRoleReadDto {
  if (!dto) {
    throw new Error("SEA contract violation: userRole DTO is required.");
  }

  if (dto.type == null) {
    throw new Error("SEA contract violation: userRole.type is required.");
  }

  return {
    ...mapTrackedFields(dto, "userRole"),
    roleId: requireString(dto.roleId, "userRole.roleId"),
    type: dto.type,
  };
}

export function mapSeaAttachmentCount(count: SeaGetAttachmentCount200): number {
  return mapSeaInteger(count, "attachment.count");
}

export function mapSeaStatistics(dto: DatabaseStatisticsReadDto): SeaStatisticsDto {
  if (!dto) {
    throw new Error("SEA contract violation: statistics DTO is required.");
  }

  return {
    projects: mapSeaInteger(dto.projects, "statistics.projects"),
    procedures: mapSeaInteger(dto.procedures, "statistics.procedures"),
    events: mapSeaInteger(dto.events, "statistics.events"),
    roles: mapSeaInteger(dto.roles, "statistics.roles"),
    attachments: mapSeaInteger(dto.attachments, "statistics.attachments"),
    reminders: mapSeaInteger(dto.reminders, "statistics.reminders"),
  };
}
