import { useMutation } from "@tanstack/react-query";
import { appointmentsListBookableDates } from "@/api/generated/appointments/appointments";
import type { BookableDateQueryRequest } from "@/api/generated/appointments/model";

export function useBookableDates() {
  return useMutation({
    mutationFn: (request: BookableDateQueryRequest) =>
      appointmentsListBookableDates(request),
  });
}
