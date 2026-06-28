import { defineConfig } from "vitest/config";
import path from "path";

export default defineConfig({
  esbuild: {
    jsx: "automatic",
  },
  test: {
    environment: "jsdom",
    globals: true,
    setupFiles: ["./src/__tests__/setup.ts"],
    css: false,
  },
  resolve: {
    alias: {
      "@": path.resolve(__dirname, "./src"),
      "framer-motion": "framer-motion/dist/cjs/index.js",
    },
  },
  define: {
    "process.env.NEXT_PUBLIC_API_BASE_URL": JSON.stringify(
      "http://localhost:5000"
    ),
  },
});
