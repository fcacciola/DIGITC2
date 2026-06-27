import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
export default defineConfig(function () {
    var _a;
    var serverUrl = (_a = process.env.DIGITC2_SERVER_URL) !== null && _a !== void 0 ? _a : "http://127.0.0.1:5188";
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
