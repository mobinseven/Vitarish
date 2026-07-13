import { useQuery } from "@tanstack/react-query";
import { seaAttachmentKeys } from "./seaAttachmentKeys";
import { listSeaAttachments, getSeaAttachmentCount } from "../../api/attachments/seaAttachmentsApi";

export function useSeaEventAttachments(eventId: string) {
  return useQuery({
    queryKey: seaAttachmentKeys.list(eventId),
    queryFn: () => listSeaAttachments(eventId),
    enabled: !!eventId,
  });
}

export function useSeaEventAttachmentCount(eventId: string) {
  return useQuery({
    queryKey: seaAttachmentKeys.count(eventId),
    queryFn: () => getSeaAttachmentCount(eventId),
    enabled: !!eventId,
  });
}
