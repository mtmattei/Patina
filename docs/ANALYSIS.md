# How the Uno Platform tooling fits together

Inventory source: `~/uno-tooling.md` (2026-10-03). This document answers two questions: how the
tools compose into one pipeline, and which of them Patina (the app in this repo) actually uses.

## The pipeline

Each tool owns one artifact. The artifacts form a chain, and each later tool checks the artifact
before it. That is the whole design: one source of truth per layer, and a checker at every hop.

| # | Stage | Tool | Artifact it owns | Checks |
|---|---|---|---|---|
| 1 | Concept | `product-thinking`, concept memory | concept paragraph | demand, depth items |
| 2 | Bootstrap decisions | UnoComposer (6 decisions → starter pack) | `CLAUDE.md`, `DESIGN.md`, `INTERACTIONS.md` | — |
| 3 | Visual direction | `xaml-art-direction` (+ `design-ledger` gate, `uno-explore-directions` fleet for options) | direction card, token set | ledger collision gate |
| 4 | Tokens → XAML | DesignMd2Uno (`DESIGN.md` → `ColorPaletteOverride.xaml`, `Tokens.xaml`, `Typography.xaml`) | resource dictionaries | contrast report |
| 5 | Motion vocabulary | MotionTokens (durations, easings, staggers as resources) + `xaml-design-polish` numbers | `Motion.xaml` | reduced-motion policy |
| 6 | Spec | `uno-write-spec` + `capability-coverage` | `SPEC.md` with route tree, page trees, data-flow graph, design graph | spec gate |
| 7 | Machine-checkable design | Design Graph plugin (`*.graph.json`) | graph of screens, components, states, tokens | `validate_graph`, `lint_graph` |
| 8 | Scaffold | `uno-scaffolding` hub + `~/.claude/rules`, `uno-platform-agent` | Uno.Sdk single project | `dotnet build` 0 warnings |
| 9 | Build | `mvux`, `uno-navigation`, `uno-toolkit`, `uno-material`, `uno-extensions-services` | source | semantic lint hook on every edit |
| 10 | States | `uno-component-states` | Loading / Empty / Error per async node | FeedView templates present |
| 11 | Runtime verify | `uno-verify` (App MCP), Atlas (declared vs observed routes), Design Graph `diff_graph` | snapshots | route tree diff, graph drift |
| 12 | Human feedback | UnoAnnotation (click element → agent with `file:line`) | annotations | resolved status |
| 13 | Quality | `gold-standard-pass`, `userinterface-wiki-uno`, `uno-audit`, `uno-completeness-audit`, `uno-break-it`, `uno-hill-climb` | findings | lint, completeness grades |
| 14 | History | Lapse (snapshot per commit, pixel diff) | timeline | visual regressions |
| 15 | Ship | `uno-wasm-pwa`, CI workflow, `demo-ready` | artifacts | CI green |

## Where the tools overlap or collide

- **Two things are called "design graph".** `uno-write-spec`'s Design graph is a text tree (tokens → styles → nodes)
  inside SPEC.md. The Design Graph plugin's graph is JSON with a schema and scripts. They describe the same
  layer. Recommendation: generate the plugin's JSON from the spec's tree, so the spec stays the source and the
  JSON is the checkable projection. Rename one of them (for example "token tree" in the spec).
- **Route tree is written three times.** The spec's route tree, `RegisterRoutes`, and Atlas's `AppModel`.
  Atlas's static extractor reads `RegisterRoutes`, so Atlas is the natural diff tool between spec and code.
- **DesignMd2Uno vs `uno-material` vs `xaml-art-direction`.** Art direction decides the tokens, DesignMd2Uno
  emits them, `uno-material` governs how screens consume them. No conflict once the order is fixed (3 → 4 → 9).
- **MotionTokens vs `xaml-design-polish`.** Same vocabulary. The skill has the numbers, the library has the
  resources. Fold the skill's numbers into the library's defaults.
- **Lint lives in three places** (`uno-audit` scripts, `uno-scaffold` repo, Studio PR #147). Local rules are the
  source of truth; the repo syncs from them.
- **Tactile** (SkSL surface effects) and **ThemeStudio** have no stage. They are showcase pieces.

## Gaps found

- No tool turns the spec's route tree into `RegisterRoutes` or checks the two match. Atlas can do the check;
  nothing generates.
- No deployment stage tooling beyond `uno-wasm-pwa`. CI, signing and store packaging are hand-written per app.
- UnoComposer produces the starter pack but nothing consumes `INTERACTIONS.md` downstream.
- The App MCP is per-project (`.mcp.json` beside the `.sln`), so a session started elsewhere has no runtime
  verification. Fallback: launch-and-capture (`reference-win32-input-injection`).

## Project compatibility assessment

Full per-project notes: `assessment/local-apps.md`, `assessment/github-apps.md`, `assessment/tooling.md`.

**Can be the base of a deployable app:** DYT (Dorval Youngtimers: MVUX, region shell, Supabase with embedded
fallback, 283 tests, three heads) and QuoteCraft (offline-first CRUD with SQLite). Both are domain-locked; extending
either means finishing that product, not building a new one.

**Work together (share infrastructure, not screens):**
- Storage and states: FieldCheck's attachment copy-into-app-storage, ZooQuest's versioned JSON store, DYT's
  StatePanel/skeleton controls. Patina adopted the attachment and store patterns.
- Maps: ChefsNobu's Mapsui setup and MorningCard's `MutedTileSource`. Patina reuses both.
- Motion: Loadpath/MotionTokens/`xaml-design-polish` describe the same layer; pick one table per app.
- Tooling chain: DesignMd2Uno → app theme; Atlas → route checks; Design Graph → drift checks; Lapse → history.

**Should stay separate:** demos and labs (Cargo, Loadpath, Lapse, ZoomLog, FlowType, PacConsole, MorningCard,
Hyperspeed, ConfPass, Meridian, Mapplate, Orbital, DeskCompanion), benchmarks (ChefsNobu, FieldCheck), shelved or
archived work (Plinth, SchoolDropoff, Lumen brief), and the Uno-Builds monorepo. Merging their features into one app
would produce a catalogue, not a product.

**Overlap to resolve across the portfolio:** three skill copies (installed skills, `claude-uno-plugins`,
`UnoPlatformSkills`), several apps duplicated inside Uno-Builds, Uno.Sdk spread from 6.4.26 to 6.7.30, and no
Uno-specific CI anywhere before Patina's workflow.

## Direction chosen

A new app (Patina) that reuses proven pieces rather than extending a domain-locked one: no existing app is a
public-art conservation tool, the closest (FieldCheck) is a single-attachment benchmark and the closest domain
(Plinth) has no screens. See `DECISIONS.md` D1.

## What Patina uses, and why

| Tool | Used | How |
|---|---|---|
| concept memory | yes | credible industry app, specific team and workflow |
| UnoComposer | substituted | its six decisions are recorded in `DECISIONS.md` instead of running the prototype app |
| `xaml-art-direction` | yes | direction, signature (condition strip), tokens in SPEC.md Design Brief |
| DesignMd2Uno | **yes** | `docs/DESIGN.md` → `Styles/ColorPaletteOverride.xaml`, `Typography.xaml`, `Tokens.xaml` (24/24 contrast pairs pass) |
| MotionTokens | substituted | values conflict with `xaml-design-polish`; house values in `Styles/Motion.xaml` (D9) |
| `uno-write-spec` | yes | `SPEC.md` |
| Design Graph plugin | deferred | not installed; the spec's text design tree stands in |
| `uno-scaffolding` + rules | yes | scaffold, Uno0001 gate (caught two unimplemented APIs), font naming, IDBFS |
| `mvux`, `uno-navigation`, `uno-toolkit`, `uno-material`, `uno-extensions-services`, `uno-wasm-pwa` | yes | build |
| semantic lint | yes | PostToolUse hook on every XAML edit; deliberate exceptions carry `xaml-lint: allow` |
| `uno-verify` | fallback mode | no App MCP in this session; Win32 capture/drive scripts (`tools/`) and Playwright for WASM |
| Atlas | available | `atlas extract` can diff `RegisterRoutes` against the spec route tree (not run) |
| UnoAnnotation, Lapse | not used | need local builds; listed in Unresolved Questions |
