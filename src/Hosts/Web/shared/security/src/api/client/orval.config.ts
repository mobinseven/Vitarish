import { defineConfig } from "orval";

const specsDir = "../../../../../src/api/specs";

export default defineConfig({
  audit: {
    input: { target: `${specsDir}/Main_audit.json` },
    output: {
      target: "../generated/audit/audit.ts",
      schemas: "../generated/audit/model",
      client: "react-query",
      override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } },
    },
  },
});
