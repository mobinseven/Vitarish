import { defineConfig } from "orval";

const specsDir = "../specs";
const sharedSpecsDir = "../../../../../src/api/specs";

export default defineConfig({
  sea: {
    input: { target: `${specsDir}/Main_sea.json` },
    output: {
      target: "../generated/sea/sea.ts",
      schemas: "../generated/sea/model",
      client: "react-query",
      override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } },
    },
  },
  // File attachment content is uploaded to Files; the attachment keeps only the stored file id.
  files: {
    input: { target: `${sharedSpecsDir}/Main_files.json` },
    output: {
      target: "../generated/files/files.ts",
      schemas: "../generated/files/model",
      client: "react-query",
      override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } },
    },
  },
});
