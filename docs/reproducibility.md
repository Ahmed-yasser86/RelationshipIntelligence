# Reproducibility

A technically competent researcher rebuilds the relevant software state
from this page alone.

- **Runtime**: .NET SDK 9.0.308 (`global.json`), all projects
  `net9.0`; Node for the client (Vite 8, React 19, TypeScript).
- **Database**: SQL Server (`DESKTOP-NFBJBC8/Contect_Manager` in dev
  config); schema via `dotnet ef database update` (30 migrations).
  Seed: 1 user + 10 persons + lookups via `HasData`; e2e login
  `testuser@contactsmanager.dev` / `Test123!` (stored hash is stale —
  documented in code, do not "fix" the hash alone).
- **Ports**: API `http://localhost:5156` (launch profile `http`),
  client `http://localhost:5173` (`VITE_API_URL` override supported).
- **Deterministic core**: scoring, queue, network, digest selection,
  outreach signals are pure functions of stored rows + `DateTime.UtcNow`
  — re-derivable by hand from the registry (see
  [methodology](methodology.md) for worked forms).
- **Non-deterministic parts**: Copilot wording, extraction output shape
  (contract-pinned, content varies), provider availability. Tests stub
  all of these; live verification needs a configured provider key.
- **Time assumptions**: UTC both ends, silence floored to whole days;
  digest weeks start Monday midnight; reminder intervals in whole days.
- **Expected outputs**: `dotnet test` → 272 + 24 green;
  `npm run build` → `tsc -b && vite build` clean; API root `/` → 404
  (no root route — expected), `/api/*` under JWT.
