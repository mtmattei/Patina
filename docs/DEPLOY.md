# Deploying Patina

Version 1.0.0. One codebase, four Uno Platform heads. CI (`.github/workflows/ci.yml`) builds every target on each
push to `main` and uploads the artifacts; this page covers what each target needs to reach users.

## What was verified, and where

Evidence is the session of 2026-10-03 on Windows 11 (Uno.Sdk 6.7.30, .NET SDK 10.0.303). "Built" means a clean
build; "Run" means launched and driven through the primary flows with screenshots.

| Target | Built here | Run here | Flows exercised |
|---|---|---|---|
| Windows desktop (`net10.0-desktop`, Skia) | yes | yes | collection + map, pin callout, artwork, survey submit, treatment to done, settings, dark theme, French, 420 / 1024 / 1440 px widths, back-navigation breakpoints |
| WebAssembly / PWA (`net10.0-browserwasm`) | yes (Release, trimmed) | yes, headless Edge | first frame in 7.5 s, no console errors, map tiles, survey draft persisted across a page reload (IndexedDB); also served under `/Patina/` with `WasmShellWebAppBasePath` (GitHub Pages layout) |
| Android (`net10.0-android`, API 36 emulator) | yes (Debug and Release) | yes | Debug: collection, map with pins and callout, artwork, survey, camera capture into a draft, system Back. Release APK (trimmed): launch, collection, map |
| Windows desktop, published Release (self-contained win-x64) | yes | yes | launch to collection in 10 s |
| Linux desktop | publish only | **no** | WSL is not installed on the build machine |
| macOS desktop | publish only | **no** | needs a Mac |
| iOS (`net10.0-ios`) | **no** | **no** | needs a Mac with Xcode; CI builds it for the simulator |

## Desktop (Windows, macOS, Linux)

```bash
dotnet publish Patina/Patina.csproj -f net10.0-desktop -c Release -r win-x64   --self-contained -p:PatinaTargetFrameworks=net10.0-desktop -o publish/win-x64
dotnet publish Patina/Patina.csproj -f net10.0-desktop -c Release -r linux-x64 --self-contained -p:PatinaTargetFrameworks=net10.0-desktop -o publish/linux-x64
dotnet publish Patina/Patina.csproj -f net10.0-desktop -c Release -r osx-arm64 --self-contained -p:PatinaTargetFrameworks=net10.0-desktop -o publish/osx-arm64
```

`PatinaTargetFrameworks` limits the restore to the desktop head (see the comment in `Patina.csproj`). Ship the folder as a zip (CI uploads one per RID). Linux needs X11 or Wayland with XWayland, plus `libfontconfig`
and `libgl1` (Mesa) on minimal distributions.

Remaining for a signed release:
- **Windows:** an Authenticode code-signing certificate (otherwise SmartScreen warns on first run). Uno's
  MSIX packaging for the desktop head is an alternative if Store distribution is wanted.
- **macOS:** an Apple Developer ID certificate and notarization (`codesign` + `notarytool`); without them Gatekeeper
  blocks the app. Wrapping the publish output in a `.app` bundle is required for Finder launch.
- **Linux:** none for a zip; Snap or Flatpak packaging is optional.

## WebAssembly (installable PWA)

```bash
dotnet workload install wasm-tools
dotnet publish Patina/Patina.csproj -f net10.0-browserwasm -c Release -o publish/wasm
# deploy publish/wasm/wwwroot
```

Hosting requirements:
- **SPA fallback.** Navigation writes deep links (`/Main/Collection`) into the address bar; the host must serve
  `index.html` for unknown paths. `staticwebapp.config.json` (Azure Static Web Apps) is included; for GitHub Pages
  the workflow copies `index.html` to `404.html`; for nginx use `try_files $uri /index.html`.
- **Sub-path hosting.** Asset paths are absolute. Publishing under `https://host/<path>/` needs
  `-p:WasmShellWebAppBasePath=/<path>/` (the Pages job does this).
- **MIME types and compression:** `application/wasm`; serve the precompressed `.br` / `.gz` files when present.
- **Storage:** the collection lives in the browser's IndexedDB for that origin. Clearing site data deletes it;
  remind users to use Settings > Export for backups.
- **GitHub Pages:** set the repository variable `DEPLOY_PAGES=true` and Pages source "GitHub Actions"; the `pages`
  job then deploys on every push to `main`.

## Android

```bash
dotnet workload install android
dotnet publish Patina/Patina.csproj -f net10.0-android -c Release -p:PatinaTargetFrameworks=net10.0-android -p:AndroidPackageFormat=aab
dotnet publish Patina/Patina.csproj -f net10.0-android -c Release -p:PatinaTargetFrameworks=net10.0-android -p:AndroidPackageFormat=apk
```

Without a keystore the Release build is signed with the debug key: fine for side-loading and testing, refused by
Google Play. For Play:
1. Create an upload key: `keytool -genkeypair -v -keystore patina.keystore -alias patina -keyalg RSA -keysize 2048 -validity 10000`
2. Add repository secrets `ANDROID_KEYSTORE_BASE64` (base64 of the keystore), `ANDROID_KEY_ALIAS`,
   `ANDROID_KEY_PASSWORD`, `ANDROID_STORE_PASSWORD`. The CI job signs the `.aab` when they exist.
3. A Google Play Console developer account (one-time fee), the app listing, a privacy policy URL (the app stores
   data only on the device and requests the camera), and the data-safety form.
4. Bump `ApplicationVersion` (integer) and `ApplicationDisplayVersion` in `Patina.csproj` for each upload.

## iOS

Needs a Mac with Xcode and the .NET `ios` workload:

```bash
dotnet publish Patina/Patina.csproj -f net10.0-ios -c Release -p:ArchiveOnBuild=true \
  -p:CodesignKey="Apple Distribution: <Team>" -p:CodesignProvision="<profile name>"
```

Remaining: an Apple Developer Program membership, an App ID (`app.patina.conservation`), a distribution certificate
and an App Store provisioning profile, App Store Connect listing and privacy details. Camera and photo-library
usage strings are already in `Platforms/iOS/Info.plist`. Never run on a device or simulator from this machine.

## External services

- **OpenStreetMap tiles** (`tile.openstreetmap.org`) are used under the OSM tile usage policy with an identifying
  User-Agent on native heads. The policy discourages heavy use by distributed apps: before a wide rollout, switch
  to a tile provider with an agreement (MapTiler, Stadia, Thunderforest, or a self-hosted server) by replacing the
  tile source in `Patina/Controls/ArtworkMap.cs`. Attribution is shown on every map.
- No other network service, account or credential is used. There are no secrets in the repository.

## Local release artifacts (2026-10-03)

Built on the Windows machine into `_publish/release/` (not committed; CI produces the same set):
`patina-1.0.0-desktop-{win-x64,linux-x64,osx-arm64}.zip`, `patina-1.0.0-web.zip`,
`patina-1.0.0-android-debugsigned.apk`, and `SHA256SUMS.txt`. The Android App Bundle for Play is produced by CI
(signed when the keystore secrets exist); the local AAB was overwritten by the APK publish and is not included.
