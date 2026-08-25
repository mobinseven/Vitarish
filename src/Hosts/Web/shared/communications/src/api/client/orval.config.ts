import { defineConfig } from "orval";

export default defineConfig({
  communications: {
    input: { target: "../specs/Main_communications.json" },
    output: {
      target: "../generated/communications/communications.ts",
      schemas: "../generated/communications/model",
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
