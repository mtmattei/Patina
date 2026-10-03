# Patina

Field app for a city's public-art conservation team. Conservators survey outdoor artworks (bronze, steel, stone,
murals, mosaic), record findings with photos, and the app grades condition, schedules the next survey and turns
serious findings into a treatment queue. Local-first, bilingual (English / French), built with
[Uno Platform](https://platform.uno) for Windows, macOS, Linux, the web, Android and iOS from one C#/XAML codebase.

The sample collection is fictional: invented works and artists placed at real Montréal sites.

## What it does

| Screen | Job |
|---|---|
| Collection | Every artwork ranked by urgency; search, filters, and a map of the city with condition-coloured pins |
| Artwork | Object label, record photograph, condition grade, next survey date and why, open treatments, survey history |
| Survey | Findings (type, zone, severity, photos), live grade, autosaved draft, submit |
| Treatments | Queue of proposed / scheduled / in-progress work, late work first |
| Treatment | Schedule, assign, log, step through to done (a completion note is required) |
| Settings | Surveyor name, theme, reduced motion, language, export / import / restore sample data |

Rules (in `Patina.Core`, all unit tested): the worst finding sets the grade (three moderate findings count as Poor);
survey intervals depend on material and shorten with condition; submitting proposes one treatment per serious or
urgent finding.

## Repository layout

| Path | Contents |
|---|---|
| `Patina/` | Uno Platform single project (MVUX, Navigation Extensions, Toolkit, Material) |
| `Patina.Core/` | Domain records, rules, queries, JSON document store (plain `net10.0`) |
| `Patina.Tests/` | NUnit tests for Core (69) |
| `docs/` | Spec inputs: `DESIGN.md` (theme source), `DECISIONS.md`, `ANALYSIS.md` and `assessment/` (tooling and project review), `KNOWN-ISSUES.md`, `DEPLOY.md` |
| `SPEC.md` | The pre-implementation spec (briefs, route/page/data/design trees, gate) |
| `tools/` | `make_resw.py` (EN/FR strings from one table), Win32 capture/drive scripts, Playwright WASM checks |

## Prerequisites

- .NET SDK 10.0.300 or later (`global.json` pins Uno.Sdk 6.7.30)
- Workloads for the heads you build: `dotnet workload install wasm-tools android ios`
- Android: JDK 17 and the Android SDK (API 36 used here). iOS: a Mac with Xcode.
- `uno-check` fixes most environment gaps: `dotnet tool install -g uno.check && uno-check`

## Build, run, test

```bash
# Tests (Core rules, store, import/export, sample data)
dotnet test Patina.Tests/Patina.Tests.csproj

# Desktop (Windows / macOS / Linux, Skia)
dotnet run --project Patina/Patina.csproj -f net10.0-desktop

# WebAssembly (serve the published folder; any static server with an SPA fallback)
dotnet publish Patina/Patina.csproj -f net10.0-browserwasm -c Release -o publish/wasm
dotnet serve -d publish/wasm/wwwroot -p 8000

# Android (emulator or device attached)
dotnet build Patina/Patina.csproj -f net10.0-android -t:Run
```

Debug builds call `UseStudio()` for Hot Reload / Hot Design. For headless runs set `APP_NO_HOTDESIGN=1`.

## Changing the design

`docs/DESIGN.md` is the source for colour roles, type ramp, spacing and radii. Regenerate
`Patina/Styles/{ColorPaletteOverride,Typography,Tokens}.xaml` with DesignMd2Uno instead of editing them:

```bash
dotnet run --project <DesignMd2Uno>/src/DesignMd2Uno.Cli -- docs/DESIGN.md -o Patina/Styles --font-root ms-appx:///Assets/Fonts/
```

Strings: edit the table in `tools/make_resw.py` and run it; it writes both `Strings/en` and `Strings/fr` and lists
any key used in code or XAML that has no translation.

## Data

Everything is stored on the device in `ApplicationData.LocalFolder`: `patina.json` (the collection, versioned) and
`photos/`. On the web that folder lives in the browser's IndexedDB. Settings > Export writes a `.patina` archive
(collection + photos) to move or back up a collection; Import replaces the device's collection after confirmation.
A damaged `patina.json` is set aside (`patina.corrupt-<time>.json`), never overwritten.

## Status

See `docs/DEPLOY.md` for per-target release steps and what was verified on which head, and
`docs/KNOWN-ISSUES.md` for framework workarounds.
