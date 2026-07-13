"use client";
import{use}from"react";import{ClinicOwnerShiftOccurrencePage}from"@vitarish/clinic-web";import{useClinicNavigation}from"@/components/clinic-navigation";
export default function ClinicOwnerShiftOccurrenceRoute({params}:{params:Promise<{shiftId:string;occurrenceDate:string}>}){const{shiftId,occurrenceDate}=use(params);return <ClinicOwnerShiftOccurrencePage shiftId={shiftId} occurrenceDate={occurrenceDate} onNavigate={useClinicNavigation()}/>;}
