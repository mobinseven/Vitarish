import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listShifts, getShift, createShift, updateShift, deleteShift, type ShiftDescriptorDto, type ShiftWriteDto } from "../../../api/appointments/appointmentsApi";
import { appointmentKeys } from "../appointmentKeys";

export function useOwnerShifts(descriptor: ShiftDescriptorDto = {}) {
  return useQuery({
    queryKey: [...appointmentKeys.shifts, descriptor],
    queryFn: () => listShifts(descriptor),
  });
}

export function useOwnerShiftDetail(shiftId: string) {
  return useQuery({
    queryKey: appointmentKeys.shiftDetail(shiftId),
    queryFn: () => getShift(shiftId),
    enabled: !!shiftId,
  });
}

export function useCreateShift() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (shift: ShiftWriteDto) => createShift(shift),
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.shifts }),
  });
}

export function useUpdateShift() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (shift: ShiftWriteDto) => updateShift(shift),
    onSuccess: (_result, variables) => {
      qc.invalidateQueries({ queryKey: appointmentKeys.shifts });
      if (variables.id) qc.invalidateQueries({ queryKey: appointmentKeys.shiftDetail(variables.id) });
    },
  });
}

export function useDeleteShift() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: deleteShift,
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.shifts }),
  });
}
