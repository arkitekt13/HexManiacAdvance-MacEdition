# Image-editor secondary gestures

Run `dotnet run --project tests/ImageEditorInput` or the complete Mac suite. The real MainWindow
and image editor use fabricated compressed/uncompressed 4bpp sprites and palettes. The pre-fix
Control-left-click painted color 7 into a color-1 pixel instead of sampling it.

The 38 checks cover physical secondary-click and Mac Control-click, retaining/releasing Control,
no ROM/history/metadata changes while sampling, motion after release, normal primary drawing,
live bitmap changes, Cmd+Z/Cmd+Shift+Z, exact bytes/pointers/metadata and edited-pixel sampling.
The view retains the physical button for release while routing the gesture through the shared
eyedropper strategy. No sprite decoding, encoding, tool or history algorithm changes.

SyntheticImageFixture is shared with the palette input/history groups. Native trackpad input
remains for confirmation; tests use no user ROMs or real clipboard/image import files.
