import type { PatientViewDto } from "../../../api/generated/clinic/model";

export interface PatientProfileFormData {
  firstName: string;
  lastName: string;
  identificationCode: string;
  dateOfBirth: string;
  gender: string;
  placeOfBirth: string;
  fatherName: string;
  education: string;
  occupation: string;
  phone: string;
  email: string;
  address: string;
  emergencyContactName: string;
  emergencyContactPhone: string;
  dateOfSurgery: string;
  height: string;
  weight: string;
  allergies: string;
  currentMedications: string;
  clinicalNotes: string;
  insuranceProvider: string;
  referrer: string;
  visitReason: string;
  tags: string[];
}

export function patientToFormData(patient: PatientViewDto): PatientProfileFormData {
  return {
    firstName: patient.firstName ?? "",
    lastName: patient.lastName ?? "",
    identificationCode: patient.identificationCode ?? "",
    dateOfBirth: patient.dateOfBirth && !patient.dateOfBirth.startsWith("0001-01-01")
      ? patient.dateOfBirth.split("T")[0]
      : "",
    gender: patient.gender ?? "Unspecified",
    placeOfBirth: patient.placeOfBirth ?? "",
    fatherName: patient.fatherName ?? "",
    education: patient.education ?? "Unspecified",
    occupation: patient.occupation ?? "",
    phone: patient.phone ?? "",
    email: patient.email ?? "",
    address: patient.address ?? "",
    emergencyContactName: patient.emergencyContactName ?? "",
    emergencyContactPhone: patient.emergencyContactPhone ?? "",
    dateOfSurgery: patient.dateOfSurgery && !patient.dateOfSurgery.startsWith("0001-01-01")
      ? patient.dateOfSurgery.split("T")[0]
      : "",
    height: patient.height?.toString() ?? "",
    weight: patient.weight?.toString() ?? "",
    allergies: patient.allergies ?? "",
    currentMedications: patient.currentMedications ?? "",
    clinicalNotes: patient.clinicalNotes ?? "",
    insuranceProvider: patient.insuranceProvider ?? "",
    referrer: patient.referrer ?? "",
    visitReason: patient.visitReason ?? "",
    tags: patient.tags ?? [],
  };
}

export function emptyFormData(): PatientProfileFormData {
  return {
    firstName: "",
    lastName: "",
    identificationCode: "",
    dateOfBirth: "",
    gender: "Unspecified",
    placeOfBirth: "",
    fatherName: "",
    education: "Unspecified",
    occupation: "",
    phone: "",
    email: "",
    address: "",
    emergencyContactName: "",
    emergencyContactPhone: "",
    dateOfSurgery: "",
    height: "",
    weight: "",
    allergies: "",
    currentMedications: "",
    clinicalNotes: "",
    insuranceProvider: "",
    referrer: "",
    visitReason: "",
    tags: [],
  };
}

export interface PatientProfileFormErrors {
  firstName?: string;
  lastName?: string;
  identificationCode?: string;
  phone?: string;
  email?: string;
}
