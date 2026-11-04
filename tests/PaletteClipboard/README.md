# Focus-aware palette clipboard routing

Run `dotnet run --project tests/PaletteClipboard` or the complete Mac suite. Before the fix,
Cmd+C in a focused palette threw because its filesystem parameter was captured before attachment.
After that was isolated, native Edit-menu Copy still targeted the image tab. Disabled palette
Paste could also reach an ancestor image binding with a null parameter.

The 40 checks drive keyboard, native Edit-menu and real palette context-menu commands. They
require existing color text/encoding, exact palette-only writes, untouched sprite/pointer bytes,
Undo/Redo, live clipboard availability, safe null/invalid text with a valid image payload,
focused palette Select All, unsupported Cut exclusion, disabled gradient routing, popup text
priority and valid image-copy fallback. Script/text ClipboardRouting remains a separate group.

Image-canvas bindings now decline keys owned by text, combo, slider and palette controls and
resolve clipboard parameters when invoked. Palette bindings consume disabled operations to
prevent fallback. Tests use a stub filesystem, synthetic ROM and headless text clipboard.
