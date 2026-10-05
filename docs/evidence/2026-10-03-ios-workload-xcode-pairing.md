# Evidence: the .NET iOS workload needs one exact Xcode version

- Session: f90a1d6b-7589-4665-aac2-1dfba9bfc389 (Claude Code), 2026-10-03
- Versions: .NET SDK 10.0.3xx band, Uno.Sdk 6.7.30, GitHub Actions `macos-latest` (default Xcode 26.6 on that date)
- Job: `iOS (simulator build check)` in `.github/workflows/ci.yml`, `dotnet build -f net10.0-ios -c Release -r iossimulator-arm64`

## Symptom

The iOS job failed at build time on every attempt until the Xcode version and the workload version were pinned as a pair.
The error names the exact Xcode version the workload expects:

| Run | Commit | Workload | Xcode selected | Result |
|---|---|---|---|---|
| 37131333801 | 613572c | latest (`Microsoft.iOS.Sdk.net10.0_27.0` 27.0.10722) | 26.6 (image default) | `This version of .NET for iOS (27.0.10722) requires Xcode 27.0. The current version of Xcode is 26.6.` |
| 37131522638 | 0371f44 | workload set 10.0.300 (`Microsoft.iOS.Sdk.net10.0_26.2` 26.2.10233) | 26.6 (image default) | `This version of .NET for iOS (26.2.10233) requires Xcode 26.3. The current version of Xcode is 26.6.` |
| 37131657724 | eee7490 | 10.0.300 | auto-detect script (parse "requires Xcode X" from a first build, pick the matching `/Applications/Xcode_X*.app`) | failed; the detection step did not select a working Xcode |
| 37131898208 | e175e2b | 10.0.300 | explicit `sudo xcode-select -s /Applications/Xcode_26.3.app` | **success** (iOS job ~42 min) |

## What held

```yaml
- name: Pin Xcode and the iOS workload together
  run: |
    sudo xcode-select -s /Applications/Xcode_26.3.app
    xcodebuild -version
    dotnet workload install ios --version 10.0.300
```

## Rule derived

- The iOS workload accepts one Xcode version, not a minimum. A newer Xcode fails exactly like an older one.
- "Latest workload" tracks the newest Xcode, which hosted runners may not have yet. Pin a workload set and the
  matching `Xcode_<ver>.app` together, and bump both in one commit when the runner image changes.
- Keep `xcodebuild -version` in the step so the log shows the pairing.

## Not established

- Not run on a Mac or a device; this is a simulator compile check only.
- The auto-detect attempt's exact failure was not diagnosed; an explicit pin was simpler and was kept.
