import { useMutation } from "@tanstack/react-query";
import { appointmentsListBookableSlots } from "@/api/generated/appointments/appointments";
import type { BookableSlotQueryRequest } from "@/api/generated/appointments/model";

export function useBookableSlots() {
  return useMutation({
    mutationFn: (request: BookableSlotQueryRequest) =>
      appointmentsListBookableSlots(request),
  });
}
