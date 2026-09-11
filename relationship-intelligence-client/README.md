# Relationship Intelligence Client

Research-oriented frontend for the Relationship Intelligence backend
(`RelationshipIntelligence.Api`). React 19 + Vite + TypeScript + Tailwind CSS v4
+ shadcn/ui. See `PLAN.md` for the product/UI/UX plan and endpoint mapping.

## Run

```bash
npm install
cp .env.example .env   # set VITE_API_URL to the API origin (default http://localhost:5156)
npm run dev
```

## Check

```bash
npx tsc --noEmit -p tsconfig.app.json
npm run build
```
