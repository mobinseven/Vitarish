import { defineConfig } from "orval";

const specsDir = "../../../../../src/api/specs";

export default defineConfig({
  platform: {
    input: {
      target: `${specsDir}/Main_platform.json`,
    },
    output: {
      target: "../generated/platform/platform.ts",
      schemas: "../generated/platform/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  parties: {
    input: { target: `${specsDir}/Main_parties.json` },
    output: { target: "../generated/parties/parties.ts", schemas: "../generated/parties/model", client: "react-query", override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } } },
  },
  landing: {
    input: { target: `${specsDir}/Main_landing.json` },
    output: { target: "../generated/landing/landing.ts", schemas: "../generated/landing/model", client: "react-query", override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } } },
  },
});
