"use client";

import { OwnerShiftsList } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicOwnerShiftsPage() {
  const navigate = useClinicNavigation();
  return <OwnerShiftsList onNavigate={navigate} />;
}
