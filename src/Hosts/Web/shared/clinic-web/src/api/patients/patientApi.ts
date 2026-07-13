import {
  clinicListPatients,
  clinicCountPatients,
  clinicSearchPatients,
  clinicLookupPatientByIdentificationCode,
  clinicGetPatientDetails,
} from "../generated/clinic/clinic";
import type {
  PatientListRequest,
  PatientCountRequest,
  PatientSearchRequest,
  PatientIdentificationLookupRequest,
} from "../generated/clinic/model";

export async function listPatients(request: PatientListRequest) {
  return clinicListPatients(request);
}

export async function countPatients(request: PatientCountRequest) {
  return clinicCountPatients(request);
}

export async function searchPatients(request: PatientSearchRequest) {
  return clinicSearchPatients(request);
}

export async function lookupPatientByIdentificationCode(
  request: PatientIdentificationLookupRequest
) {
  return clinicLookupPatientByIdentificationCode(request);
}

export async function getPatientDetails(patientId: string) {
  return clinicGetPatientDetails(patientId);
}
