# Evidence: `{utu:Responsive}` on `ColumnDefinition.Width` does not switch on Skia desktop

- Session: f90a1d6b-7589-4665-aac2-1dfba9bfc389 (Claude Code), 2026-10-03
- Versions: Uno.Sdk 6.7.30, Uno.Toolkit.WinUI 9.1.3, `net10.0-desktop` (Skia, Windows 11)
- Driver: `tools/Drive.ps1` (Win32 resize, twice per resize) and `tools/Capture-Window.ps1` (PrintWindow)

## Symptom

Two-column pages (list + map, artwork photo + record) declared their column widths as Responsive `GridLength`
values on `ColumnDefinition.Width`, with the second column at 0 below the threshold. At 1024 and 1440 px the pages
stayed single-column, as if only the first provided value applied. The original markup was replaced in the same
session and is not preserved verbatim.
Other Responsive values on the same page (`Visibility`, `Margin` on panels) switched normally at the same widths.

## Cause (inferred, partly source-read)

`ColumnDefinition` is a `DependencyObject`, not a `FrameworkElement`. `ResponsiveExtension` connects through its
target's `Loaded`/`Unloaded` and the window size; for non-FrameworkElement targets it needs a proxy host. The Toolkit
source mentions a proxy-host path for `ColumnDefinition` scoped to Windows. Not traced at runtime.

This is consistent with FieldCheck (2026-09-25, WinAppSDK head), where `GridLength` values were applied and the
gotchas file records non-string Responsive values as working: that measurement was on WinAppSDK, this one is on a
Skia head.

## Fix that held

Fixed star columns; the Responsive value moves to the panels (`ArtworkPage.xaml:58-102`, as committed):

```xml
<Grid.ColumnDefinitions>
  <ColumnDefinition Width="5*" />
  <ColumnDefinition Width="4*" />
</Grid.ColumnDefinitions>
<StackPanel Grid.ColumnSpan="{utu:Responsive Narrowest=2, Wide=1}"> <!-- photo --> </StackPanel>
<StackPanel Grid.Row="{utu:Responsive Narrowest=1, Wide=0}"
            Grid.Column="{utu:Responsive Narrowest=0, Wide=1}"
            Grid.ColumnSpan="{utu:Responsive Narrowest=2, Wide=1}"> <!-- record --> </StackPanel>
```

`CollectionPage.xaml:73` uses the same fixed columns; its span later moved to `AdaptiveTrigger` setters because of the
separate Unloaded bug (`2026-10-03-responsive-extension-unloaded.md`). Verified at 420 / 1024 / 1440 px.

## Not established

- Not reduced to a minimal page; not tested on WebAssembly, Android, or the WinAppSDK head in this session.
- `RowDefinition.Height` presumably behaves the same (same type family); not tested.
