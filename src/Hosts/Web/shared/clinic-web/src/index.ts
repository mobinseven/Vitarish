// Components
export { ClinicEmptyState, ClinicErrorState, ClinicLoadingState } from "./features/clinic/components/ClinicStateSurface";
// ClinicDetailGrid promoted to @vitarish/web-ui as DetailGrid; re-export for backward compat
export { DetailGrid as ClinicDetailGrid } from "@vitarish/web-ui";
export type { DetailGridItem as ClinicDetailItem } from "@vitarish/web-ui";
export { ClinicNavigationGrid } from "./features/clinic/components/ClinicNavigationGrid";
export type { ClinicNavigationItem } from "./features/clinic/components/ClinicNavigationGrid";

// Hooks
export { useOwnerShifts, useOwnerShiftDetail, useCreateShift, useUpdateShift, useDeleteShift } from "./features/clinic/appointments/shifts/hooks/useOwnerShifts";
export { useOwnerReservations, useOwnerReservationDetail, useCreateReservation, useUpdateReservationStatus, useDeleteReservation } from "./features/clinic/appointments/reservations/hooks/useOwnerReservations";
export { useBookableDates } from "./features/clinic/appointments/hooks/useBookableDates";
export { useBookableSlots } from "./features/clinic/appointments/hooks/useBookableSlots";

// Types
export type { EntityReadDto, ShiftReadDto, ReservationReadDto, ServiceTypeReadDto, ServiceReadDto, ServiceProviderReadDto, ShiftDescriptorDto, ReservationDescriptorDto, ServiceTypeDescriptorDto, ServiceDescriptorDto, ServiceProviderDescriptorDto, AppointmentsCommandResult, ShiftWriteDto, ServiceWriteDto, ServiceTypeWriteDto, ServiceProviderWriteDto } from "./features/clinic/appointments/api/appointmentsApi";
export type { PatientViewDto } from "./features/clinic/types";

// Pages
export { OwnerReservationsAgenda } from "./features/clinic/appointments/agenda/components/OwnerReservationsAgenda";
export { AppointmentsAgenda } from "./features/clinic/appointments/agenda/components/AppointmentsAgenda";

// Agenda hooks
export { useAgendaEvents } from "./features/clinic/appointments/agenda/hooks/useAgendaEvents";

// Patient API
export { getPatientDetails, lookupPatientByIdentificationCode, listPatients, countPatients, searchPatients } from "./features/clinic/api/patientApi";

// Patient hooks
export { usePatientDetail } from "./features/clinic/hooks/usePatientDetail";
export { usePatientList } from "./features/clinic/hooks/usePatientList";
export { usePatientSearch } from "./features/clinic/hooks/usePatientSearch";
export { usePatientCount } from "./features/clinic/hooks/usePatientCount";
export { usePatientCreate } from "./features/clinic/hooks/usePatientCreate";
export { usePatientUpdate } from "./features/clinic/hooks/usePatientUpdate";
export { usePatientDelete } from "./features/clinic/hooks/usePatientDelete";
export { usePatientIdentificationLookup } from "./features/clinic/hooks/usePatientIdentificationLookup";
export { patientKeys } from "./features/clinic/hooks/patientKeys";

// Appointment sub-features
export { OwnerShiftsList } from "./features/clinic/appointments/shifts/components/OwnerShiftsList";
export { OwnerReservationsList } from "./features/clinic/appointments/reservations/components/OwnerReservationsList";
export { AppointmentSettingsDashboard } from "./features/clinic/appointments/settings/components/AppointmentSettingsDashboard";
export { UserShiftsList } from "./features/clinic/user-appointments/shifts/components/UserShiftsList";
export { UserReservationsList } from "./features/clinic/user-appointments/reservations/components/UserReservationsList";

// Dashboard
export { ClinicOwnerDashboard } from "./features/clinic/dashboard/components/ClinicOwnerDashboard";

// Reminders
export { ClinicOwnerRemindersGrid } from "./features/clinic/reminders/components/ClinicOwnerRemindersGrid";
export { useClinicOwnerReminders } from "./features/clinic/reminders/hooks/useClinicOwnerReminders";

// Settings
export { ClinicSettingsDashboard } from "./features/clinic/settings/components/ClinicSettingsDashboard";
export { ClinicSettingsUsersSurface } from "./features/clinic/settings/users/components/ClinicSettingsUsersSurface";
export { ClinicStatisticsDashboard } from "./features/clinic/statistics/components/ClinicStatisticsDashboard";

// Services
export { ServicesPlaceholder } from "./features/clinic/services/components/ServicesPlaceholder";
export { ServiceTypesList } from "./features/clinic/service-types/components/ServiceTypesPlaceholder";
export { ServiceProvidersPlaceholder } from "./features/clinic/service-providers/components/ServiceProvidersPlaceholder";

// Service hooks
export { useServiceDetail, useCreateService, useUpdateService, useDeleteService } from "./features/clinic/services/hooks/useServices";
export { useServiceTypeDetail, useCreateServiceType, useUpdateServiceType, useDeleteServiceType } from "./features/clinic/service-types/hooks/useServiceTypes";
export { useServiceProviderDetail, useCreateServiceProvider, useUpdateServiceProvider, useDeleteServiceProvider } from "./features/clinic/service-providers/hooks/useServiceProviders";

// Patient components
export { PatientCheckInWizard } from "./features/clinic/check-in/components/PatientCheckInWizard";
export { PatientProfileForm } from "./features/clinic/patients/profile/components/PatientProfileForm";
export { PatientOverviewPageSurface } from "./features/clinic/patients/overview/components/PatientOverviewPageSurface";
export { PatientFindOrCreate } from "./features/clinic/patients/find-or-create/components/PatientFindOrCreate";

// Patient hooks
export { usePatientProfileEditor } from "./features/clinic/patients/profile/hooks/usePatientProfileEditor";
export { useCreatePatientProfile } from "./features/clinic/patients/profile/hooks/useCreatePatientProfile";
export { usePatientOverview } from "./features/clinic/patients/overview/hooks/usePatientOverview";

// Patient list/detail pages
export { ClinicOwnerPatientsPage } from "./features/clinic/components/ClinicOwnerPatientsPage";
export { ClinicOwnerPatientDetailPage } from "./features/clinic/components/ClinicOwnerPatientDetailPage";

// Patient procedure components
export { PatientProcedureHeader } from "./features/clinic/patients/procedures/components/PatientProcedureHeader";
export { PatientProcedureEventTimeline } from "./features/clinic/patients/procedures/components/PatientProcedureEventTimeline";
export { PatientProcedureEventForm } from "./features/clinic/patients/procedures/components/PatientProcedureEventForm";
export { PatientProcedureEventDeleteDialog } from "./features/clinic/patients/procedures/components/PatientProcedureEventDeleteDialog";
export { PatientProcedureEventItemsPanel } from "./features/clinic/patients/procedures/components/PatientProcedureEventItemsPanel";
export { PatientProcedureEventAccessPanel } from "./features/clinic/patients/procedures/components/PatientProcedureEventAccessPanel";

// Patient procedure hooks
export { usePatientProcedureDetail } from "./features/clinic/patients/procedures/hooks/usePatientProcedureDetail";
export { usePatientProcedureEventDetail } from "./features/clinic/patients/procedures/hooks/usePatientProcedureEventDetail";
export { usePatientProcedureEvents } from "./features/clinic/patients/procedures/hooks/usePatientProcedureEvents";
export { useCommitPatientProcedureEvent, useUncommitPatientProcedureEvent } from "./features/clinic/patients/procedures/hooks/usePatientProcedureEventMutations";

// Patient profile models
export { emptyFormData } from "./features/clinic/patients/profile/models/PatientProfileForm";
export type { PatientProfileFormData } from "./features/clinic/patients/profile/models/PatientProfileForm";
