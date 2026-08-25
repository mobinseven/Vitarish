import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listServices, getService, createService, updateService, deleteService, type ServiceDescriptorDto, type ServiceWriteDto } from "../../api/appointments/appointmentsApi";
import { appointmentKeys } from "../appointments/appointmentKeys";

export function useServices(descriptor: ServiceDescriptorDto = {}) {
  return useQuery({
    queryKey: [...appointmentKeys.services, descriptor],
    queryFn: () => listServices(descriptor),
  });
}

export function useServiceDetail(id: string) {
  return useQuery({
    queryKey: appointmentKeys.serviceDetail(id),
    queryFn: () => getService(id),
    enabled: !!id,
  });
}

export function useCreateService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: ServiceWriteDto) => createService(dto),
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.services }),
  });
}

export function useUpdateService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: ServiceWriteDto) => updateService(dto),
    onSuccess: (_result, variables) => {
      qc.invalidateQueries({ queryKey: appointmentKeys.services });
      if (variables.id) qc.invalidateQueries({ queryKey: appointmentKeys.serviceDetail(variables.id) });
    },
  });
}

export function useDeleteService() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: deleteService,
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.services }),
  });
}
