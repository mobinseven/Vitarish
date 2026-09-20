import type { PatientViewDto, PatientSortBy } from "../../api/generated/clinic/model";

export type { PatientViewDto, PatientSortBy };

export interface PatientSearchCriteria {
  search?: string;
  skip?: number;
  take?: number;
  sortBy?: PatientSortBy;
  descending?: boolean;
  tags?: string[];
}

export interface IdentificationLookupState {
  status: "idle" | "searching" | "found" | "not-found" | "error";
  patient: PatientViewDto | null;
  error: string | null;
}

export interface AdvancedSearchCriteria {
  name?: string;
  identificationCode?: string;
  dateOfBirth?: string;
  phone?: string;
  email?: string;
}
