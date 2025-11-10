# Linux SPA Routing & Deep-Link Verification

Use this checklist when you deploy the ASP.NET + Vite stack on your Linux host (Nginx, cloudflared, etc.).

1. **Remove legacy `wwwroot/app/**`**  
   - The ASP.NET `BuildSpa` target now copies `FocusDeck.WebApp/dist/**` directly to `src/FocusDeck.Server/wwwroot/`.  
   - Delete `/var/www/focusdeck/wwwroot/app` (or equivalent) on the server so nothing else tries to serve `/app`. A `.gitkeep` already owns the empty folder in the repo.

2. **Configure `/` routing (Nginx/Cloudflare)**  
   - Point `location / { proxy_pass http://127.0.0.1:5000; }`.  
   - Ensure any Cloudflare page rules remove `/app` prefixes; the server expects history fallback at `/`.  
   - Optional: add `proxy_set_header Host $host; proxy_set_header X-Forwarded-For $remote_addr;` so ASP.NET can generate correct links.

3. **Verify deep links**  
   - On the Linux host (or via remote tunnel), open `http://localhost:5000/notes` and `http://localhost:5000/lectures/123`.  
   - Each should return the SPA HTML (200) rather than a 404 from Nginx or the API.  
   - If you see `/app/app/...`, check `vite.config.ts` (the `base` is `/`) and confirm `src/FocusDeck.Server/wwwroot/index.html` is served for fallback routes (`Program.cs` already applies `MapWhen` to non-API paths).

4. **Document configuration**  
   - Save your Nginx (and Cloudflare/tunnel) snippet with the port `5000` highlighted and keep it with `docs/Linux_SPA_Routing_Guide.md` for future reference.

Once these steps are confirmed, mark Phase 1.3 as complete in the roadmap.
