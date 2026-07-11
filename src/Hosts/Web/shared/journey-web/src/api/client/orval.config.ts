import { defineConfig } from "orval";

const specsDir = "../specs";

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
});
