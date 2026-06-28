import { defineConfig } from "orval";

export default defineConfig({
  authentication: {
    input: {
      target: "http://localhost:5000/openapi/authentication.json",
    },
    output: {
      target: "./src/api/generated/authentication/authentication.ts",
      schemas: "./src/api/generated/authentication/model",
      client: "react-query",
      override: {
        mutator: {
          path: "./src/api/client/apiFetch.ts",
          name: "apiFetch",
        },
      },
    },
  },
});
