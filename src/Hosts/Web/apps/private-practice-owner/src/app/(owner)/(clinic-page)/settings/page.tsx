"use client";

import { SettingsPage } from "@vitarish/clinic";
import { PracticeTimeZoneSettings } from "@vitarish/journey";
import { useClinicNavigation } from "@/components/clinic-navigation";

export default function SettingsRoute() {
  return (
    <div className="flex flex-col gap-6">
      <SettingsPage onNavigate={useClinicNavigation()} />
      <PracticeTimeZoneSettings />
    </div>
  );
}
