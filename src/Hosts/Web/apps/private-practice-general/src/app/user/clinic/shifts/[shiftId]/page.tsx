"use client";
import { use } from "react";
import { ClinicUserShiftDetailPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function ClinicUserShiftDetailRoute({params}:{params:Promise<{shiftId:string}>}) { const {shiftId}=use(params); return <ClinicUserShiftDetailPage shiftId={shiftId} onNavigate={useClinicNavigation()} />; }
