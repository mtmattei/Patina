# HANDOFF — Patina v1.0.0 build
Updated: 2026-10-03 02:20

## Where we are
Assessed the Uno tooling and app portfolio (`docs/ANALYSIS.md`, `docs/assessment/`), chose a new app, specced it
(`SPEC.md`, `docs/DECISIONS.md` D1–D15) and built Patina: a local-first, EN/FR field app for a municipal public-art
conservation team (collection + map, artwork record, condition survey with photos, treatment queue, settings,
export/import). Desktop, WebAssembly and Android are built, run and verified; Linux/macOS are published but not run;
iOS is CI-only. Release artifacts are in `_publish/release/` (git-ignored). Pushed to https://github.com/mtmattei/Patina (private); CI green on all jobs incl. the first iOS simulator build (run 37131898208).

## Last verified state
- Build: desktop, browserwasm, android Debug + Release: 0 warnings, 0 errors (Uno0001 as error). iOS not built.
- Tests: 69/69 NUnit pass (`Patina.Core`).
- Runtime: Windows desktop (all flows, 420/1024/1440 px, dark, French, published Release exe); WASM in headless Edge (load 7.5 s, draft survives reload, `/Patina/` subpath); Android API 36 emulator (Debug: map, callout, survey, camera, Back; Release APK: launch, collection, map). App MCP not used (session not rooted in the repo); Win32 + Playwright harnesses in `tools/`.
- Git: `main` pushed to origin (mtmattei/Patina, private). Tag `backup/before-untrack-publish` keeps pre-rewrite history (publish output was committed by mistake, then filtered out of 4 unpushed commits).
- Lint: CARD 0 · HEX 0 · TOKENTHEME 0 · BACKBAR 0 · CODEBEHIND 0 · OVERLAY 0 · RESPONSIVE 0 · BUILTIN 0 · ICON 0 (3 WORKAROUND markers, documented).

## Guidance applied
| Skill / rule / decision | Applied at (file:line) |
|---|---|
| mvux: AsyncEnumerable store feed | `Patina/Presentation/ArtworkModel.cs:13` |
| mvux: Retry by ElementName in ErrorTemplate | `Patina/Presentation/ArtworkPage.xaml:186` |
| uno-navigation: route-based Visibility region, DataViewMap | `Patina/Presentation/MainPage.xaml:97`, `Patina/App.xaml.cs:80` |
| uno-toolkit: NavigationBar, FilterChipGroupStyle | `Patina/Presentation/ArtworkPage.xaml:41`, `CollectionPage.xaml:146` |
| uno-material / art direction: invariant grade tokens, static card | `Patina/Styles/Condition.xaml:10`, `Patina/Styles/Patina.xaml:161` |
| xaml-design-polish: house easing | `Patina/Styles/Motion.xaml:4` |
| rules: Uno0001 as error, IDBFS, OSM User-Agent | `Patina/Patina.csproj:49`, `:59`, `Patina/Controls/ArtworkMap.cs:24` |
| gotchas: DialogAction callbacks, FallbackValue=Collapsed | `Patina/Presentation/SettingsModel.cs:111`, `ArtworkPage.xaml:27` |
| workaround protocol: AdaptiveTrigger on cached pages | `Patina/Presentation/CollectionPage.xaml:48` |

Read, not applied: Design Graph plugin, UnoAnnotation, Lapse, Atlas route diff (available, not run).

## Next actions (in order)
1. Android: create the upload keystore and add the four `ANDROID_*` secrets; download the signed AAB from CI.
2. Run the Linux zip once (any Linux box or `wsl --install`) and the macOS zip on a Mac; update `docs/DEPLOY.md` matrix.
3. File the ResponsiveExtension reconnect bug on unoplatform/uno.toolkit.ui (evidence: `docs/evidence/2026-10-03-responsive-extension-unloaded.md`). Already in `~/.claude/rules/uno-runtime-gotchas.md` with the ColumnDefinition and iOS Xcode-pairing gotchas (2026-10-05).
4. Run `atlas extract Patina/App.xaml.cs --source Patina --out app.json` and diff against the SPEC route tree.

## Open questions
- Sync/sign-in omitted (D3): does a real deployment need multi-device sync (DYT's Supabase pattern) before v1.1?
- OSM public tiles are a policy dependency; pick a tile provider before wide distribution (`docs/DEPLOY.md`).
- Signing/accounts still needed: Authenticode (Windows), Apple Developer ID + notarization (macOS), Apple Developer Program (iOS), Google Play account + upload key (Android).
- Calendar date picker leaves today's cell blank (Material CalendarView "today" colours); not fixed.
- First-frame ghost glyph on a filter chip (desktop, Release) matches the documented resize/stale-pixel artifact; not fixed.
- Android Skia accessibility (TalkBack) not verified; per rules it is WIP upstream.

## Relaunch
```
cd C:\Users\Platform006\Patina
dotnet test Patina.Tests/Patina.Tests.csproj
dotnet run --project Patina/Patina.csproj -f net10.0-desktop
```
Start the next session rooted in `C:\Users\Platform006\Patina` so `.mcp.json` registers the uno-app MCP.

CI note: iOS needs Xcode 26.3 paired with workload set 10.0.300 (`.github/workflows/ci.yml`); bump both together when the runner image changes.
