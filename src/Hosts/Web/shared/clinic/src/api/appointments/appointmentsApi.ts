import { apiFetch } from "../client/apiFetch";

const BASE = "/api/modules/appointments";

// --- Read DTOs ---

export interface EntityReadDto {
  id: string;
  createdAt: string;
  createdBy?: string;
  updatedAt?: string;
  updatedBy?: string;
  deletedAt?: string;
  deletedBy?: string;
  isDeleted: boolean;
}

export interface ReservationReadDto extends EntityReadDto {
  status: number;
  shiftId: string;
  date: string;
  reservingPartyId?: string;
  estimatedSessionLength?: string;
  confirmedAt?: string;
  cancelledAt?: string;
  isHold: boolean;
  expiresAt?: string;
}

export interface ShiftReadDto extends EntityReadDto {
  status: number;
  visibility: number;
  dayOfWeek: number;
  startTime: string;
  endTime: string;
  timeZone: string;
  totalSeats: number;
  bookingOpensBefore?: number;
  bookingCloseBefore?: number;
  serviceProviderId?: string;
  serviceId?: string;
}

export interface ServiceTypeReadDto extends EntityReadDto {
  name: string;
  description?: string;
}

export interface ServiceReadDto extends EntityReadDto {
  name: string;
  description?: string;
  serviceTypeId: string;
  serviceTypeName?: string;
}

export interface ServiceProviderReadDto extends EntityReadDto {
  name: string;
  description?: string;
  linkedUserIds: string[];
}

export interface AppointmentsCommandResult {
  succeeded: boolean;
  entityId?: string;
  error?: { code: number; message: string; validationErrors?: Record<string, string[]> };
}

// --- Descriptor DTOs ---

export interface ReservationDescriptorDto {
  status?: number;
  shiftId?: string;
  dateFrom?: string;
  dateTo?: string;
  reservingPartyId?: string;
  isHold?: boolean;
}

export interface ShiftDescriptorDto {
  status?: number;
  visibility?: number;
  dayOfWeek?: number;
  startTime?: string;
  endTime?: string;
  sortBy?: number;
  descending?: boolean;
  serviceProviderId?: string;
  serviceId?: string;
}

export interface ServiceTypeDescriptorDto {
  name?: string;
}

export interface ServiceDescriptorDto {
  name?: string;
}

export interface ServiceProviderDescriptorDto {
  name?: string;
}

// --- Write DTOs ---

export interface ShiftWriteDto {
  id?: string;
  status?: number;
  visibility?: number;
  dayOfWeek?: number;
  startTime?: string;
  endTime?: string;
  timeZone?: string;
  totalSeats?: number;
  bookingOpensBefore?: number;
  bookingCloseBefore?: number;
  serviceProviderId?: string;
  serviceId?: string;
}

export interface ServiceWriteDto {
  id?: string;
  name: string;
  description?: string;
  serviceTypeId: string;
}

export interface ServiceTypeWriteDto {
  id?: string;
  name: string;
  description?: string;
}

export interface ServiceProviderWriteDto {
  id?: string;
  name: string;
  description?: string;
  linkedUserIds?: string[];
}

// --- API Functions ---

export async function listReservations(descriptor: ReservationDescriptorDto = {}): Promise<ReservationReadDto[]> {
  return apiFetch<ReservationReadDto[]>({
    url: `${BASE}/reservations`,
    method: "POST",
    data: { descriptor },
  });
}

export async function getReservation(id: string): Promise<ReservationReadDto> {
  return apiFetch<ReservationReadDto>({ url: `${BASE}/reservations/${id}`, method: "GET" });
}

export async function createReservation(params: { shiftId: string; date: string; userId: string }): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({
    url: `${BASE}/reservations/create`,
    method: "POST",
    data: {
      ...params,
      estimatedSessionLengthOverride: null,
    },
  });
}

export async function updateReservationStatus(id: string, status: number): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({
    url: `${BASE}/reservations/${id}/status`,
    method: "POST",
    data: { reservationId: id, status },
  });
}

export async function deleteReservation(id: string): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({ url: `${BASE}/reservations/${id}`, method: "DELETE" });
}

export async function listShifts(descriptor: ShiftDescriptorDto = {}): Promise<ShiftReadDto[]> {
  return apiFetch<ShiftReadDto[]>({
    url: `${BASE}/shifts`,
    method: "POST",
    data: { descriptor },
  });
}

export async function getShift(id: string): Promise<ShiftReadDto> {
  return apiFetch<ShiftReadDto>({ url: `${BASE}/shifts/${id}`, method: "GET" });
}

export async function createShift(shift: ShiftWriteDto): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({
    url: `${BASE}/shifts/create`,
    method: "POST",
    data: { shift },
  });
}

export async function updateShift(shift: ShiftWriteDto): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({
    url: `${BASE}/shifts`,
    method: "PUT",
    data: { shift },
  });
}

export async function deleteShift(id: string): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({ url: `${BASE}/shifts/${id}`, method: "DELETE" });
}

export async function listServiceTypes(descriptor: ServiceTypeDescriptorDto = {}): Promise<ServiceTypeReadDto[]> {
  return apiFetch<ServiceTypeReadDto[]>({
    url: `${BASE}/service-types`,
    method: "POST",
    data: { descriptor },
  });
}

export async function getServiceType(id: string): Promise<ServiceTypeReadDto> {
  return apiFetch<ServiceTypeReadDto>({ url: `${BASE}/service-types/${id}`, method: "GET" });
}

export async function createServiceType(dto: ServiceTypeWriteDto): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({
    url: `${BASE}/service-types/create`,
    method: "POST",
    data: { serviceType: dto },
  });
}

export async function updateServiceType(dto: ServiceTypeWriteDto): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({
    url: `${BASE}/service-types`,
    method: "PUT",
    data: { serviceType: dto },
  });
}

export async function deleteServiceType(id: string): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({ url: `${BASE}/service-types/${id}`, method: "DELETE" });
}

export async function listServices(descriptor: ServiceDescriptorDto = {}): Promise<ServiceReadDto[]> {
  return apiFetch<ServiceReadDto[]>({
    url: `${BASE}/services`,
    method: "POST",
    data: { descriptor },
  });
}

export async function getService(id: string): Promise<ServiceReadDto> {
  return apiFetch<ServiceReadDto>({ url: `${BASE}/services/${id}`, method: "GET" });
}

export async function createService(dto: ServiceWriteDto): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({
    url: `${BASE}/services/create`,
    method: "POST",
    data: { service: dto },
  });
}

export async function updateService(dto: ServiceWriteDto): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({
    url: `${BASE}/services`,
    method: "PUT",
    data: { service: dto },
  });
}

export async function deleteService(id: string): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({ url: `${BASE}/services/${id}`, method: "DELETE" });
}

export async function listServiceProviders(descriptor: ServiceProviderDescriptorDto = {}): Promise<ServiceProviderReadDto[]> {
  return apiFetch<ServiceProviderReadDto[]>({
    url: `${BASE}/service-providers`,
    method: "POST",
    data: { descriptor },
  });
}

export async function getServiceProvider(id: string): Promise<ServiceProviderReadDto> {
  return apiFetch<ServiceProviderReadDto>({ url: `${BASE}/service-providers/${id}`, method: "GET" });
}

export async function createServiceProvider(dto: ServiceProviderWriteDto): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({
    url: `${BASE}/service-providers/create`,
    method: "POST",
    data: { serviceProvider: dto },
  });
}

export async function updateServiceProvider(dto: ServiceProviderWriteDto): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({
    url: `${BASE}/service-providers`,
    method: "PUT",
    data: { serviceProvider: dto },
  });
}

export async function deleteServiceProvider(id: string): Promise<AppointmentsCommandResult> {
  return apiFetch<AppointmentsCommandResult>({ url: `${BASE}/service-providers/${id}`, method: "DELETE" });
}
