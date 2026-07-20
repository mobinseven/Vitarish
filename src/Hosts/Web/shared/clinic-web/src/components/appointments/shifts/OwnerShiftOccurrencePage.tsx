"use client";
import { Button } from "@heroui/react";
import type { NavigationIntent } from "../../../navigation";
import { useLocalizedText } from "../../../localization/index";
import { EmptyStateSurface as ClinicEmptyState, ErrorStateSurface as ClinicErrorState, LoadingStateSurface as ClinicLoadingState } from "@vitarish/web-ui";
import { useOwnerShiftDetail } from "../../../hooks/appointments/shifts/useOwnerShifts";
export interface OwnerShiftOccurrencePageProps{shiftId:string;occurrenceDate:string;onNavigate:(intent:NavigationIntent)=>void}
export function OwnerShiftOccurrencePage({shiftId,occurrenceDate,onNavigate}:OwnerShiftOccurrencePageProps){const{t}=useLocalizedText();const query=useOwnerShiftDetail(shiftId);if(query.isLoading)return <ClinicLoadingState label={t("common.states.loading")}/>;if(query.error||!query.data)return <ClinicErrorState title={t("clinic.appointments.shifts.notFound")}/>;return <section className="flex flex-col gap-6"><header><Button variant="ghost" onPress={()=>onNavigate({kind:"ownerShiftDetail",shiftId})}>{t("common.actions.previous")}</Button><h1 className="text-2xl font-bold">{query.data.startTime} - {query.data.endTime}</h1><p className="text-sm text-muted">{occurrenceDate}</p></header><ClinicEmptyState title={t("clinic.appointments.shifts.occurrenceComingSoon")}/></section>}
