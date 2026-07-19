"use client";
import{use}from"react";import{ClinicServiceTypeDetailPage}from"@vitarish/clinic-web";import{useClinicNavigation}from"@/components/clinic-navigation";
export default function ClinicServiceTypeDetailRoute({params}:{params:Promise<{serviceTypeId:string}>}){const{serviceTypeId}=use(params);return <ClinicServiceTypeDetailPage serviceTypeId={serviceTypeId} onNavigate={useClinicNavigation()}/>;}
