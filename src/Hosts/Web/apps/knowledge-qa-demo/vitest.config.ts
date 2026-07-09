import { defineConfig } from "vitest/config";
import path from "path";

export default defineConfig({
  esbuild: {
    jsx: "automatic",
  },
  test: {
    environment: "jsdom",
    environmentOptions: {
      jsdom: {
        url: "https://localhost:3001/",
      },
    },
    globals: true,
    setupFiles: ["./src/__tests__/setup.ts"],
    css: false,
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
    },
  },
  define: {
    "process.env.NEXT_PUBLIC_API_BASE_URL": JSON.stringify(
      "https://localhost:7272"
    ),
  },
});
