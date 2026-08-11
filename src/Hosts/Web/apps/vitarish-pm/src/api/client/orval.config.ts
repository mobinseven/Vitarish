import { defineConfig } from "orval";

export default defineConfig({
  projectManagement: {
    input: { target: "../specs/Main_projectmanagement.json" },
    output: {
      target: "../generated/project-management/projectManagement.ts",
      schemas: "../generated/project-management/model",
      clean: true,
      client: "react-query",
      override: { mutator: { path: "./apiFetch.ts", name: "apiFetch" } },
    },
  },
});
