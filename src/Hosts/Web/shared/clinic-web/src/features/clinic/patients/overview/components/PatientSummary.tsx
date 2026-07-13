"use client";
import { DetailGrid } from "@vitarish/web-ui";
import { useLocalizedText, formatDate as fmtDate, type SupportedLocale } from "../../../../../localization";

export interface PatientSummaryModel {
  firstName?: string | null;
  lastName?: string | null;
  name?: string | null;
  identificationCode?: string | null;
  phone?: string | null;
  email?: string | null;
  dateOfBirth?: string | null;
  gender?: string | null;
  insuranceProvider?: string | null;
}
export interface PatientSummaryProps { patient: PatientSummaryModel }
function safeDate(dateString:string|null|undefined,locale:SupportedLocale,fallback:string){
  if(!dateString||dateString.startsWith("0001-01-01"))return fallback;
  return fmtDate(dateString,locale)||fallback;
}
export function PatientSummary({patient}:PatientSummaryProps){
  const{t,locale}=useLocalizedText();const empty=t("common.values.notAvailable");
  const displayName=patient.firstName||patient.lastName?((patient.firstName??"")+" "+(patient.lastName??"")).trim():patient.name||t("clinic.patients.unnamedPatient");
  return <section className="rounded-medium border border-divider p-4"><header className="mb-3"><h2>{displayName}</h2></header><DetailGrid items={[
    {label:t("clinic.patient.fields.identificationCode"),value:patient.identificationCode||empty},
    {label:t("clinic.patient.fields.phone"),value:patient.phone||empty},
    {label:t("clinic.patient.fields.email"),value:patient.email||empty},
    {label:t("clinic.patient.fields.dateOfBirth"),value:safeDate(patient.dateOfBirth,locale,empty)},
    {label:t("clinic.patient.fields.gender"),value:patient.gender==="Unspecified"?empty:patient.gender||empty},
    {label:t("clinic.patient.fields.insuranceProvider"),value:patient.insuranceProvider||empty},
  ]}/></section>;
}
