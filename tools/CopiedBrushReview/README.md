# Copied brush and border investigation

Mac-only synthetic real-control investigation; not a passing regression suite. It modifies no
production source and opens no user ROM. Build from the repository root:

```sh
dotnet restore tools/CopiedBrushReview/CopiedBrushReview.csproj --ignore-failed-sources -p:NuGetAudit=false
dotnet build tools/CopiedBrushReview/CopiedBrushReview.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1 -nr:false -v:q
dotnet tools/CopiedBrushReview/bin/Debug/net8.0/CopiedBrushReview.dll stamp copied
dotnet tools/CopiedBrushReview/bin/Debug/net8.0/CopiedBrushReview.dll stamp preserve
dotnet tools/CopiedBrushReview/bin/Debug/net8.0/CopiedBrushReview.dll nine explicit
dotnet tools/CopiedBrushReview/bin/Debug/net8.0/CopiedBrushReview.dll border-drag single
```

Final matrix: stamp/rectangle/nine/border each with copied, explicit, preserve and ui-clear;
stamp/rectangle each with toggle and recopy; border-drag/border-drag-right each with single and
multi; nine with late-explicit. Each invocation runs in a fresh process. The special
border-drag-before operation is for comparing a historical build without assuming its capture
owner; it is not part of the current 25-scenario matrix.

`explicit` updates the actual bound Movement selector; `ui-clear` verifies that empty selection
does not enable preserve. `preserve` and `toggle` deliberately assign CollisionIndex=-1 through
Core while a copied brush remains active; no selectable preserve option exists in the current
Mac dropdown. This distinction is essential to interpreting the findings.

Actual Control selection and pointer painting are used. Border probes remove only the fixture's
slide-panel transition before opening it, then verify the border image is the real hit target
and owns capture. No native animation timing claim is made. Nine-grid draws an L-shaped union
of three 2x2 stamps so its center exercises an automatically discovered inner corner.

Setup, unrelated-byte, metadata, in-memory Save/reload and exact Undo/Redo controls throw on
failure. Behavioral observations are separate: `OBSERVATION:...:FAIL` records a reproduced
unmet invariant, not a passing assertion. Exit zero means investigation completion, including
known failures. Cases with no observations merely record raw data where semantics are not
assumed, particularly in-bounds border movement bits and copied-mode derived nine-grid corners.

Evidence, counts and proposed fixes: [COPIED_BRUSH_REVIEW.md](../../tests/COPIED_BRUSH_REVIEW.md).
