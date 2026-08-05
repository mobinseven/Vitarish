import { defineConfig } from "orval";

// Main host OpenAPI generation writes module documents to the Web host's
// canonical specs directory. Keep auth-web generation on that source rather
// than maintaining a second stale copy.
const specsDir = "../../../../../src/api/specs";

export default defineConfig({
  authentication: {
    input: {
      target: `${specsDir}/Main_authentication.json`,
    },
    output: {
      target: "../generated/authentication/authentication.ts",
      schemas: "../generated/authentication/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
  users: {
    input: {
      target: `${specsDir}/Main_users.json`,
    },
    output: {
      target: "../generated/users/users.ts",
      schemas: "../generated/users/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
});
