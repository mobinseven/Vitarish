"use client";
import{use}from"react";import{ClinicPatientProcedureEventPage}from"@vitarish/clinic-web";import{useClinicNavigation}from"@/components/clinic-navigation";
export default function ClinicPatientProcedureEventRoute({params}:{params:Promise<{patientId:string;procedureId:string;eventId:string}>}){const{patientId,procedureId,eventId}=use(params);return <ClinicPatientProcedureEventPage patientId={patientId} procedureId={procedureId} eventId={eventId} onNavigate={useClinicNavigation()}/>;}
