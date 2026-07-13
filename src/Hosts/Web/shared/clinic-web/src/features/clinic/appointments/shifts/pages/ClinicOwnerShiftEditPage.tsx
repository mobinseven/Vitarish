"use client";
import type { ClinicNavigationIntent } from "../../../../../navigation";
import { useLocalizedText } from "../../../../../localization";
import { ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { ShiftEditor } from "../components/ShiftEditor";
import { useOwnerShiftDetail,useUpdateShift } from "../hooks/useOwnerShifts";
export interface ClinicOwnerShiftEditPageProps{shiftId:string;onNavigate:(intent:ClinicNavigationIntent)=>void}
export function ClinicOwnerShiftEditPage({shiftId,onNavigate}:ClinicOwnerShiftEditPageProps){const{t}=useLocalizedText();const query=useOwnerShiftDetail(shiftId);const mutation=useUpdateShift();if(query.isLoading)return <ClinicLoadingState label={t("common.states.loading")}/>;if(query.error||!query.data)return <ClinicErrorState title={t("clinic.appointments.shifts.notFound")}/>;const shift=query.data;return <section className="flex flex-col gap-6"><h1 className="text-2xl font-bold">{t("clinic.appointments.shifts.editTitle")}</h1><ShiftEditor initialValue={{dayOfWeek:shift.dayOfWeek,startTime:shift.startTime,endTime:shift.endTime,totalSeats:shift.totalSeats,timeZone:shift.timeZone}} isPending={mutation.isPending} failure={mutation.isError?t("clinic.appointments.shifts.saveFailed"):undefined} onCancel={()=>onNavigate({kind:"ownerShiftDetail",shiftId})} onSubmit={value=>mutation.mutate({id:shiftId,...value},{onSuccess:result=>{if(result.succeeded)onNavigate({kind:"ownerShiftDetail",shiftId});}})}/></section>}
