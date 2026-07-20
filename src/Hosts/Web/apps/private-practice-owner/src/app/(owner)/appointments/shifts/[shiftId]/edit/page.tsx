"use client";
import{use}from"react";import{OwnerShiftEditPage}from"@vitarish/clinic-web";import{useClinicNavigation}from"@/components/clinic-navigation";
export default function OwnerShiftEditRoute({params}:{params:Promise<{shiftId:string}>}){const{shiftId}=use(params);return <OwnerShiftEditPage shiftId={shiftId} onNavigate={useClinicNavigation()}/>;}
