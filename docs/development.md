# Development

## Local run (no Docker)

```powershell
$env:ASPNETCORE_ENVIRONMENT = "Development"
dotnet run --project RelationshipIntelligence.Api/RelationshipIntelligence.Api.csproj --urls http://localhost:5156 --launch-profile http
npm --prefix relationship-intelligence-client run dev -- --port 5173 --strictPort
dotnet test RelationshipIntelligence.sln
```

API: `http://localhost:5156` (Swagger in Development only).
Client: `http://localhost:5173`. Never overlap `dotnet run` with
`dotnet test` (locked DLLs). Kill stray hosts first:
`Stop-Process -Name RelationshipIntelligence.Api -Force`.

## Secrets and config

`appsettings.json` holds non-secrets; provider keys are per-user rows
(`AiProviderSettings`, DataProtected), set via Copilot setup in the UI
or `PUT /api/Copilot/PutAiSettings`. JWT uses lowercase `jwt:` keys.
Never commit keys; gitleaks scans on push.

## Scripts worth knowing

- `Temp/opencode/*.ps1` patterns from history: `rundll.ps1` (launch),
  `filterlive.ps1` / `orglive*.ps1` (live endpoint checks), `tsc*.ps1`
  (typecheck). Recreate as needed; none are tracked.
- Migrations: `dotnet ef migrations add <Name>` + `dotnet ef database
  update` with the Api as startup project.

## Contributor notes

One class per file; services behind contracts; repositories behind
contracts; API as composition root; AI references Core only. Every
behavioral fix ships with its regression test. Docs live in `docs/`;
the root README is the product front door, not the CI manual (see
[ci-cd](ci-cd.md)).
