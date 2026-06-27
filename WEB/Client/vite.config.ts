import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";

declare const process: {
  env: Record<string, string | undefined>;
};

export default defineConfig(() => {
  const serverUrl = process.env.DIGITC2_SERVER_URL ?? "http://127.0.0.1:5188";

  return {
    plugins: [react()],
    server: {
      port: 5173,
      proxy: {
        "/api": serverUrl,
        "/health": serverUrl
      }
    }
  };
});
