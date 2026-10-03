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

## What Patina uses, and why

| Tool | Used | How |
|---|---|---|
| concept memory | yes | credible industry app, specific team and workflow |
| UnoComposer | substituted | its six decisions are recorded in `DECISIONS.md`; the app is not run to save a round trip |
| `xaml-art-direction` | yes | direction and tokens in SPEC.md Design Brief |
| DesignMd2Uno | substituted | not published as a tool; `DESIGN.md` is written in its format and the palette is hand-emitted to the same file layout |
| MotionTokens | substituted | library is private and unpackaged; motion resources follow its naming in `Styles/Motion.xaml` |
| `uno-write-spec` | yes | `SPEC.md` |
| Design Graph plugin | deferred | not installed locally; the spec's design tree stands in |
| `uno-scaffolding` + rules | yes | scaffold and conventions |
| `mvux`, `uno-navigation`, `uno-toolkit`, `uno-material`, `uno-extensions-services` | yes | build |
| semantic lint | yes | PostToolUse hook plus a full run before each commit |
| `uno-component-states` | yes | every FeedView |
| `uno-verify` | yes, fallback mode | no App MCP in the coordinating session; launch-and-capture |
| Atlas, UnoAnnotation, Lapse | not used | need their own local builds; listed in Unresolved Questions |
| `uno-wasm-pwa` | yes | WebAssembly head ships as a PWA |
