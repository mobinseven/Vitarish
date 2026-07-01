import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listServiceTypes, getServiceType, createServiceType, updateServiceType, deleteServiceType, type ServiceTypeDescriptorDto, type ServiceTypeWriteDto } from "../../appointments/api/appointmentsApi";
import { appointmentKeys } from "../../appointments/hooks/appointmentKeys";

export function useServiceTypes(descriptor: ServiceTypeDescriptorDto = {}) {
  return useQuery({
    queryKey: [...appointmentKeys.serviceTypes, descriptor],
    queryFn: () => listServiceTypes(descriptor),
  });
}

export function useServiceTypeDetail(id: string) {
  return useQuery({
    queryKey: appointmentKeys.serviceTypeDetail(id),
    queryFn: () => getServiceType(id),
    enabled: !!id,
  });
}

export function useCreateServiceType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: ServiceTypeWriteDto) => createServiceType(dto),
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.serviceTypes }),
  });
}

export function useUpdateServiceType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: ServiceTypeWriteDto) => updateServiceType(dto),
    onSuccess: (_result, variables) => {
      qc.invalidateQueries({ queryKey: appointmentKeys.serviceTypes });
      if (variables.id) qc.invalidateQueries({ queryKey: appointmentKeys.serviceTypeDetail(variables.id) });
    },
  });
}

export function useDeleteServiceType() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: deleteServiceType,
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.serviceTypes }),
  });
}
