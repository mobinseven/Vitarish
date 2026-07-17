import { defineConfig } from "orval";

export default defineConfig({
  intelligence: {
    input: { target: "../specs/Main_intelligence.json" },
    output: {
      target: "../generated/intelligence/intelligence.ts",
      schemas: "../generated/intelligence/model",
      client: "react-query",
      override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } },
    },
  },
});
