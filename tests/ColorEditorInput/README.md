# Color-field propagation

Run `dotnet run --project tests/ColorEditorInput`. Real palette-popup fields exercise familiar
colors that Avalonia serializes as names, ordinary numeric colors, and the existing raw-ROM
versus rendered channel order. The pre-fix `7FFF` case failed: the field became `White`, which
the swatch's hex-only decoder silently rejected before raising ResultChanged.

The 93 checks cover six 16-bit codes, eight color words, exact selected-color bytes, palette
view-model values, focus/popup retention, unrelated bytes and metadata, bare/hash-prefixed
short/full/ARGB theme hex, named swatches, and invalid-draft exclusion. No Core numeric/color
encoding rules change. PalettePopup is a test helper for the code-created color popup, which
has a separate input root and no XAML name scope. Tests use only synthetic ROM data.
