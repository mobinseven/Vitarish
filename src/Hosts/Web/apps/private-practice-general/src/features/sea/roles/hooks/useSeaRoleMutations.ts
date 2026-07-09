import { useMutation, useQueryClient } from "@tanstack/react-query";
import { seaRoleKeys } from "./seaRoleKeys";
import {
  createSeaRole,
  updateSeaRole,
  deleteSeaRole,
  assignSeaUserRole,
  removeSeaUserRole,
  assignSeaEventRole,
  removeSeaEventRole,
  type SeaRoleWriteDto,
  type SeaUserRoleWriteDto,
  type SeaEventRoleWriteDto,
} from "../api/seaRolesApi";

export function useCreateSeaRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaRoleWriteDto) => createSeaRole(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaRoleKeys.all }),
  });
}

export function useUpdateSeaRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaRoleWriteDto) => updateSeaRole(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaRoleKeys.all }),
  });
}

export function useDeleteSeaRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => deleteSeaRole(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaRoleKeys.all }),
  });
}

export function useAssignSeaUserRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaUserRoleWriteDto) => assignSeaUserRole(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaRoleKeys.all }),
  });
}

export function useRemoveSeaUserRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => removeSeaUserRole(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaRoleKeys.all }),
  });
}

export function useAssignSeaEventRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (data: SeaEventRoleWriteDto) => assignSeaEventRole(data),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaRoleKeys.all }),
  });
}

export function useRemoveSeaEventRole() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (id: string) => removeSeaEventRole(id),
    onSuccess: () => qc.invalidateQueries({ queryKey: seaRoleKeys.all }),
  });
}
