"use client";
import { use } from "react";
import { ClinicOwnerShiftDetailPage } from "@vitarish/clinic-web";
import { useClinicNavigation } from "@/components/clinic-navigation";
export default function ClinicOwnerShiftDetailRoute({params}:{params:Promise<{shiftId:string}>}) { const {shiftId}=use(params); return <ClinicOwnerShiftDetailPage shiftId={shiftId} onNavigate={useClinicNavigation()} />; }
