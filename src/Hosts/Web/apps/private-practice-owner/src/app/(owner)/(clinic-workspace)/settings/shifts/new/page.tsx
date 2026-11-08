"use client";
import { OwnerShiftCreatePage } from "@vitarish/clinic";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function OwnerShiftCreateRoute() {
  return <OwnerShiftCreatePage onNavigate={useClinicNavigation()} />;
}
