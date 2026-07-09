import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaAttachmentKeys } from "./seaAttachmentKeys";
import {
  createSeaTextAttachment,
  updateSeaTextAttachment,
  createSeaFileAttachment,
  deleteSeaAttachment,
  type SeaAttachmentWriteDto,
} from "../api/seaAttachmentsApi";
import { seaEventKeys } from "../../events/hooks/seaEventKeys";

export function useCreateSeaTextAttachment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaAttachmentWriteDto) => createSeaTextAttachment(data),
    onSuccess: (_result, variables) => {
      qc.invalidateQueries({ queryKey: seaAttachmentKeys.all });
      qc.invalidateQueries({ queryKey: seaEventKeys.detail(variables.eventId) });
    },
  });
}

export function useUpdateSeaTextAttachment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaAttachmentWriteDto) => updateSeaTextAttachment(data),
    onSuccess: (_result, variables) => {
      qc.invalidateQueries({ queryKey: seaAttachmentKeys.all });
      qc.invalidateQueries({ queryKey: seaEventKeys.detail(variables.eventId) });
    },
  });
}

export function useCreateSeaFileAttachment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaAttachmentWriteDto) => createSeaFileAttachment(data),
    onSuccess: (_result, variables) => {
      qc.invalidateQueries({ queryKey: seaAttachmentKeys.all });
      qc.invalidateQueries({ queryKey: seaEventKeys.detail(variables.eventId) });
    },
  });
}

export function useDeleteSeaAttachment() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteSeaAttachment(id),
    onSuccess: () => {
      qc.invalidateQueries({ queryKey: seaAttachmentKeys.all });
    },
  });
}
