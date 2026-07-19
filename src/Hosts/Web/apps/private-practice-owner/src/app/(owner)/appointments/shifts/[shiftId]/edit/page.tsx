"use client";
import{use}from"react";import{ClinicOwnerShiftEditPage}from"@vitarish/clinic-web";import{useClinicNavigation}from"@/components/clinic-navigation";
export default function ClinicOwnerShiftEditRoute({params}:{params:Promise<{shiftId:string}>}){const{shiftId}=use(params);return <ClinicOwnerShiftEditPage shiftId={shiftId} onNavigate={useClinicNavigation()}/>;}
