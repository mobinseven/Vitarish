"use client";
import{use}from"react";import{ClinicServiceProviderDetailPage}from"@vitarish/clinic-web";import{useClinicNavigation}from"@/components/clinic-navigation";
export default function ClinicServiceProviderDetailRoute({params}:{params:Promise<{serviceProviderId:string}>}){const{serviceProviderId}=use(params);return <ClinicServiceProviderDetailPage serviceProviderId={serviceProviderId} onNavigate={useClinicNavigation()}/>;}
