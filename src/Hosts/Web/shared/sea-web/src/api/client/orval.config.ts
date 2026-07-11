import { defineConfig } from "orval";

const specsDir = "../specs";

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
});
