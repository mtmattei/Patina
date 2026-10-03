# Local app projects (assessment notes, 2026-10-03)

Method: read-only file inspection and git log. Nothing rebuilt in this pass unless stated.

| Project | Purpose | Stack | Persistence / tests | State | Verdict |
|---|---|---|---|---|---|
| DYT (Dorval Youngtimers) | Hockey league app | 6.7.x, android/wasm/desktop, MVUX, Nav regions, Supabase + fallback | real backend; 283 tests passing per HANDOFF | stall on tab switch, unapplied RLS fix, stacked PRs | most mature, domain-locked; ship as itself |
| ChefsNobu | Recipe benchmark rebuild | **6.8.0-dev.23 preview**, 4 heads, Material, MVUX, Mapsui 5.1 | local stand-in data; no tests | Windows Settings crash | benchmark; lift `Presentation/Map/NearMePage.xaml.cs` |
| MorningCard.Uno | "When to leave" car card | android/desktop, MVUX, Mapsui | 73 tests | dark/Android unverified | demo; lift `Controls/MutedTileSource.cs` |
| FieldCheck (2 runs) | Equipment inspection benchmark | android/WinAppSDK/desktop, MVVM | JSON in LocalAppData; ~50 tests; 96/96 acceptance | benchmark artifact | lift `Services/AttachmentPicker.cs`, `IDataStore`/`DataMode` test modes, `StatusBadge` |
| Plinth | Museum exhibit readiness | scaffold only, no screens | — | back-pocketed 2026-09-28 | keep separate; condition-report mock informs Patina's compare view |
| Cargo | Port ops dashboard | android/ios/wasm/desktop, MVVM, heavy Skia | in-memory constants; no tests | fresh clone does not build (local Liveline branch) | showcase |
| Loadpath | Truss workbench | desktop, MVUX | real files; 24 tests | dark mode open | demo; lift `Themes/MotionTokens.xaml`, `Controls/Motion.cs` |
| Lapse | App visual history | wasm/desktop, MVUX | real; 15 tests | desktop only verified | dev tool |
| UnoBusiness | Dataverse CRM | 6.7.22 desktop, MSAL, Kiota | real; 137 tests | lookup picker, sign-out broken | backend R&D |
| ZoomLog, FlowType, PacConsole | labs | desktop | tests present | — | labs |
| SchoolDropoff | archived (→ MorningCard) | — | — | archived | — |
| UNMS | research PDFs | — | — | — | not an app |

## Patina vs FieldCheck
Shared backbone: asset list → detail → inspection form → history. Patina adds what FieldCheck lacks: several
photos per survey, structured findings (type, zone, severity), a treatment queue with owners and status, a map,
and a per-artwork condition timeline. Not a duplicate.
