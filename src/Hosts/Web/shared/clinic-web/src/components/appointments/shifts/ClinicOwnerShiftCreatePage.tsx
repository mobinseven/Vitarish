"use client";
import type { ClinicNavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import { ShiftEditor } from "./ShiftEditor";
import { useCreateShift } from "../../../hooks/appointments/shifts/useOwnerShifts";
export interface ClinicOwnerShiftCreatePageProps{onNavigate:(intent:ClinicNavigationIntent)=>void}
export function ClinicOwnerShiftCreatePage({onNavigate}:ClinicOwnerShiftCreatePageProps){const{t}=useLocalizedText();const mutation=useCreateShift();return <section className="flex flex-col gap-6"><header><h1 className="text-2xl font-bold">{t("clinic.appointments.shifts.createTitle")}</h1></header><ShiftEditor initialValue={{dayOfWeek:1,startTime:"09:00",endTime:"10:00",totalSeats:10,timeZone:"UTC"}} isPending={mutation.isPending} failure={mutation.isError?t("clinic.appointments.shifts.saveFailed"):undefined} onCancel={()=>onNavigate({kind:"ownerShiftIndex"})} onSubmit={value=>mutation.mutate(value,{onSuccess:result=>{if(result.succeeded&&result.entityId)onNavigate({kind:"ownerShiftDetail",shiftId:result.entityId});}})}/></section>}
