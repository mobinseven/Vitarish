import { useQuery } from "@tanstack/react-query";
import { seaRoleKeys } from "./seaRoleKeys";
import {
  listSeaRoles,
  listSeaUserRoles,
  listSeaEventRoles,
  listSeaEventReadRoles,
  listSeaEventWriteRoles,
} from "../api/seaRolesApi";

export function useSeaRoles() {
  return useQuery({
    queryKey: seaRoleKeys.list(),
    queryFn: listSeaRoles,
  });
}

export function useSeaUserRoles(userId: string) {
  return useQuery({
    queryKey: seaRoleKeys.userRoles(userId),
    queryFn: () => listSeaUserRoles(userId),
    enabled: !!userId,
  });
}

export function useSeaEventRoles(eventId: string) {
  return useQuery({
    queryKey: seaRoleKeys.eventRoles(eventId),
    queryFn: () => listSeaEventRoles(eventId),
    enabled: !!eventId,
  });
}

export function useSeaEventReadRoles(eventId: string) {
  return useQuery({
    queryKey: seaRoleKeys.eventReadRoles(eventId),
    queryFn: () => listSeaEventReadRoles(eventId),
    enabled: !!eventId,
  });
}

export function useSeaEventWriteRoles(eventId: string) {
  return useQuery({
    queryKey: seaRoleKeys.eventWriteRoles(eventId),
    queryFn: () => listSeaEventWriteRoles(eventId),
    enabled: !!eventId,
  });
}
