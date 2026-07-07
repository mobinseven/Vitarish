import type { ProgressState, RoleType } from "@/api/generated/sea/model";

export type SeaMetadata = Record<string, unknown>;

export interface SeaStatsReadDto {
  state: ProgressState | null;
  procedures: number;
  events: number;
  committedEvents: number;
  eventItems: number;
  newItems: number;
  unread: boolean;
  lastUpdate: string | null;
}

export interface SeaTrackedReadFields {
  id: string;
  createdAt: string;
  createdBy: string | null;
  updatedAt: string | null;
  updatedBy: string | null;
  deletedAt: string | null;
  deletedBy: string | null;
  isDeleted: boolean;
  metadata: SeaMetadata | null;
  metadataValues: SeaMetadata | null;
}

export interface SeaProjectReadDto extends SeaTrackedReadFields {
  name: string;
  description: string;
  avatarFileId: string | null;
  identificationCode: string | null;
  template: boolean;
  stats: SeaStatsReadDto | null;
  userIds: string[];
}

export interface SeaProcedureReadDto extends SeaTrackedReadFields {
  projectId: string;
  name: string;
  description: string | null;
  stats: SeaStatsReadDto | null;
}

export interface SeaEventReadDto extends SeaTrackedReadFields {
  projectId: string;
  procedureId: string;
  previousEventId: string | null;
  committedAt: string | null;
  committedBy: string;
  title: string;
  description: string;
  isCommitted: boolean;
  stats: SeaStatsReadDto | null;
}

export interface SeaAttachmentReadDto extends SeaTrackedReadFields {
  eventId: string;
  content: string;
  contentType: string;
  contentThumbnail: string | null;
  size: number;
}

export interface SeaRoleReadDto extends SeaTrackedReadFields {
  name: string;
}

export interface SeaUserRoleReadDto extends SeaTrackedReadFields {
  roleId: string;
  type: RoleType;
}

export interface SeaStatisticsDto {
  projects: number;
  procedures: number;
  events: number;
  roles: number;
  attachments: number;
  reminders: number;
}
