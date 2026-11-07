# Editor scrollbar layout

Run the group through `scripts/verify-mac.sh`, or build and execute it separately:

```sh
dotnet restore tests/EditorScrollbars/EditorScrollbars.csproj --ignore-failed-sources -p:NuGetAudit=false
dotnet build tests/EditorScrollbars/EditorScrollbars.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false
dotnet tests/EditorScrollbars/bin/Debug/net8.0/EditorScrollbars.dll
```

Uses actual Avalonia/Skia templates in headless windows, synthetic text, and viewer instances
from the production MapTab/TabView XAML. No personal ROM, settings, emulator or native window
is loaded. The source TextEditor has overflowing content in both directions. Geometry must
keep its content presenter outside each visible scrollbar, including after hover and resizing;
its separately painted syntax must clip to the same viewport. Scrolling preserves exact source
and selection.

Map blocks, events, header, tile editor, table, image and multi-script viewer instances are
extracted from their XAML parents and hosted with controlled overflowing content. At widths
220 and 420, content must stay outside the scrollbar gutter, a right-edge button must receive
real headless pointer clicks after scrollbar hover, and vertical scrolling must work. These are
layout/input checks, not actual event-pointer writes, map painting or script compilation tests;
the existing map/script regression groups cover those workflows.

On checkpoint ccb765fa, the first content-viewport assertion fails because the script editor's
content extends beneath the scrollbar. The production repair reserves space using the existing
AllowAutoHide setting. The custom TextBox template must forward that setting, and syntax paint
must respect the narrower viewport. The final group passes 78 assertions.

Native trackpad feel, Retina rendering and real-ROM panel appearance remain manual checks.
