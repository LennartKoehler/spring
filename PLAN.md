# Smart Home Server — Project Plan

Raspberry Pi runs the backend (plain .NET console app + SQLite). React web app (PWA) connects over WiFi and lets the user view/interact with smart home devices from their phone.

**No ASP.NET.** The HTTP server is built from scratch using .NET's built-in `HttpListener` — you write the REST router, JSON handling, and static file serving yourself. Maximum learning.

---

## Tech Stack

| Layer      | Technology                              | Why                                             |
|------------|-----------------------------------------|-------------------------------------------------|
| Backend    | C# / plain .NET 8+ console app          | Runs on Pi (ARM64), no framework magic          |
| HTTP       | `System.Net.HttpListener` (built-in)    | Raw HTTP server, you build routing yourself     |
| JSON       | `System.Text.Json` (built-in)           | Already part of .NET, no extra library          |
| Database   | SQLite + `Microsoft.Data.Sqlite`        | Single-file DB, raw SQL (learn SQL directly)    |
| Frontend   | React (Vite) + TypeScript               | Industry standard, huge community               |
| Frontend   | PWA (installable on phone)              | Native-like app, no app store needed            |
| Styling    | Tailwind CSS                            | Fast UI development, no CSS grind               |
| Deployment | Console app serves API + static files   | One process on Pi, self-built server            |
| Access     | local WiFi first, Tailscale for remote  | VPN access without exposing the Pi to internet  |
| Hardware   | System.Device.Gpio (later)              | C# GPIO library for LED/sensors                 |

Optional later: MQTT (Mosquitto) for real IoT device communication.

---

## Final Project Structure

```
smart-home/
├── backend/                      # Plain .NET console app
│   ├── Program.cs                # Start HttpListener, route requests
│   ├── Http/
│   │   ├── HttpServer.cs         # HttpListener loop
│   │   ├── Router.cs             # Match method+path → handler
│   │   └── StaticFiles.cs        # Serve React build output
│   ├── Api/
│   │   └── DevicesApi.cs         # Device endpoint handlers (JSON)
│   ├── Models/
│   │   ├── Device.cs
│   │   └── Room.cs
│   ├── Data/
│   │   └── Database.cs           # SQLite connection + init + queries
│   └── smart-home.csproj
└── frontend/                     # React app (Vite)
    ├── src/
    │   ├── api/                  # fetch wrappers calling the backend
    │   ├── components/           # DeviceCard, LightSwitch, Dashboard...
    │   ├── pages/
    │   ├── App.tsx
    │   └── main.tsx
    └── package.json
```

In production the React app is built to static files and served by your own `HttpListener` server:
`http://<pi-ip>:5000` → React UI, `http://<pi-ip>:5000/api/...` → API.

---

## Phases

### Phase 0 — Environment Setup
- [ ] Install .NET 8+ SDK on the Pi (`sudo apt install dotnet-sdk-8.0` or dotnet-install script)
- [ ] Install .NET SDK on dev machine (develop here, deploy to Pi)
- [ ] Install Node.js (20+) on dev machine
- [ ] Learn: what a SDK is, `dotnet` CLI basics (`dotnet new`, `dotnet run`)

### Phase 1 — HTTP Server from Scratch (no database)
- [ ] Scaffold console app: `dotnet new console -n backend`
- [ ] Start `HttpListener` on port 5000, accept requests in a loop
- [ ] Build a minimal router: match HTTP method + path → handler function
- [ ] Implement device endpoints (in-memory list first, hardcoded data):
  - `GET    /api/devices`        → list all (JSON)
  - `GET    /api/devices/{id}`   → one device
  - `POST   /api/devices`        → create (parse JSON body with `System.Text.Json`)
  - `PUT    /api/devices/{id}`   → update (toggle on/off)
  - `DELETE /api/devices/{id}`   → remove
- [ ] Return correct status codes (200/201/404/400) yourself
- [ ] Test with curl and Postman (no Swagger — that's an ASP.NET feature)
- [ ] Learn: HTTP request/response anatomy, headers, body, status codes, JSON, routing, REST design, threading (`AcceptAsync` loop)

### Phase 2 — Database (raw SQLite)
- [ ] Add `Microsoft.Data.Sqlite` NuGet package
- [ ] Create `devices` table on startup (`CREATE TABLE IF NOT EXISTS ...`)
- [ ] Replace hardcoded list with SQL queries (`SELECT`, `INSERT`, `UPDATE`, `DELETE`)
- [ ] Always use parameterized queries (`@Name`) — never string concatenation
- [ ] Learn: SQL basics, schemas, primary keys, parameterized queries, SQL injection (why it matters)

### Phase 3 — Frontend Fundamentals
- [ ] Scaffold React app: `npm create vite@latest frontend -- --template react-ts`
- [ ] Learn plain HTML/CSS/JS basics if needed (DOM, events, fetch)
- [ ] Learn React basics:
  - Components, JSX, props
  - State (`useState`), effects (`useEffect`)
  - Rendering lists, conditional rendering
- [ ] Build a static dashboard UI (fake data): device cards with on/off toggle
- [ ] Learn: component thinking, controlled state, list rendering (keys)

### Phase 4 — Wire Frontend to Backend
- [ ] Call the real API with `fetch()` in `useEffect`
- [ ] Handle loading / error / success states
- [ ] Optimistic UI or refetch after toggling a device
- [ ] Add form to create/delete devices
- [ ] Vite dev proxy: `/api` → `http://localhost:5000` (avoids CORS in dev)
- [ ] Bonus: implement CORS headers in your own server and skip the proxy — see both sides
- [ ] Learn: async JS, promises, fetch, CORS, why proxies exist

### Phase 5 — Serve the Frontend Yourself + Deploy
- [ ] `npm run build` → React outputs static files to `dist/`
- [ ] Write static file handler in your server: map URL paths to files in `dist/`
- [ ] Correct MIME types (`text/html`, `application/javascript`, `text/css`), serve `index.html` as fallback (SPA routing)
- [ ] Publish app: `dotnet publish -r linux-arm64` → copy to Pi
- [ ] Run as systemd service (auto-start on boot)
- [ ] Access from phone on same WiFi: `http://<pi-ip>:5000`
- [ ] Learn: builds vs dev servers, MIME types, SPA fallback, self-contained publishing, systemd, basic networking (IP, ports)

### Phase 6 — Polish & PWA
- [ ] Make React app a PWA (vite-plugin-pwa): installable, icon, manifest
- [ ] Add simple auth: login endpoint in your own router, issue token yourself (JWT via `System.IdentityModel.Tokens.Jwt` package, or hand-rolled signed token)
- [ ] Protect API endpoints: check token in router middleware
- [ ] Hash passwords (BCrypt.Net NuGet package), never store plaintext
- [ ] Learn: auth concepts, tokens, password hashing, middleware patterns

### Phase 7 — Real Hardware (Stretch)
- [ ] Install `System.Device.Gpio` NuGet package
- [ ] Control a real LED/relay via GPIO from your console app
- [ ] Read DHT11/DHT22 temperature/humidity sensor
- [ ] Show live sensor data on the dashboard (polling or Server-Sent Events — implement SSE by hand in your server, great learning)
- [ ] Learn: GPIO pins, pull-up/down, background work (`Task.Run` loops, `PeriodicTimer`)

### Phase 8 — Extras (Stretch)
- [ ] Remote access: install Tailscale on Pi + phone → connect from anywhere
- [ ] Schedule actions (e.g. light on at sunset) — store jobs in DB, run with `PeriodicTimer` loop
- [ ] WebSockets, hand-implemented via `HttpListener.AcceptWebSocketAsync` for real-time updates
- [ ] Mosquitto MQTT broker talking to real smart home devices

---

## Learning Goals (map to phases)

| Concept               | Phase |
|-----------------------|-------|
| HTTP protocol itself  | 1 (deeper than ASP.NET would teach) |
| REST design           | 1     |
| C#                    | 1–2   |
| SQL: no abstraction   | 2     |
| HTML/CSS/JS           | 3     |
| React                 | 3–4   |
| Async, CORS, MIME/SPA serving | 4–5 |
| Deployment, Linux, systemd | 5 |
| Auth, security        | 6     |
| Hardware / IoT        | 7     |

---

## First Session Checklist

1. `dotnet new console -n backend`
2. Start `HttpListener` on port 5000, respond to `GET /api/devices` with hardcoded JSON
3. `dotnet run` → `curl http://localhost:5000/api/devices` → see JSON → done, that's your own HTTP server.

---

## Notes & Gotchas

- `HttpListener` is simple but low-level: URL decoding, path matching, and body parsing are all on you. Build the `Router` early and keep handlers thin.
- `System.Text.Json` is strict: mismatched casing between C# properties and JSON breaks things. Set `PropertyNamingPolicy = CamelCase` in your serializer options once.
- SQLite is fine on the Pi; keep the DB file outside your publish folder (e.g. `/opt/smart-home/data`) so redeploying doesn't wipe it.
- Plain .NET console apps have no built-in DI/logging/config — if you miss them, that's a sign you're ready for ASP.NET later. Use the pain as a teacher: hand-roll a tiny logger first.
