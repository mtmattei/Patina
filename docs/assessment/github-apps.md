# GitHub-only app repos (assessment notes, 2026-10-03)

Method: read-only GitHub API (trees, global.json, csproj, README/HANDOFF/SPEC). Nothing built or run.

| Repo | Purpose | Stack | Completeness | Lift |
|---|---|---|---|---|
| dorval-youngtimers | Rec hockey league app | 6.7.22, android/wasm/desktop, MVUX, Nav, Auth, Http, Loc en/fr, Supabase + embedded fallback | ~13 pages, 218 Core tests; desktop verified, Android/WASM not rebuilt | Core/UI split, `Controls/{StatePanel,Skeleton*}`, `Services/{AppThemeService,ToastService,MotionSettings,AppSettings}`, PWA manifest |
| QuoteCraft (Uno-Builds) | Quotes CRUD | MVUX, SQLite (sqlcipher), Supabase, QuestPDF, tests, android/wasm/desktop | 11 pages | SQLite repositories |
| ZooQuest | Zoo companion for kids | 6.6.42, MVUX, Nav, Storage, Serialization | 29 tests; several placeholder pages | `Services/CameraService.cs`, `Services/FileAdventureStore.cs` (debounced, versioned, source-gen JSON) |
| Bill-tracker (BillCal) | Personal bills calendar | 6.6.42, desktop/android, MVUX | 32 tests, ~6 screens; narrow breakpoints unverified | `DuShell.xaml` responsive shell, `Clock.cs` |
| Aware | AR room memory | 6.6.42, MVVM, 4 TFMs | 135 tests; ARCore unverified | 4-locale resw, PWA manifest, `HapticsService`, `MotionSettings` |
| Meridian | Market dashboard | 6.5.31, MVUX, LiveCharts | no tests | `Liveline/` chart project |
| Mapplate | Map posters | 6.7.30 desktop, MVUX | 1 screen, tests | `MapPlate.Render` (static OSM renderer) |
| Orbital | Dev daily hub | 6.5.36 desktop | no tests | 4-locale strings |
| Deskcompanion | Claude Code desktop pet | 6.7.30 | tests | env-switch verification pattern |
| ConfPass, Hyperspeed, Thermostat-Build | design demos | old SDKs | no tests | — |
| Lumen, SampleBuilds | briefs / HTML only | — | — | — |

Findings: no repo has Uno CI (`.github/workflows`); no repo has an interactive map control; Uno.Sdk spread 6.4.26–6.7.30; several apps duplicated inside Uno-Builds.
