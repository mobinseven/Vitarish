import { defineConfig } from "orval";

const specsDir = "../../../../../src/api/specs";

export default defineConfig({
  appointments: {
    input: { target: `${specsDir}/Main_appointments.json` },
    output: {
      target: "../generated/appointments/appointments.ts",
      schemas: "../generated/appointments/model",
      client: "react-query",
      override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } },
    },
  },
  clinic: {
    input: { target: `${specsDir}/Main_clinic.json` },
    output: {
      target: "../generated/clinic/clinic.ts",
      schemas: "../generated/clinic/model",
      client: "react-query",
      override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } },
    },
  },
  medical: {
    input: { target: `${specsDir}/Main_medical.json` },
    output: {
      target: "../generated/medical/medical.ts",
      schemas: "../generated/medical/model",
      client: "react-query",
      override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } },
    },
  },
});
