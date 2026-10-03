# Patina: spec

Field app for a municipal public-art conservation unit. Conservators and technicians survey outdoor artworks,
record findings with photos, grade condition, and work a treatment queue. Local-first, bilingual EN/FR.
Decisions: `docs/DECISIONS.md`. Design source: `docs/DESIGN.md`. Tool/project assessment: `docs/ANALYSIS.md`, `docs/assessment/`.

**Users.** Conservator (surveys, grades, proposes treatment), technician (schedules and performs treatment).
v1 has no roles or sign-in; the surveyor name is a setting stamped on every record.

**Core loop.** Collection → artwork → survey (findings + photos) → submit → grade updates, urgent findings
become proposed treatments → queue → schedule → in progress → done (with note) → artwork history.

## Architecture Brief

- **Targets:** `net10.0-desktop` (Windows/macOS/Linux, Skia), `net10.0-browserwasm` (PWA), `net10.0-android`, `net10.0-ios`. Uno.Sdk **6.7.30** pinned in `global.json`.
- **UnoFeatures:** `Material; Toolkit; Hosting; Navigation; MVUX; Localization; Logging; Configuration; ThemeService; SkiaRenderer; Skia`.
- **Pattern:** MVUX. Decision: MVUX for every page. Reason: async store-backed feeds, built-in Progress/None/Error states, generated commands (`uno-scaffolding.md`). Tradeoff: `{Binding}` only against generated view models; editable fields bind top-level states, never `Data.X` TwoWay inside a FeedView (runtime gotcha).
- **Projects:** `Patina.Core` (net10.0: records, rules, queries, `PatinaStore`, JSON source-gen context), `Patina` (Uno app), `Patina.Tests` (NUnit over Core, template default).
- **Shell:** `Shell.xaml` hosts `ExtendedSplashScreen` only. `MainPage` owns `Region.Attached` root, a Visibility region (route-based, empty), a vertical rail `utu:TabBar` (≥ Wide) and a bottom `utu:TabBar` (< Wide), sharing `Region.Name`s.
- **Routes:** see Route tree. Detail pages are siblings of `Main` under the shell (Chefs pattern) and stack on the shell's frame.
- **DI (`IHostBuilder`):** `IDocumentFile` → `LocalFolderDocumentFile` (singleton), `PatinaStore` (singleton), `IPhotoService` → `PhotoService` (singleton, needs `Window`), `IDataTransfer` → `DataTransferService` (export/import pickers), `IAppSettings` → `AppSettings` (`ApplicationData.LocalSettings`), `IClock` → `SystemClock`, `IThemeService`, `ILocalizationService`.
- **Models:** `ShellModel`, `MainModel`, `CollectionModel`, `ArtworkModel(ArtworkSummary)`, `SurveyModel(SurveyStart)`, `QueueModel`, `TreatmentModel(TreatmentSummary)`, `SettingsModel`. Change propagation: `PatinaStore.Watch()` (`IAsyncEnumerable<PatinaDocument>`, current value then each commit) projected with `Feed.AsyncEnumerable`; never awaited inside a model (gotcha G30).
- **Persistence:** one JSON document `patina.json` (schema `Version`), atomic write (temp file then replace) through `StorageFile` APIs in `LocalFolder`; photos in `LocalFolder/photos/<id>.<ext>`. First run seeds from embedded `seed.json` (fictional collection, real Montréal coordinates). Corrupt file → renamed `patina.corrupt-<ts>.json`, load error surfaced with Retry and Restore-sample actions.
- **Rules (Core, pure, tested):**
  - Grade from findings: none or max severity 1 → Good; max 2 → Fair; max 3 → Poor; max 4 → Critical; three or more severity-2 findings escalate Fair → Poor.
  - Survey interval by material (months): Mural 6, Wood 6, Bronze 12, Steel 12, Mosaic 12, Stone 24, Concrete 24. Poor halves it, Critical caps it at 3.
  - Due status: NeverSurveyed / Overdue / DueSoon (≤ 30 days) / Current.
  - Submit proposes one treatment per finding with severity ≥ 3 (severity 4 → Urgent priority, 3 → High).
  - Treatment transitions: Proposed → Scheduled (requires date) → InProgress → Done (requires completion note). Back-steps allowed to the previous status; Done is final.
- **Platform constraints:** Camera capture Android/iOS only (`CameraCaptureUI`); picker everywhere. Map: Mapsui 5.1 (desktop + Android validated; WASM, iOS unverified). File pickers on WASM use the File System Access API where present, else download/upload pickers.
- **Testing:** NUnit on Core (rules, transitions, queries, store round-trip, corrupt-file recovery, import/export). Runtime verification per head with `uno-verify` (App MCP when the session runs in the repo; launch-and-capture otherwise). Lint: `xaml-semantic-lint.ps1` clean before each commit. Atlas route extraction diffed against the Route tree.

### Capability inventory

| Capability | Status | Choice |
|---|---|---|
| Interactive map of artworks | implemented | Mapsui 5.1 + muted OSM tiles (`MutedTileSource`) |
| Photo capture | implemented (Android/iOS) / substituted (desktop, web: file picker) | `CameraCaptureUI`, `FileOpenPicker` |
| Local persistence, offline | implemented | JSON document + photo files in `LocalFolder` |
| Multi-device sync, sign-in | omitted | export/import file instead (D3) |
| Localization EN/FR | implemented | `Uno.Extensions.Localization`, `.resw`; switch applies on restart (docs) |
| Light/dark theme | implemented | Material roles from DesignMd2Uno + `IThemeService` |
| Data export/import | implemented | `FileSavePicker` / `FileOpenPicker` |
| Notifications for due surveys | omitted | due status shown in-app only |
| Artwork photos in seed data | omitted | fictional collection; honest empty state |
| Reduced motion | implemented | `UISettings.AnimationsEnabled` gate |

## Design Brief

- **Direction:** a conservator's condition report crossed with a museum object label. Cool limestone, iron ink, verdigris as the one committed color. Calibration (art-direction pass 2): the default cluster is cream + serif + terracotta; this plan uses a cool mineral ground, a condensed grotesque and a green drawn from the subject (weathered bronze), and keeps mono only for accession numbers, dates and measurements, which object labels type.
- **Signature: the condition strip.** Four patches in a row, like the color-calibration strip conservators photograph beside an object. Filled count = grade (1–4), each patch carries its grade color, and the grade numeral + word sit beside it. Used on collection rows, the artwork header, the live survey grade and the queue. One signature, nothing else decorative.
- **Second identity device: the tombstone label.** Accession (mono), title (display), artist · year, material · district. It heads the artwork and survey pages.
- **Tokens (generated, `Styles/`):** Material roles (`ColorPaletteOverride.xaml`, Light + Dark), type ramp (`Typography.xaml`, Display 56/60 Condensed SemiBold → Label 12/16), spacing 4/8/12/16/24/32/48 and radius 6/12/24/999 (`Tokens.xaml`).
- **Custom tokens (rung 4, `Styles/Condition.xaml`):** `Grade1InvariantBrush`…`Grade4InvariantBrush` (`#3E8762`, `#9C7C22`, `#C0632A`, `#D04436`; each ≥ 3:1 against light and dark Surface/Background), empty patches use `OutlineVariantBrush`. Reason: Material has no ordinal status ramp. Theme-invariant fills: patches never carry text, and the numeral beside them uses `OnSurfaceBrush`.
- **Motion (`Styles/Motion.xaml`, house values):** `EaseSmooth` `0.22,1 0.36,1`, `DurationFast` 150 ms, `DurationNormal` 200 ms, `DurationSlow` 280 ms, entrance rise 6 px, press scale 0.98.
- **Scopes:** everything app-wide in `App.xaml`; no page-scoped dictionaries in v1.
- **Styles:** `CardContentControl` Filled/Outlined via lightweight keys (`CardCornerRadius` 12); `ConditionStrip` control template (`Controls/ConditionStrip.xaml`); `TombstoneLabel` templated control.
- **Fonts:** `Assets/Fonts/IBMPlexSansCondensed-{Medium,SemiBold}.ttf`, `IBMPlexSans-{Regular,Medium,SemiBold}.ttf`, `IBMPlexMono-{Regular,Medium}.ttf` + Uno font manifests; OFL. Static instances only (variable fonts render their default instance on Skia).
- **Layout (breakpoints, `{utu:Responsive}`, default layout 150/300/600/800/1080):**
  - Nav: bottom TabBar below Wide (800), vertical rail at Wide and up.
  - Collection: below Widest, a segmented List / Map switch (inline Visibility region); at Widest and up, list (420 px) and map side by side, map full-bleed to the window edge.
  - Detail pages: single column, max content width 760 centred below Wide; at Wide the artwork page splits hero/label (left) and history (right).

## Interaction Brief

- **Flows:**
  1. Collection → tap artwork → Artwork → Start survey → Survey → Add finding × n → Submit → back to Artwork (grade and proposed treatments updated).
  2. Queue → tap treatment → Treatment → Schedule (date) → Start → Mark done (note) → back.
  3. Artwork → open treatment → Treatment.
  4. Settings → Export → save file; Import → pick file → confirm replace dialog → data replaced.
- **Input:** every action reachable by keyboard; Enter in search applies; Esc leaves a detail page (NavigationBar back). Touch targets ≥ 44 px. Map: pan/zoom by pointer and touch; pins select the artwork (list scrolls and highlights it).
- **FeedView states:** Collection list, Artwork dossier, Queue list, Treatment detail: Progress (skeleton rows), None (invitation copy), Error (message + Retry bound by `ElementName`). Survey edits are states, not feeds.
- **Dialogs:** import, reset and discard-draft confirmations use `INavigator.ShowMessageDialogAsync` with `DialogAction` callbacks (the awaited result is unreliable; runtime gotchas). Survey Back autosaves a draft instead of asking.
- **Visual states:** cards and rows: Normal / PointerOver / Pressed (0.98) / Focused; severity selector: segmented with Selected state.
- **Animations:** page content entrance (opacity 0→1 + 6 px rise, 280 ms EaseSmooth, Storyboard); condition strip switches state without animation. Reduced motion (system setting, plus an in-app toggle because Skia desktop reports animations always on) → no entrance.
- **Accessibility:** `AutomationProperties.Name` on every icon button and the condition strip ("Condition 3 of 4, Poor"); focus order follows reading order; contrast checked by DesignMd2Uno (24/24 ≥ 4.5:1).
- **Verification (uno-verify):** launch desktop; snapshot each page; exercise flow 1 and 2 end to end; resize across 600/800/1080; light and dark; FR culture; WASM publish served locally; Android emulator install and flow 1.

## Spec Graph Brief

```text
Shell  ViewMap<ShellPage... ShellModel>   (ExtendedSplashScreen; no Region.Attached here)
├─ Main        ViewMap<MainPage, MainModel>   IsDefault   [Grid Region.Attached; Visibility region, route-based]
│  ├─ Collection  ViewMap<CollectionPage, CollectionModel>  IsDefault
│  │  └─ ArtworkList (ItemsRepeater) → Artwork (data: ArtworkSummary)
│  ├─ Queue       ViewMap<QueuePage, QueueModel>
│  │  └─ TreatmentList → Treatment (data: TreatmentSummary)
│  └─ Settings    ViewMap<SettingsPage, SettingsModel>
├─ Map         ViewMap<MapPage, MapModel>   (narrow windows; wide windows show the map beside the list)
├─ Artwork     DataViewMap<ArtworkPage, ArtworkModel, ArtworkSummary>
│  ├─ StartSurvey  → Survey (data: SurveyStart)          [model command, INavigator]
│  └─ OpenTreatment → Treatment (data: TreatmentSummary)
├─ Survey      DataViewMap<SurveyPage, SurveyModel, SurveyStart>   back: - (after Submit / Discard)
└─ Treatment   DataViewMap<TreatmentPage, TreatmentModel, TreatmentSummary>   back: -
```

```text
CollectionPage
├─ Header: count needing attention (DisplayLarge) ← Stats feed   {Value, Progress, None, Error}
├─ SearchBox ⇒ IState Query        FilterChips ⇒ IState Filter
├─ ArtworkList ← FeedView(Artworks: IListFeed<ArtworkSummary>) {Value, Progress, None, Error}
│  └─ Row: accession · title · artist·year · ConditionStrip ← Grade · due label ← Due
└─ ArtworkMap ← Artworks (pins), ⇒ IState SelectedId
ArtworkPage
├─ NavigationBar (back) · TombstoneLabel ← Dossier.Artwork
├─ Hero photo ← Dossier.CoverPhoto | empty invitation
├─ ConditionStrip ← Dossier.Grade · due line ← Dossier.Due
├─ History list ← Dossier.Surveys      Open treatments ← Dossier.Treatments → Treatment
└─ Start survey (primary) → command StartSurvey
SurveyPage
├─ TombstoneLabel (compact) · live ConditionStrip ← Grade (Findings.Select)
├─ Surveyor ⇒ IState Surveyor · Weather ⇒ IState Weather · Notes ⇒ IState Notes · Overview photos ⇒ IListState Photos
├─ Findings ← IListState Findings (remove command per row)
├─ FindingEditor: Type ⇒ NewType · Zone ⇒ NewZone · Severity ⇒ NewSeverity · Note ⇒ NewNote · photos ⇒ NewPhotos
└─ Save draft / Submit (validation message ← IState Problem)
QueuePage:   urgent count (Display) · status chips ⇒ IState StatusFilter · TreatmentList ← FeedView
TreatmentPage: header · status stepper ← Detail.Status · Date ⇒ IState Date · Assignee ⇒ IState Assignee · Note ⇒ IState Note · Advance/Back/Save commands · log ← Detail.Log
SettingsPage: Surveyor ⇒ IState · Theme ⇒ IState · Language ⇒ IState · Export / Import / Reset commands · About
```

```text
PatinaStore (singleton) ──Watch()──► Feed.AsyncEnumerable
  CollectionModel.Document ─ Combine(Query, Filter) ─► Artworks (IListFeed) , Stats
  ArtworkModel.Document ─ Select(Queries.Dossier(id)) ─► Dossier
  QueueModel.Document ─ Combine(StatusFilter) ─► Treatments (IListFeed), Stats
  TreatmentModel.Document ─ Select(Queries.Treatment(id)) ─► Detail
Writers (single writer each): Survey states ← two-way bindings only; store ← SurveyModel.SaveDraft/Submit,
TreatmentModel.Advance/Back/Save, SettingsModel.Import/Reset. All store writes go through PatinaStore.UpdateAsync (serialised).
```

```text
App.xaml @App
├─ ColorPaletteOverride (Material roles, ThemeResource, Light/Dark)    generated
├─ Typography (DisplayLarge…LabelSmall, MonoTextStyle)                  generated
├─ Tokens (Spacing*, Radius*)                                           generated
├─ Condition.xaml  Grade1–4Brush  StaticResource  cue: patch count + numeral + word
│  └─ ConditionStrip.Patches ← GradeNBrush
├─ Motion.xaml  EaseSmooth · DurationFast/Normal/Slow
│  ├─ ConditionStrip patch fill ← DurationFast    reduced: final state
│  └─ Page entrance ← DurationSlow                reduced: none
└─ Card lightweight keys  CardCornerRadius ← RadiusMdCornerRadius
```

### Spec gate

| Check | Result | Nodes | Resolution |
|---|---|---|---|
| Registration | PASS | all pages mapped; data pages use DataViewMap with named type | — |
| Region host | PASS | Main Visibility region hosts Collection/Queue/Settings; detail pages on shell frame | — |
| Shell safety | PASS | no Region.Attached in Shell | — |
| Cross-page links | PASS | Artwork, Survey, Treatment carry data records | — |
| Back stack | PASS | detail pages `-`; Submit/Discard return `-` | — |
| Sources / feed vs state | PASS | editable fields are states; lists are feeds | — |
| Single writer | PASS | store writes serialised through `UpdateAsync` | — |
| FeedView states | PASS | four async nodes declare all templates | — |
| Failure paths | PASS | survey validation, treatment transition rules, save failure InfoBar, import validation | — |
| Tokens / theme | PASS | grade brushes theme-invariant by design (no text on them) | — |
| Color cue | PASS | strip = count + numeral + word | — |
| Motion | PASS | durations/easing resources + reduced-motion fallback | — |
| Type assets | RISK | static TTFs + manifests; verify on WASM and Android | Risks |
| Targets | RISK | camera mobile-only; map on WASM/iOS unverified | Risks |
| Capabilities | PASS | omitted rows listed in Unresolved Questions | — |

## Implementation Plan

1. Scaffold `dotnet new unoapp -preset recommended -theme material`, add Core and Tests projects, `global.json`, `.mcp.json`, Uno0001 policy. Build 0 warnings. Commit.
2. Core: records, rules, queries, store, seed. Tests green. Commit.
3. Theme: generated Styles, fonts + manifests, Condition and Motion dictionaries, `ConditionStrip`, `TombstoneLabel`. Commit.
4. Shell + navigation: MainPage regions, both TabBars, routes. Atlas extract diff. Commit.
5. Collection page (list, filters, search, stats) + map control. Verify desktop. Commit.
6. Artwork page. Survey page (findings, photos, validation, submit). Verify flow 1. Commit.
7. Queue + Treatment pages. Verify flow 2. Commit.
8. Settings: theme, language, surveyor, export/import, reset, about. FR strings complete. Commit.
9. States pass (`uno-component-states`), polish pass, lint clean, responsive sweep 600/800/1080, dark mode.
10. Heads: WASM publish + PWA manifest; Android build + emulator run; Linux desktop via WSL.
11. Release: CI workflow, README (dev/build/test), DEPLOY.md per target, version 1.0.0.

## Risks

- API: Mapsui 5.1 on WASM (Skia renderer) and OSM tiles from the browser (no custom User-Agent header) — spike 15 min after step 10 starts.
- API: `LocalFolder` persistence across reloads on WASM — spike 10 min (write, reload, read).
- API: showing photo files from `LocalFolder` in `Image` on every head (`ms-appdata:///local/` vs stream `SetSourceAsync`) — spike 10 min in step 6.
- API: font manifests on WASM/Android — verify in step 10.
- API: `ILocalizationService` culture switch (applies on restart) — verify in step 8.

## Build outcome (2026-10-03)

Built as specified with the deviations recorded in `docs/DECISIONS.md` D11–D15 and `docs/KNOWN-ISSUES.md`. Verification per head: `docs/DEPLOY.md`.

## Unresolved Questions

- Multi-device sync and sign-in omitted (D3). Candidate: DYT's Supabase source + embedded fallback pattern. Needs a backend owner and tenancy decision.
- Due-survey notifications omitted; due status is in-app only.
- No artwork photos in seed data (fictional collection). A real deployment imports the city's records.
- Camera capture on desktop/web substituted by file picker.
- iOS and macOS are intended targets that cannot be built or run on this machine (needs a Mac).
- Atlas, UnoAnnotation, Lapse, Design Graph JSON not wired into this release (dev-time tools; see ANALYSIS.md).
