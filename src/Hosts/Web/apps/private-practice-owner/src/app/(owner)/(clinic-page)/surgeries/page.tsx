"use client";
import { SurgeryCalendarPage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function SurgeryCalendarRoute() {
  return <SurgeryCalendarPage onNavigate={useClinicNavigation()} />;
}
