import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listServiceProviders, getServiceProvider, createServiceProvider, updateServiceProvider, deleteServiceProvider, type ServiceProviderDescriptorDto, type ServiceProviderWriteDto } from "../../api/appointments/appointmentsApi";
import { appointmentKeys } from "../appointments/appointmentKeys";

export function useServiceProviders(descriptor: ServiceProviderDescriptorDto = {}) {
  return useQuery({
    queryKey: [...appointmentKeys.serviceProviders, descriptor],
    queryFn: () => listServiceProviders(descriptor),
  });
}

export function useServiceProviderDetail(id: string) {
  return useQuery({
    queryKey: appointmentKeys.serviceProviderDetail(id),
    queryFn: () => getServiceProvider(id),
    enabled: !!id,
  });
}

export function useCreateServiceProvider() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: ServiceProviderWriteDto) => createServiceProvider(dto),
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.serviceProviders }),
  });
}

export function useUpdateServiceProvider() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: (dto: ServiceProviderWriteDto) => updateServiceProvider(dto),
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.serviceProviders }),
  });
}

export function useDeleteServiceProvider() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: deleteServiceProvider,
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.serviceProviders }),
  });
}
