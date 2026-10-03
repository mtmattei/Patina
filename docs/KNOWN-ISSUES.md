# Known issues and workarounds

Each entry follows the workaround protocol: symptom, repro, versions, cause, what the app does instead, and where
the workaround lives (`grep WORKAROUND`).

## ResponsiveExtension does not reconnect after its host is unloaded

- **Symptom:** after opening a detail page (Artwork) and going back, every `{utu:Responsive}` value on the main
  shell and the Collection page stops updating. Resizing the window across a breakpoint changes nothing: the rail
  stays at phone width, the map stays visible at 420 px.
- **Repro:** Uno.Sdk 6.7.30, Uno.Toolkit 9.1.3, `net10.0-desktop` (Windows 11). Page with
  `NavigationCacheMode="Required"` and a `{utu:Responsive}` Visibility; navigate to a sibling route that replaces it
  in the shell frame; navigate back; resize the window across the threshold. Fresh launch + resize works; the failure
  needs one unload/reload of the cached page.
- **Cause (source-read):** `src/Uno.Toolkit.UI/Markup/ResponsiveExtension.cs` on `release/stable/9.1`: `Connect`
  subscribes to `ResponsiveHelper.WindowSizeChanged` and to the host's `Unloaded`; `OnHostUnloaded` disconnects.
  The `Loaded` handler is removed after the first connect, so the next `Loaded` of the same host never reconnects.
- **Workaround:** the three cached pages (`MainPage`, `CollectionPage`, `SettingsPage`) use `AdaptiveTrigger`
  visual states for their breakpoints, marked `WORKAROUND(ResponsiveExtension does not reconnect after Unloaded)`.
  Detail pages are recreated per navigation and keep `{utu:Responsive}`.
- **Verified:** wide → artwork → back → 420 px → 1440 px switches correctly in both directions (2026-10-03).
- **Upstream:** not yet filed. Suggested fix: re-subscribe `Loaded` in `OnHostUnloaded` (or keep the `Loaded`
  subscription for the extension's lifetime).

## `{utu:Responsive}` on a `ColumnDefinition.Width` does not switch

- **Symptom:** a two-column layout stayed single-column at 1024 and 1440 px.
- **Cause:** `ColumnDefinition` is not a `FrameworkElement`; the extension needs a proxy host and did not resolve
  (Toolkit source mentions a proxy-host path for ColumnDefinition on Windows only).
- **What the app does:** fixed star columns; the responsive value moves to the panel (`Grid.ColumnSpan`,
  `Grid.Row`, `Grid.Column`). See `ArtworkPage.xaml`, `CollectionPage.xaml`.

## Material outlined card shows a hover tint on a static form container

- `CardContentControl` styles carry pointer-over and pressed overlays for clickable cards. On the survey's
  "Add a finding" container that tint suggested the whole card was clickable. The container uses a static style
  (`StaticCardContentControlStyle`) instead.

## Calendar date picker shows the system date format

- `CalendarDatePicker` formats with the OS region (US `10/7/2026` on the test machine) unless `DateFormat` is set.
  The treatment page sets `DateFormat` to the app's day-month-year form.
