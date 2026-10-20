import { defineConfig } from "orval";

const specsDir = "../specs";
// Onboarding calls other modules' operations; their specs are read where Main writes them.
const mainSpecsDir = "../../../../../src/api/specs";

export default defineConfig({
  journey: {
    input: {
      target: `${specsDir}/Main_journey.json`,
    },
    output: {
      target: "../generated/journey/journey.ts",
      schemas: "../generated/journey/model",
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
    input: { target: `${mainSpecsDir}/Main_parties.json` },
    output: { target: "../generated/parties/parties.ts", schemas: "../generated/parties/model", client: "react-query", override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } } },
  },
  authentication: {
    input: { target: `${mainSpecsDir}/Main_authentication.json` },
    output: { target: "../generated/authentication/authentication.ts", schemas: "../generated/authentication/model", client: "react-query", override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } } },
  },
  users: {
    input: { target: `${mainSpecsDir}/Main_users.json` },
    output: { target: "../generated/users/users.ts", schemas: "../generated/users/model", client: "react-query", override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } } },
  },
  files: {
    input: { target: `${mainSpecsDir}/Main_files.json` },
    output: { target: "../generated/files/files.ts", schemas: "../generated/files/model", client: "react-query", override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } } },
  },
});
