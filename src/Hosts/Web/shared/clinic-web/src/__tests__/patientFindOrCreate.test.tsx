import type { ReactNode } from "react";
import { QueryClient, QueryClientProvider } from "@tanstack/react-query";
import { cleanup, fireEvent, render, screen, waitFor } from "@testing-library/react";
import { afterEach, describe, expect, it, vi } from "vitest";
import { ApiError } from "@vitarish/web-api-client";
import { PatientFindOrCreate } from "../components/patients/find-or-create/PatientFindOrCreate";
import { emptyFormData } from "../models/patients/profile/PatientProfileForm";
import { toPatientFindOrCreateRequest } from "../hooks/patients/profile/useCreatePatientProfile";
import { LocalizationProvider, clinicMessageCatalogs } from "../localization";
import type { PatientViewDto } from "../api/generated/clinic/model";

const lookupPatient = vi.hoisted(() => vi.fn());

vi.mock("../api/patients/patientApi", async (importOriginal) => ({
  ...(await importOriginal<typeof import("../api/patients/patientApi")>()),
  lookupPatientByIdentificationCode: lookupPatient,
}));

afterEach(() => {
  cleanup();
  lookupPatient.mockReset();
});

const messages = {
  ...clinicMessageCatalogs.en,
  common: { actions: { previous: "Previous" } },
};

function Wrapper({ children }: { children: ReactNode }) {
  const queryClient = new QueryClient({
    defaultOptions: { queries: { retry: false }, mutations: { retry: false } },
  });
  return (
    <QueryClientProvider client={queryClient}>
      <LocalizationProvider locale="en" messages={messages}>
        {children}
      </LocalizationProvider>
    </QueryClientProvider>
  );
}

describe("patient find or create", () => {
  it("notifies the parent when the identification lookup finds a patient", async () => {
    const patient = { id: "patient-1", identificationCode: "1234567890" } as PatientViewDto;
    lookupPatient.mockResolvedValue(patient);
    const onPatientSelected = vi.fn();

    render(<PatientFindOrCreate onPatientSelected={onPatientSelected} />, { wrapper: Wrapper });
    fireEvent.change(screen.getByPlaceholderText("Enter identification code..."), {
      target: { value: " 1234567890 " },
    });
    fireEvent.click(screen.getByRole("button", { name: "Look up patient" }));

    await waitFor(() => expect(onPatientSelected).toHaveBeenCalledWith("patient-1"));
    expect(lookupPatient).toHaveBeenCalledWith({ identificationCode: "1234567890" });
  });

  it("opens a create form prefilled with the missed identification code", async () => {
    lookupPatient.mockRejectedValue(new ApiError({ status: 404, title: "Not found" }));

    render(<PatientFindOrCreate onPatientSelected={vi.fn()} />, { wrapper: Wrapper });
    fireEvent.change(screen.getByPlaceholderText("Enter identification code..."), {
      target: { value: " 1234567890 " },
    });
    fireEvent.click(screen.getByRole("button", { name: "Look up patient" }));

    expect(await screen.findByRole("heading", { name: "New Patient" })).toBeInTheDocument();
    expect(screen.getByLabelText("Identification Code")).toHaveValue("1234567890");
  });

  it("maps every patient profile field into the create command request", () => {
    const request = toPatientFindOrCreateRequest({
      ...emptyFormData(),
      identificationCode: "1234567890",
      firstName: "Ada",
      lastName: "Lovelace",
      dateOfBirth: "1815-12-10",
      dateOfSurgery: "1843-01-01",
      gender: "Female",
      placeOfBirth: "London",
      fatherName: "Lord Byron",
      education: "Professional",
      occupation: "Mathematician",
      phone: "+44 20 0000 0000",
      email: "ada@example.test",
      address: "St James's Square",
      emergencyContactName: "Charles Babbage",
      emergencyContactPhone: "+44 20 1111 1111",
      height: "165",
      weight: "55",
      allergies: "None",
      currentMedications: "None",
      clinicalNotes: "Analytical Engine notes",
      insuranceProvider: "Private",
      referrer: "Charles Babbage",
      visitReason: "Consultation",
    });

    expect(request).toMatchObject({
      identificationCode: "1234567890",
      firstName: "Ada",
      lastName: "Lovelace",
      dateOfBirth: "1815-12-10T00:00:00.000Z",
      dateOfSurgery: "1843-01-01T00:00:00.000Z",
      gender: "Female",
      education: "Professional",
      height: 165,
      weight: 55,
      clinicalNotes: "Analytical Engine notes",
      emergencyContactName: "Charles Babbage",
    });
    expect(Object.keys(request)).toHaveLength(23);
  });
});
