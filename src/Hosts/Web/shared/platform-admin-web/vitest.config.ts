import { defineConfig } from "vitest/config";

export default defineConfig({
  test: {
    environment: "jsdom",
    globals: true,
  },
  define: {
    "process.env.NEXT_PUBLIC_API_BASE_URL": JSON.stringify("https://localhost:7272"),
  },
});
