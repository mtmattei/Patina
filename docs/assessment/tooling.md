# Tooling repos (assessment notes, 2026-10-03)

Method: shallow clones read in a temp folder; build/test evidence taken from each repo's README/HANDOFF. Nothing rebuilt.

| Tool | Uno.Sdk | Maturity | Consumed as | Usable now |
|---|---|---|---|---|
| DesignMd2Uno | 6.7.30 | golden-file tests, samples | `dotnet run --project src/DesignMd2Uno.Cli -- DESIGN.md -o Styles` → ColorPaletteOverride/Typography/Tokens.xaml + report.md | **ready** |
| Atlas (AppMap) | 6.5.36 | CI, tests, `atlas` dotnet tool installed globally | `atlas extract App.xaml.cs --source <dir> --out app.json` | **CLI ready**, viewer needs build |
| uno-design-graph-plugin | — | CI, tests, Python scripts | plugin or copied skill; `validate_graph.py` needs `jsonschema` | ready, not installed |
| MotionTokens | 6.7.30 | gallery + tests, no package | copy source or ProjectReference | needs build; **numbers conflict with `xaml-design-polish`** |
| Tactile | 6.7.30 | tests, desktop/Skia only | copy `.sksl` + `TactileSurface` | needs build; no WASM/mobile |
| UnoAnnotation | 6.5.31 | packable, unpublished; License.md wrong | Debug ProjectReferences + server + `.mcp.json` | needs build |
| UnoComposer / Composer | 6.5.31 | prototype / superseded | generator app | needs build |
| CompositionStack | — | templates | copy docs | ready |
| uno-scaffold | — | source of installed skills | skills + lint | in use |
| claude-uno-plugins, Skills | — | mirrors / superseded | — | redundant locally |
| uno-fleet-kit | — | install script | `install.ps1 -Repo <app>` | ready |
| Lapse | 6.7.30 | 15 tests, desktop verified | app | ready as reference |
| themestudio | private SDK | needs Uno.Themes branch | demo | heavy |
| design-ledger | — | skill + ledger | skill | ready (web oriented) |
| ComponentStatesLab, uno-agent-ledger, uno-gotcha-evidence, Workflow | — | knowledge / archive | read | — |

Chains: spec docs → DesignMd2Uno → Material resources; spec route tree → `RegisterRoutes` → `atlas extract` diff;
design graph JSON → App MCP snapshot → `diff_graph.py`; UnoAnnotation → agent; fleet kit + Lapse for history.

Conflicts: MotionTokens vs `xaml-design-polish` (150/200/280 ms, EaseSmooth `0.22,1 0.36,1` vs 100–600 ms,
`0.2,0,0,1`); DesignMd2Uno vs hand-written palettes (one owner for `ColorPaletteOverride.xaml`); three skill copies.
