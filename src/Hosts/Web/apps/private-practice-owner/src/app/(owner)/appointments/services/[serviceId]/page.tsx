"use client";
import{use}from"react";import{ClinicServiceDetailPage}from"@vitarish/clinic-web";import{useClinicNavigation}from"@/components/clinic-navigation";
export default function ClinicServiceDetailRoute({params}:{params:Promise<{serviceId:string}>}){const{serviceId}=use(params);return <ClinicServiceDetailPage serviceId={serviceId} onNavigate={useClinicNavigation()}/>;}
