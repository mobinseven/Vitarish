"use client";

import { UserShiftsList } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function ClinicUserShiftsPage() {
  const navigate = useClinicNavigation();
  return <UserShiftsList onNavigate={navigate} />;
}
