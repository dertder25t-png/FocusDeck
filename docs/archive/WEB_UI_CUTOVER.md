## FocusDeck Web UI Cutover – November 13, 2025

- **Summary**: The React/Vite webapp under `src/FocusDeck.WebApp` is now the only UI that the server serves. The legacy static bundle (`app.js`, `styles.css`, etc.) was deleted from the live deployment folder (`/root/focusdeck-server/wwwroot`) so users always land on the modern login-first interface.
- **Build steps executed**:
  1. `npm install --legacy-peer-deps` (needed because `qrcode.react` has not declared React 19 support yet).
  2. `npm run build` inside `src/FocusDeck.WebApp` to produce the latest `dist` bundle.
  3. Synchronized `dist/` into `src/FocusDeck.Server/wwwroot/`, then copied those assets into `/root/focusdeck-server/wwwroot/`.
- **Result**: `wwwroot` now only contains the hashed Vite assets (`index.html`, `vite.svg`, `assets/index-*.{js,css}`) that boot the authenticated SPA. Hitting `/` or any SPA route now loads the new UI and uses the existing `ProtectedRoute` + login flow.
- **Operational note**: Because `wwwroot` is gitignored, these assets do not show up in `git status`. Future deployments should repeat the steps above (or automate them) so the publish output always includes the new bundle.
