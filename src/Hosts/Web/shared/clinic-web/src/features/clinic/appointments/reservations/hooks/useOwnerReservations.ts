import { useQuery, useMutation, useQueryClient } from "@tanstack/react-query";
import { listReservations, getReservation, createReservation, updateReservationStatus, deleteReservation, type ReservationDescriptorDto } from "../../api/appointmentsApi";
import { appointmentKeys } from "../../hooks/appointmentKeys";

export function useOwnerReservations(descriptor: ReservationDescriptorDto = {}) {
  return useQuery({
    queryKey: [...appointmentKeys.reservations, descriptor],
    queryFn: () => listReservations(descriptor),
  });
}

export function useOwnerReservationDetail(reservationId: string) {
  return useQuery({
    queryKey: appointmentKeys.reservationDetail(reservationId),
    queryFn: () => getReservation(reservationId),
    enabled: !!reservationId,
  });
}

export function useCreateReservation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: createReservation,
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.reservations }),
  });
}

export function useUpdateReservationStatus() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: ({ id, status }: { id: string; status: number }) => updateReservationStatus(id, status),
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.reservations }),
  });
}

export function useDeleteReservation() {
  const qc = useQueryClient();
  return useMutation({
    mutationFn: deleteReservation,
    onSuccess: () => qc.invalidateQueries({ queryKey: appointmentKeys.reservations }),
  });
}
