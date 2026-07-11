import { defineConfig } from "orval";

const specsDir = "../specs";

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
