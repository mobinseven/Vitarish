"use client";
import{use}from"react";import{ClinicPatientProcedurePage}from"@vitarish/clinic-web";import{useClinicNavigation}from"@/components/clinic-navigation";
export default function ClinicPatientProcedureRoute({params}:{params:Promise<{patientId:string;procedureId:string}>}){const{patientId,procedureId}=use(params);return <ClinicPatientProcedurePage patientId={patientId} procedureId={procedureId} onNavigate={useClinicNavigation()}/>;}
