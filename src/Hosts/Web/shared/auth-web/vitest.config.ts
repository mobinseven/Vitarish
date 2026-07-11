import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "jsdom",
    setupFiles: ["./src/__tests__/setup.ts"],
  },
  define: {
    "process.env.NEXT_PUBLIC_API_BASE_URL": JSON.stringify("https://localhost:7272"),
  },
});
