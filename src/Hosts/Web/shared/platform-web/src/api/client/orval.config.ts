import { defineConfig } from "orval";

const specsDir = "../specs";

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
});
