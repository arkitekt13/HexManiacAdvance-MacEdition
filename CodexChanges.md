# Codex Changes

This log records changes we make in this fork, starting with our local work; it does not repeat changes inherited from the original projects.

## Entry format

- Add a numbered section for each fix or feature, in chronological order.
- Use the same three columns for every entry: **File Name**, **Line**, and **Summary**.
- Add one row per meaningful change; use a repository-relative file path and a single starting line number.
- Keep each summary to one sentence describing what changed and why.
- Line numbers refer to the file when the entry was written and may shift after later edits.
- Include supporting tests and configuration changes, and record validation below the table.
- Record completed work only; identify any verification still pending.
- Do not add entries for creating or maintaining this log.

## 001 — Fix overlapping text in the script editor

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TextEditor.axaml](src/HexManiac.Avalonia/Controls/TextEditor.axaml) | 14 | Give the editing and highlighting layers a shared monospaced font with Menlo preferred on macOS so character widths remain aligned. |
| [src/HexManiac.Avalonia/Controls/TextEditor.axaml](src/HexManiac.Avalonia/Controls/TextEditor.axaml) | 16 | Register and apply a display converter to the bound syntax layers so invisible placeholders retain their intended spacing. |
| [src/HexManiac.Avalonia/Controls/SyntaxLayerTextConverter.cs](src/HexManiac.Avalonia/Controls/SyntaxLayerTextConverter.cs) | 13 | Replace zero-width NUL placeholders with ordinary spaces for display while preserving character indices, line breaks, and editable script content. |
| [src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs](src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs) | 197 | Normalize the manually rendered comment layer before creating its inline runs so comments and error markings use the same spacing as the other layers. |
| [tests/ScriptEditorLayout/ScriptEditorLayout.csproj](tests/ScriptEditorLayout/ScriptEditorLayout.csproj) | 1 | Add a small regression-check executable that references the actual Mac editor without requiring a new test framework. |
| [tests/ScriptEditorLayout/Program.cs](tests/ScriptEditorLayout/Program.cs) | 21 | Reproduce zero-width placeholders and check monospaced metrics, all six display bindings, token positions, comments, line endings, and unchanged editable text using Avalonia and Skia without opening a window. |
| [tests/ScriptEditorLayout/README.md](tests/ScriptEditorLayout/README.md) | 1 | Document how to run the automated layout check and manually inspect script alignment, scrolling, caret placement, and selection. |
| [.gitignore](.gitignore) | 12 | Ignore generated bin directories so compiled regression-check output stays out of version control. |

Validation: The editor and regression-check project built with zero warnings and zero errors, the regression check passed on this Mac, and the user confirmed improved readability with a screenshot showing aligned commands, arguments, comments, and dialogue in the reported NPC script.

Pending verification: Check scrolling, caret placement, and selection in the running editor.

## 002 — Keep map tutorial descriptions readable on hover

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml](src/HexManiac.Avalonia/Controls/TutorialControl.axaml) | 18 | Clip the tutorial cards to a viewport that follows the panel width while keeping the full cards aligned to its left edge. |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml](src/HexManiac.Avalonia/Controls/TutorialControl.axaml) | 40 | Restore the card pointer-entry handler so hovering a tutorial can highlight its associated map control. |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs](src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs) | 33 | Expand the actual right-anchored panel from 100 to 400 pixels on hover instead of sliding the cards outside a narrow layout and hover area. |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs](src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs) | 80 | Position the completion checkmark relative to the icon column in both collapsed and expanded layouts. |
| [tests/TutorialLayout/TutorialLayout.csproj](tests/TutorialLayout/TutorialLayout.csproj) | 1 | Add a regression-check executable referencing the real tutorial control without requiring a ROM or an additional test framework. |
| [tests/TutorialLayout/Program.cs](tests/TutorialLayout/Program.cs) | 20 | Check repeated pointer entry and exit at two host widths to verify right-edge anchoring and containment of all five visible tutorial descriptions. |
| [tests/TutorialLayout/README.md](tests/TutorialLayout/README.md) | 1 | Document how to run the tutorial layout check and verify hover, navigation between cards, dismissal, and collapse with a real mouse. |

Validation: The editor and tutorial regression-check project built with zero warnings and zero errors, the offscreen geometry check passed for three expansion/collapse cycles at each of two host widths, and the user confirmed the original hover clipping was resolved.

Follow-up: The user's requested compact sizing and reported overlay/preview issues are addressed in entry 003.

## 003 — Make tutorials compact and keep map previews out of overlays

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml](src/HexManiac.Avalonia/Controls/TutorialControl.axaml) | 14 | Reduce the collapsed strip to 72 pixels and the five-card height to 400 pixels while giving the panel a transparent hit-test background. |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml](src/HexManiac.Avalonia/Controls/TutorialControl.axaml) | 33 | Position cards using the compact row height and reduce their icons and typography within a 320-pixel expanded panel. |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs](src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs) | 23 | Keep row positioning and completion animations aligned with the smaller 80-pixel cards without changing Core's tutorial models. |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs](src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs) | 39 | Ease the panel's width over 160 milliseconds so hover expansion no longer jumps instantly. |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs](src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs) | 43 | Consume tutorial pointer presses, releases, and wheel events before they reach map interaction handlers. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml](src/HexManiac.Avalonia/Controls/MapTab.axaml) | 1240 | Place tutorials above the map arrows and selection overlays so those elements cannot draw over or intercept the cards. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 46 | Close map previews when leaving the tab or moving onto another control and clear them immediately on tutorial entry. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 129 | Delegate preview ownership and caching to a map-surface controller instead of attaching NPC dialogue to the entire tab. |
| [src/HexManiac.Avalonia/Controls/MapHoverPreview.cs](src/HexManiac.Avalonia/Controls/MapHoverPreview.cs) | 20 | Restrict preview sources to the map surface and its children so overlay controls cannot trigger NPC dialogue. |
| [src/HexManiac.Avalonia/Controls/MapHoverPreview.cs](src/HexManiac.Avalonia/Controls/MapHoverPreview.cs) | 23 | Remove the attached tooltip on exit while retaining cached content for re-entering the same NPC cell and clearing it over empty cells. |
| [tests/TutorialLayout/Program.cs](tests/TutorialLayout/Program.cs) | 58 | Update layout checks for compact dimensions and verify that all 36 descriptions fit their cards. |
| [tests/TutorialLayout/Program.cs](tests/TutorialLayout/Program.cs) | 94 | Check tutorial stacking configuration and preview source filtering, dismissal, cached re-entry, and empty-cell clearing without a native window. |
| [tests/TutorialLayout/README.md](tests/TutorialLayout/README.md) | 3 | Document the compact-layout and preview checks along with the remaining live mouse and animation verification. |

Validation: The editor and updated regression-check project built with zero warnings and zero errors, and the compact layout, full-description fit, stacking configuration, and preview lifecycle checks passed.

Pending verification: In the running app, check the smaller cards and expansion animation, confirm arrows stay behind them, and move from an NPC to the tutorials and back to verify preview dismissal and re-entry.

## 004 — Restore missing rotated map arrows

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Resources/MarkupExtensions.cs](src/HexManiac.Avalonia/Resources/MarkupExtensions.cs) | 100 | Wrap icon geometries when Avalonia cannot clone them so rotated arrows render while their shared resources retain their original transforms. |
| [tests/MapArrowIcons/MapArrowIcons.csproj](tests/MapArrowIcons/MapArrowIcons.csproj) | 1 | Add a regression-check executable using the actual editor resources and renderer without opening a window. |
| [tests/MapArrowIcons/Program.cs](tests/MapArrowIcons/Program.cs) | 18 | Reproduce the three rotated-icon cloning failures and verify all six arrow types render visible pixels repeatedly without changing shared resources. |
| [tests/MapArrowIcons/README.md](tests/MapArrowIcons/README.md) | 1 | Document the automated arrow rendering check and the remaining visual check in the map editor. |

Validation: The editor and both arrow and tutorial regression projects built with zero warnings and zero errors; all six arrow rendering checks and the existing tutorial layout and preview checks passed.

Pending verification: Relaunch the editor and confirm the left, top, and bottom arrows appear correctly on a map.

## 005 — Fix map focus animation crash and improve arrow contrast

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs](src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs) | 705 | Route the focus outline through a shared animation helper instead of applying a transform animation directly to a TranslateTransform and triggering an InvalidCastException. |
| [src/HexManiac.Avalonia/Controls/FocusOutlineAnimation.cs](src/HexManiac.Avalonia/Controls/FocusOutlineAnimation.cs) | 14 | Animate position and size together on the owning control and hide and reset the outline when the animation finishes. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml](src/HexManiac.Avalonia/Controls/MapTab.axaml) | 1105 | Use the main theme foreground for all map arrows to increase contrast against their button backgrounds. |
| [tests/FocusAnimation/FocusAnimation.csproj](tests/FocusAnimation/FocusAnimation.csproj) | 1 | Add a regression executable that runs the actual focus animation without requiring a native window or ROM. |
| [tests/FocusAnimation/Program.cs](tests/FocusAnimation/Program.cs) | 16 | Reproduce the reported cast failure and drive three corrected animations through movement, resizing, completion, and cleanup using Avalonia's animation clock. |
| [tests/FocusAnimation/README.md](tests/FocusAnimation/README.md) | 1 | Document the animation check, its version-dependent clock adapter, and live map-editor verification. |
| [tests/MapArrowIcons/Program.cs](tests/MapArrowIcons/Program.cs) | 65 | Check the production arrow brushes, render complete button backgrounds and foregrounds, and compare rotated arrow shapes with an edge antialiasing tolerance. |
| [tests/MapArrowIcons/README.md](tests/MapArrowIcons/README.md) | 7 | Document the additional arrow color and rotation checks. |

Validation: The editor and both regression projects built with zero warnings and zero errors; the exact cast failure was reproduced, three corrected focus animations completed successfully, and arrow rendering, color, rotation, and shared-resource checks passed; the user confirmed the crash is fixed, and complete button rendering confirmed all six backgrounds use the same opaque #383838 in the default theme.

Pending verification: The user still perceives a top-versus-side background difference in the live map; the automated renderer shows identical backgrounds, but screenshot pixel sampling was unavailable, so the apparent contrast effect remains an inference.


## 006 — Add a purple script welcome screen for Mac

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/StartScreen.axaml](src/HexManiac.Avalonia/Controls/StartScreen.axaml) | 1 | Replace the unused information page with a dark welcome screen featuring purple script lettering, a white Mac subtitle, a subtle glow, and a layout that scales to smaller windows. |
| [src/HexManiac.Avalonia/Controls/StartScreen.axaml](src/HexManiac.Avalonia/Controls/StartScreen.axaml) | 40 | Connect Open File to the existing editor command and show the existing keyboard shortcut and ROM drop option. |
| [src/HexManiac.Avalonia/Controls/StartScreen.axaml](src/HexManiac.Avalonia/Controls/StartScreen.axaml) | 53 | Display and dismiss file-opening errors on welcome so they remain accessible while the workspace is hidden. |
| [src/HexManiac.Avalonia/Controls/StartScreen.axaml.cs](src/HexManiac.Avalonia/Controls/StartScreen.axaml.cs) | 7 | Populate the footer from the actual application assembly version and compile-time Debug or Release configuration without changing the version number. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml](src/HexManiac.Avalonia/Windows/MainWindow.axaml) | 267 | Hide the empty editor workspace while welcome is visible so its controls cannot receive keyboard focus underneath the screen. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml](src/HexManiac.Avalonia/Windows/MainWindow.axaml) | 584 | Mount welcome when the tab count is zero so it disappears on opening a ROM and returns after the final tab closes. |
| [tests/WelcomeScreen/WelcomeScreen.csproj](tests/WelcomeScreen/WelcomeScreen.csproj) | 1 | Add a regression executable referencing the real welcome control without requiring a ROM or native window. |
| [tests/WelcomeScreen/Program.cs](tests/WelcomeScreen/Program.cs) | 17 | Verify layout at three window sizes, command wiring, tab-count visibility, error display, shell mounting, and the version footer with optional rendered preview output. |
| [tests/WelcomeScreen/README.md](tests/WelcomeScreen/README.md) | 1 | Document the welcome regression checks and remaining native file-picker and launch verification. |

Validation: The app and regression project built with zero warnings and zero errors; welcome layout, Open command binding, zero-tab lifecycle, error display, shell mounting, and version checks passed, and the rendered preview was visually inspected.

Pending verification: Relaunch on macOS and check the native picker, cancellation, Cmd+O, dropping a ROM, direct Finder opening, and returning to welcome after closing the final tab.

## 007 — Refine welcome proportions and fix the partial button highlight

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/StartScreen.axaml](src/HexManiac.Avalonia/Controls/StartScreen.axaml) | 15 | Apply hover and pressed colors to the full button surface and add a visible keyboard-focus outline. |
| [src/HexManiac.Avalonia/Controls/StartScreen.axaml](src/HexManiac.Avalonia/Controls/StartScreen.axaml) | 36 | Reduce the script title and subtitle sizes and tighten their spacing while scaling only the lettering down for smaller windows. |
| [src/HexManiac.Avalonia/Controls/StartScreen.axaml](src/HexManiac.Avalonia/Controls/StartScreen.axaml) | 47 | Keep Open File at a fixed size and give it a local rounded template so the global button theme cannot leave a smaller highlighted rectangle behind the label. |
| [tests/WelcomeScreen/Program.cs](tests/WelcomeScreen/Program.cs) | 56 | Exercise the production button's hover, pressed, exit, and keyboard-focus states and verify its rounded surface covers the entire button. |
| [tests/WelcomeScreen/README.md](tests/WelcomeScreen/README.md) | 8 | Describe the full-surface button state checks. |

Validation: The app and welcome regression project built with zero warnings and zero errors; button state, focus, layout, command, visibility, error, and version checks passed, and the refined rendered preview was visually inspected.

Pending verification: Relaunch and confirm the full button highlights under a real mouse and assess the smaller lettering and tighter spacing.

## 008 — Use a bold purple welcome title

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/StartScreen.axaml](src/HexManiac.Avalonia/Controls/StartScreen.axaml) | 39 | Replace the main title's formal script with bold purple sans-serif lettering at a size that fits the existing layout while retaining the white Mac script subtitle. |

Validation: The app and welcome regression project built with zero warnings and zero errors; the existing layout and button checks passed, and the rendered preview was visually inspected.

Pending verification: Relaunch to review the new title in the running app.

## 009 — Add one reliable Mac verification command

| File Name | Line | Summary |
| --- | --- | --- |
| [scripts/verify-mac.sh](scripts/verify-mac.sh) | 1 | Discover, rebuild, and run every headless regression group while failing empty runs and missing, duplicate, zero, or unsuccessful assertion reports. |
| [tests/README.md](tests/README.md) | 1 | Document the unified command, assertion-count contract, and the original xUnit suite's remaining portability work. |
| [tests/FocusAnimation/Program.cs](tests/FocusAnimation/Program.cs) | 13 | Count completed assertions and emit a machine-readable successful-run report. |
| [tests/MapArrowIcons/Program.cs](tests/MapArrowIcons/Program.cs) | 16 | Count completed assertions and emit a machine-readable successful-run report. |
| [tests/ScriptEditorLayout/Program.cs](tests/ScriptEditorLayout/Program.cs) | 18 | Count completed assertions and emit a machine-readable successful-run report. |
| [tests/TutorialLayout/Program.cs](tests/TutorialLayout/Program.cs) | 18 | Count completed assertions and emit a machine-readable successful-run report. |
| [tests/WelcomeScreen/Program.cs](tests/WelcomeScreen/Program.cs) | 20 | Count completed assertions and emit a machine-readable successful-run report. |
| [tests/VerificationRunner/VerificationRunner.csproj](tests/VerificationRunner/VerificationRunner.csproj) | 1 | Add a dependency-free executable for verification-runner failure cases. |
| [tests/VerificationRunner/Program.cs](tests/VerificationRunner/Program.cs) | 1 | Exercise empty discovery, silent or ambiguous success, restore/build/process failure, and successful totals using isolated fake processes. |
| [tests/VerificationRunner/README.md](tests/VerificationRunner/README.md) | 1 | Describe the runner checks and their isolation from ROMs and native windows. |

Validation: All six regression groups passed with 547 assertions; the runner's platform guard was then rebuilt and its nine failure-path assertions passed with zero warnings or errors.

Scope note: The original xUnit suite is not part of this count and is not claimed as passing; its restoration and platform-specific build configuration remain separate work.

## 010 — Preserve documents and existing files when saving fails

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/Models/AtomicFileWriter.cs](src/HexManiac.Core/Models/AtomicFileWriter.cs) | 1 | Stage and flush complete files beside their destinations before replacement while retaining permissions, symbolic links, read-only protection, and cleanup after failed writes. |
| [src/HexManiac.Core/ViewModels/ViewPort.cs](src/HexManiac.Core/ViewModels/ViewPort.cs) | 654 | Keep Save, Save As, and close operations unsaved or open when writing ROM metadata fails so the user can retry. |
| [src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs](src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs) | 292 | Replace ROM and metadata writes through the staged writer, restore file watchers in finally, and return cancellation rather than discard after a requested save fails. |
| [src/HexManiac.WPF/Implementations/WindowsFileSystem.cs](src/HexManiac.WPF/Implementations/WindowsFileSystem.cs) | 221 | Apply the same staged-write and null-on-save-error contract to the original Windows adapter. |
| [tests/SaveSafety/SaveSafety.csproj](tests/SaveSafety/SaveSafety.csproj) | 1 | Add headless save regressions using the actual shared engine and copied reference resources. |
| [tests/SaveSafety/Program.cs](tests/SaveSafety/Program.cs) | 1 | Test save failures, metadata retry, close cancellation, shrinking replacements, UTF-8, permissions, symbolic links, and read-only files with synthetic data. |
| [tests/SaveSafety/README.md](tests/SaveSafety/README.md) | 1 | Document the save checks and the separate-file and external-lock limitations. |

Validation: All seven regression groups passed with 564 assertions; Mac application and regression builds passed, including staged-write failure injection and the previously failing metadata-save case.

Limitations: ROM and metadata remain separate writes rather than a two-file power-loss transaction; a failed metadata write retains an open unsaved document for retry, and the Windows WPF application/native dialogs have not been built or exercised on this Mac.

## 011 — Validate patches before changing ROM data

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/Models/Code/Patcher.cs](src/HexManiac.Core/Models/Code/Patcher.cs) | 8 | Parse bounded IPS and UPS candidates off-model and commit changed spans only after complete structural and checksum validation. |
| [src/HexManiac.Core/Models/Code/Patcher.cs](src/HexManiac.Core/Models/Code/Patcher.cs) | 12 | Fix IPS generation at the last changed byte, reserved EOF offset, zero-filled expansion, and truncation boundaries. |
| [src/HexManiac.Core/Models/Code/Patcher.cs](src/HexManiac.Core/Models/Code/Patcher.cs) | 124 | Include truncated source tails in UPS generation so reverse application restores the original bytes. |
| [src/HexManiac.Core/ViewModels/ViewPort.cs](src/HexManiac.Core/ViewModels/ViewPort.cs) | 1468 | Preserve existing edits and undo history after rejected patch imports and report that validation failures leave the ROM unchanged. |
| [tests/PatchSafety/PatchSafety.csproj](tests/PatchSafety/PatchSafety.csproj) | 1 | Add synthetic patch regressions with shared engine reference resources. |
| [tests/PatchSafety/Program.cs](tests/PatchSafety/Program.cs) | 1 | Check randomized forward and reverse round trips, empty and zero-tail cases, malformed inputs, reserved offsets, size limits, and undo preservation. |
| [tests/PatchSafety/README.md](tests/PatchSafety/README.md) | 1 | Document patch validation, supported bounds, and the retained history-reset behavior for successful UI patch imports. |

Validation: All eight regression groups passed with 921 assertions, including the previously failing output-checksum and truncated-IPS cases; the final added IPS truncation guard is covered by subsequent verification runs.

Limitations: Successful whole-ROM patch imports still rebuild metadata and reset history as before; failed imports preserve history, and this entry does not claim successful patch imports are undoable or that native dialogs were tested.

## 012 — Validate complete project imports before applying edits

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Project/ProjectExport.cs](src/HexManiac.Avalonia/Project/ProjectExport.cs) | 66 | Version new exports and fingerprint table layouts so imports can reject incompatible structures before editing data. |
| [src/HexManiac.Avalonia/Project/ProjectExport.cs](src/HexManiac.Avalonia/Project/ProjectExport.cs) | 94 | Preserve escaped carriage returns and reject incomplete or unknown text escapes instead of silently dropping characters. |
| [src/HexManiac.Avalonia/Project/ProjectExport.cs](src/HexManiac.Avalonia/Project/ProjectExport.cs) | 111 | Validate manifests, tables, columns, indices, numeric widths, and lossless text encoding with structured file/row/field diagnostics before requesting a change token. |
| [src/HexManiac.Avalonia/Project/ProjectExport.cs](src/HexManiac.Avalonia/Project/ProjectExport.cs) | 205 | Apply a standalone undo token only after validation and roll it back on failure without discarding earlier undo or redo history. |
| [tests/ProjectImport/ProjectImport.csproj](tests/ProjectImport/ProjectImport.csproj) | 1 | Add synthetic import regressions with shared reference resources. |
| [tests/ProjectImport/Program.cs](tests/ProjectImport/Program.cs) | 1 | Test invalid numeric/text input, diagnostics, unchanged round trips, one-step undo, redo retention, and rollback after an injected second-field failure. |
| [tests/ProjectImport/README.md](tests/ProjectImport/README.md) | 1 | Document import validation and the legacy-export and context-only-field compatibility rules. |
| [PORTING.md](PORTING.md) | 3 | Clarify that this fork allows targeted shared correctness fixes while prohibiting UI-specific workarounds in Core. |
| [PORT_STATUS.md](PORT_STATUS.md) | 3 | Distinguish the original port's historical status from this fork's subsequent shared-engine fixes. |
| [README.md](README.md) | 130 | Document the fork's shared fixes, verification command, and checkpoint-based revision workflow. |

Validation: All nine regression groups passed with 951 assertions; the previous 999-to-231 truncation is rejected without mutation, valid imports undo in one step, and injected commit failure restores both data and metadata while retaining redo history.

Compatibility: Legacy exports still require matching game code and ROM size and receive a missing-fingerprint note; pointers, calculated fields, and unsupported field types remain context only, and project import is not a full ROM serializer.

## 013 — Publish consistent, recoverable project exports

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Project/ProjectExport.cs](src/HexManiac.Avalonia/Project/ProjectExport.cs) | 60 | Prepare deterministic table files and an explicit ownership manifest before publishing, reject filename collisions, and import only the declared files under a read lock. |
| [src/HexManiac.Avalonia/Project/ProjectExportFiles.cs](src/HexManiac.Avalonia/Project/ProjectExportFiles.cs) | 10 | Stage complete exports with a recovery journal and exclusive writer lock while preserving unrelated files and restoring the previous set after failed publication. |
| [tests/ProjectExportSafety/ProjectExportSafety.csproj](tests/ProjectExportSafety/ProjectExportSafety.csproj) | 1 | Add synthetic export regressions with shared reference resources. |
| [tests/ProjectExportSafety/Program.cs](tests/ProjectExportSafety/Program.cs) | 1 | Verify stable output, owned-file cleanup, unrelated-file preservation, interrupted-export recovery, path rejection, locking, and publication rollback. |
| [tests/ProjectExportSafety/README.md](tests/ProjectExportSafety/README.md) | 1 | Document ownership, recovery, external-editor limitations, and the persistent coordination lock. |

Validation: All ten regression groups passed with 964 assertions, including 13 export checks using temporary folders and simulated interruption or publication failure.

Limitations: Recovery covers process interruption rather than guaranteed physical power-loss durability; external editors do not honor the app's lock, and project export remains a table project rather than a full ROM serializer.

## 014 — Report Mac crashes to this fork with consistent app versions

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/AppIdentity.cs](src/HexManiac.Avalonia/AppIdentity.cs) | 7 | Define the Mac fork URLs and assembly-based app version with build configuration independently of Core metadata migration versions. |
| [src/HexManiac.Avalonia/Windows/CrashReporter.cs](src/HexManiac.Avalonia/Windows/CrashReporter.cs) | 31 | Save reports to a stable user log folder, report write failures truthfully, and offer Finder only after a successful write. |
| [src/HexManiac.Avalonia/Windows/CrashReporter.cs](src/HexManiac.Avalonia/Windows/CrashReporter.cs) | 71 | Open a bounded issue draft on the Mac fork and retain complete clipboard diagnostics with platform details, inner exceptions, and guarded editor-state collection. |
| [src/HexManiac.Avalonia/Windows/CustomTraceListener.cs](src/HexManiac.Avalonia/Windows/CustomTraceListener.cs) | 32 | Use the shared app identity for assertions and direct their reporting action to this fork instead of upstream Discord. |
| [src/HexManiac.Avalonia/Controls/StartScreen.axaml.cs](src/HexManiac.Avalonia/Controls/StartScreen.axaml.cs) | 7 | Read the welcome version label from the shared Mac app identity. |
| [src/HexManiac.Avalonia/Windows/AboutWindow.axaml.cs](src/HexManiac.Avalonia/Windows/AboutWindow.axaml.cs) | 17 | Show the app version and build configuration in About and identify the Mac fork as the preview-report destination. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml](src/HexManiac.Avalonia/Windows/MainWindow.axaml) | 257 | Point the Help menu's Report an Issue action to the shared Mac fork issues URL. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs](src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs) | 492 | Use the app version in the window title instead of Core's metadata version. |
| [src/HexManiac.Avalonia/Mcp/McpServer.cs](src/HexManiac.Avalonia/Mcp/McpServer.cs) | 197 | Report the same app version during MCP initialization. |
| [tests/CrashReporting/CrashReporting.csproj](tests/CrashReporting/CrashReporting.csproj) | 1 | Add a headless diagnostic regression group referencing the actual Mac application. |
| [tests/CrashReporting/Program.cs](tests/CrashReporting/Program.cs) | 1 | Verify identity, fork routing, inner exceptions, full clipboard content, saved-log actions, append behavior, and failure reporting with synthetic exceptions. |
| [tests/CrashReporting/README.md](tests/CrashReporting/README.md) | 1 | Document the crash checks, new log location, issue-draft workflow, and native integration checks that remain manual. |

Validation: All eleven regression groups passed with 993 assertions, including 29 crash-report checks; the Mac application builds successfully and no real crash, ROM edit, browser launch, or issue submission was required.

Limitations: Native dialog appearance and actual Finder/browser/clipboard interaction still require manual confirmation; existing logs in old working directories are retained, and Core metadata migration versions remain unchanged.

## 015 — Check compatible Mac updates without blocking startup

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Updates/MacUpdateChecker.cs](src/HexManiac.Avalonia/Updates/MacUpdateChecker.cs) | 16 | Query this fork's release API with a timeout and response limit, select newer stable versions with compatible Mac assets, and cache successful results by repository, version, and architecture. |
| [src/HexManiac.Avalonia/App.axaml.cs](src/HexManiac.Avalonia/App.axaml.cs) | 46 | Direct the existing Update action to this fork's releases page. |
| [src/HexManiac.Avalonia/App.axaml.cs](src/HexManiac.Avalonia/App.axaml.cs) | 126 | Start update discovery after the window opens and cancel pending work when the window closes. |
| [src/HexManiac.Avalonia/App.axaml.cs](src/HexManiac.Avalonia/App.axaml.cs) | 308 | Discard legacy upstream update flags and replace synchronous HTML scraping with background discovery and a guarded UI-thread result update. |
| [src/HexManiac.Core/ViewModels/EditorViewModel.cs](src/HexManiac.Core/ViewModels/EditorViewModel.cs) | 383 | Notify bindings when update availability changes so asynchronously discovered updates enable the menu action. |
| [tests/StartupUpdates/StartupUpdates.csproj](tests/StartupUpdates/StartupUpdates.csproj) | 1 | Add headless startup-update regressions with shared reference resources. |
| [tests/StartupUpdates/Program.cs](tests/StartupUpdates/Program.cs) | 1 | Verify compatible release filtering, yielding requests, cache rules, property notifications, size limits, offline behavior, timeout, and cancellation with fake HTTP responses. |
| [tests/StartupUpdates/README.md](tests/StartupUpdates/README.md) | 1 | Document release tag and asset conventions, daily caching, startup behavior, and the native and live-network checks that remain manual. |

Validation: All twelve regression groups passed with 1,035 assertions, including 42 startup-update checks; no live GitHub access, downloads, installs, or ROM edits were required.

Limitations: Discovery considers the most recent 30 releases and requires documented numeric tags and compatible Mac asset names; native startup appearance and live browser/network behavior remain manual checks, and Update opens the releases page rather than installing software.

## 016 — Reload external ROM changes safely across replacement and rapid saves

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/MacPlatform/ExternalFileMonitor.cs](src/HexManiac.Avalonia/MacPlatform/ExternalFileMonitor.cs) | 12 | Combine native change/create/delete/rename/error notifications with metadata reconciliation, debounced stable content snapshots, retry batches, duplicate suppression, and cancellation of stale deliveries. |
| [src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs](src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs) | 50 | Allow injected UI delivery and provide verified reload snapshots without rereading temporarily locked files. |
| [src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs](src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs) | 256 | Read ROMs without requiring write access and reject incomplete stream reads instead of returning zero-padded tails. |
| [src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs](src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs) | 274 | Suspend external deliveries during local saves and acknowledge successful bytes before resuming monitoring in finally. |
| [src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs](src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs) | 354 | Canonicalize and deduplicate listener registrations, dispose removed monitors, and clear all remaining subscriptions on filesystem disposal. |
| [src/HexManiac.Core/ViewModels/ViewPort.cs](src/HexManiac.Core/ViewModels/ViewPort.cs) | 2466 | Recheck saved state when queued UI reload work runs so intervening user edits cannot be overwritten. |
| [src/HexManiac.Avalonia/App.axaml.cs](src/HexManiac.Avalonia/App.axaml.cs) | 128 | Dispose file monitoring when the main window closes. |
| [tests/ExternalFileChanges/ExternalFileChanges.csproj](tests/ExternalFileChanges/ExternalFileChanges.csproj) | 1 | Add real-filesystem watcher regressions using synthetic ROM data and copied reference resources. |
| [tests/ExternalFileChanges/Program.cs](tests/ExternalFileChanges/Program.cs) | 1 | Verify rapid saves, replacements, renames, locks, snapshots, local-save suppression, callback recovery, disposal, and protection of unsaved edits. |
| [tests/ExternalFileChanges/README.md](tests/ExternalFileChanges/README.md) | 1 | Document monitoring behavior, reconciliation, retries, and limitations around external writers and conflict handling. |

Validation: All thirteen regression groups passed with 1,073 assertions, including 38 external-file checks; native rapid writes initially exposed a missed notification that the reconciliation fallback resolves.

Limitations: Unsaved edits remain untouched without automatic merging or a new conflict dialog; stable reads reduce but cannot eliminate partial-write risk, nonempty ROMs up to 64 MiB are supported, and renamed paths or retargeted links may require reopening as detailed in the regression README.

## 017 — Run the original engine tests on Mac and record their baseline

| File Name | Line | Summary |
| --- | --- | --- |
| [src/Directory.Build.props](src/Directory.Build.props) | 2 | Supply a solution-root fallback scoped to upstream test project names so standalone test builds have predictable output paths without moving Mac application outputs. |
| [src/HexManiac.Tests/HexManiac.Tests.csproj](src/HexManiac.Tests/HexManiac.Tests.csproj) | 4 | Target the test host at .NET 8 and use portable project references and resource copying while retaining Core's target and existing package versions. |
| [src/HexManiac.Tests/TestEnvironment.cs](src/HexManiac.Tests/TestEnvironment.cs) | 6 | Initialize relative test paths from the output directory and reject missing or empty required reference resources before fixtures load. |
| [src/HexManiac.Tests/Integration/UpgradeTests.cs](src/HexManiac.Tests/Integration/UpgradeTests.cs) | 37 | Resolve historical metadata from copied fixtures instead of a fixed parent-directory traversal without changing assertions. |
| [scripts/verify-upstream.sh](scripts/verify-upstream.sh) | 1 | Restore isolated dependencies, build fresh tests, require discovery, and retain validated results for normal VSTest or explicitly selected in-process xUnit execution. |
| [scripts/summarize-upstream.py](scripts/summarize-upstream.py) | 1 | Validate TRX or xUnit XML against discovery and case outcomes while rejecting empty, entirely skipped, inconsistent, aborted, or unsuccessful runs. |
| [scripts/run-upstream-command.py](scripts/run-upstream-command.py) | 1 | Enforce discovery/execution deadlines and terminate the command's own process group after timeout or interruption. |
| [tools/UpstreamTestRunner/UpstreamTestRunner.csproj](tools/UpstreamTestRunner/UpstreamTestRunner.csproj) | 1 | Reference the original test project and a runner utility pinned to its xUnit 2.4.1 engine. |
| [tools/UpstreamTestRunner/Program.cs](tools/UpstreamTestRunner/Program.cs) | 1 | Discover and execute unchanged upstream cases through xUnit's engine in one process and retain standard XML without requiring VSTest's socket connection. |
| [tests/UpstreamVerificationRunner/UpstreamVerificationRunner.csproj](tests/UpstreamVerificationRunner/UpstreamVerificationRunner.csproj) | 1 | Add a dependency-free regression group for the upstream verification infrastructure. |
| [tests/UpstreamVerificationRunner/Program.cs](tests/UpstreamVerificationRunner/Program.cs) | 1 | Exercise both result formats, discovery/process failures, skipped-only and inconsistent runs, retained summaries, and watchdog timeout behavior using disposable fake processes. |
| [tests/UpstreamVerificationRunner/README.md](tests/UpstreamVerificationRunner/README.md) | 1 | Document runner failure checks and their separation from upstream case totals. |
| [src/HexManiac.Tests/README.md](src/HexManiac.Tests/README.md) | 1 | Document runner commands, dependency isolation, resource setup, environment restrictions, and the unchanged optional-ROM policy. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 1 | Record two identical failing-case baselines, classify the 33 failures, account for 1,047 missing-ROM skips, and list pending follow-up work. |
| [tests/README.md](tests/README.md) | 11 | Link the independent upstream runner and baseline while distinguishing the still-unported second integration project. |

Validation: Two complete Debug in-process xUnit runs each discovered 2,570 cases with 1,490 passed, 33 failed, 1,047 skipped, and zero engine errors; the runner correctly returns failure, and the failed-case sets are identical.

Validation note: Mac regression verification passed all fourteen groups with 1,102 assertions, including 29 new runner checks, on its repeat run; an existing FocusAnimation check failed during the first run and then passed both isolated and full reruns without assertion changes.

Limitations: VSTest's localhost socket is denied in this session and its full execution is not verified; the separate HexManiac.Integration project remains unported, upstream failures are retained for triage, and optional ROM fixtures were not supplied or copied.

## 018 — Configure complete successful saves in the upstream fixtures

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Tests/Before_Baseclass/ViewPortSaveTests.cs](src/HexManiac.Tests/Before_Baseclass/ViewPortSaveTests.cs) | 111 | Explicitly return metadata-save success in three successful-save scenarios while retaining their close-event, save-count, filename, and notification assertions. |
| [src/HexManiac.Tests/DiffTests.cs](src/HexManiac.Tests/DiffTests.cs) | 309 | Configure metadata-save success for both shared-model Save As cases before expecting the other viewport to close. |
| [src/HexManiac.Tests/Before_Baseclass/ChangeHistoryTests.cs](src/HexManiac.Tests/Before_Baseclass/ChangeHistoryTests.cs) | 473 | Complete the simulated close-save with a successful metadata write before asserting that history is saved. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 97 | Record the six resolved cases, updated upstream counts, unchanged remaining failures, and separate save-failure validation. |

Validation: The full Debug in-process upstream run discovered 2,570 cases with 1,496 passed, 27 failed, 1,047 skipped, and zero engine errors; exactly the six intended cases changed from failing to passing with no new failures, and all existing assertion lines in the modified test files remain unchanged.

Validation note: The existing SaveSafety group rebuilt successfully and passed all 17 assertions, including metadata failure, retry, Save As, and cancellation of close after a requested save fails.

Scope: Only individual successful-save fixtures were configured; production save behavior and the shared stub's default failure behavior remain unchanged, and the upstream suite still correctly returns failure for the remaining 27 cases.

## 019 — Keep trainer validation working when evolution data is unavailable

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Map/Flags.cs](src/HexManiac.Core/ViewModels/Map/Flags.cs) | 272 | Return empty minimum-level hints when the model has no evolution table instead of enumerating null during trainer validation. |
| [src/HexManiac.Core/ViewModels/Map/Flags.cs](src/HexManiac.Core/ViewModels/Map/Flags.cs) | 294 | Return empty predecessor hints for the same missing-table condition so cached trainer and map-template consumers remain safe. |
| [src/HexManiac.Tests/TrainerValidationTests.cs](src/HexManiac.Tests/TrainerValidationTests.cs) | 10 | Add nine cases proving missing-table safety without mutation while retaining valid minimum-level, inherited, non-level, and multiple-slot behavior. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 125 | Record the reproduced application defect, 15 resolved original failures, passing new cases, and remaining upstream counts. |

Validation: Before the fix, two new missing-table cases reproduced the crash while seven positive evolution-data cases passed; afterward all nine passed and exactly the 15 original trainer/table failures disappeared with no new failures or changes to existing test assertions or fixtures.

Validation note: The full Debug in-process upstream run discovered 2,579 cases with 1,520 passed, 12 failed, 1,047 skipped, and zero engine errors; all fourteen Mac regression groups passed with 1,102 assertions.

Scope: This is a shared application fix rather than a fixture adjustment; it omits only hints that cannot be derived without evolution data, retains the existing valid-table algorithms and warnings, and leaves the twelve unrelated upstream failures for separate work.

## 020 — Complete script braces without corrupting dialogue or commands

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/Models/Code/ScriptParser.cs](src/HexManiac.Core/Models/Code/ScriptParser.cs) | 419 | Recognize existing LF, CRLF, and CR separators during brace completion so inserted boundaries preserve dialogue, command tokens, compiled lengths, and caret positions. |
| [src/HexManiac.Tests/ScriptNewlineTests.cs](src/HexManiac.Tests/ScriptNewlineTests.cs) | 11 | Add nineteen cases covering newline variants, mixed input, brace boundaries, caret movement, exact encoded text and pointers, intentional blank lines, and undo/redo round trips. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 154 | Record the shared newline root cause, eight resolved original failures, new regression coverage, and the four remaining upstream failures. |

Validation: All eight original script failures and nineteen new cases passed without modifying existing assertions or fixtures; the full Debug in-process upstream run discovered 2,598 cases with 1,547 passed, four failed, 1,047 skipped, and zero engine errors, with no new failures.

Validation note: All fourteen Mac regression groups passed with 1,102 assertions, including script editor layout and the existing save, patch, import/export, and UI regressions.

Limitations: Native keyboard/caret appearance remains a manual check, script compilation retains its existing separate undo-history entries, and large-script performance and the four unrelated upstream failures remain separate work.

## 021 — Include Core's declared version in standalone builds

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/HexManiac.Core.csproj](src/HexManiac.Core/HexManiac.Core.csproj) | 22 | Include SharedAssemblyInfo through a project-relative path so standalone builds retain the declared Core version used by metadata migration. |
| [src/HexManiac.Tests/MetadataIdentityTests.cs](src/HexManiac.Tests/MetadataIdentityTests.cs) | 7 | Check declared version attributes, metadata export round trips, release identity, and recognition of older metadata. |

Validation: The new checks exposed missing assembly attributes and version 0.0.0 before the fix; afterward all three passed, and the original list and table-group migration failures passed without changing their assertions or the migration algorithms.

Scope: The first version-only upstream run recorded 2,601 cases with 1,552 passed, two failed, and 1,047 skipped; the remaining warning-timing and missing-fixture cases are addressed in subsequent entries.

## 022 — Complete controlled test work deterministically

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/Models/IFileSystem.cs](src/HexManiac.Core/Models/IFileSystem.cs) | 203 | Complete controlled workloads through task-completion sources so synchronous initialization continuations drain reliably and workload failures fault their returned tasks. |
| [src/HexManiac.Tests/ControlledDispatchTests.cs](src/HexManiac.Tests/ControlledDispatchTests.cs) | 7 | Verify successful completion, draining of continuation-created work, and propagation of workload failures without timing sleeps. |

Validation: All three new dispatcher checks failed before the fix and passed afterward, along with the unchanged matched-word warning assertion that previously raced initialization completion.

Scope: ControlledDispatch is the test dispatcher used by that warning fixture; production Mac dispatching and the warning's validation logic are unchanged.

## 023 — Report absent optional ROM fixtures explicitly

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Tests/Integration/BasicLoadTests.cs](src/HexManiac.Tests/Integration/BasicLoadTests.cs) | 13 | Discover sorted ROM paths case-insensitively and emit one explained skip only when no optional fixture is supplied while retaining actual file-load failures. |
| [src/HexManiac.Tests/RomFixtureDiscoveryTests.cs](src/HexManiac.Tests/RomFixtureDiscoveryTests.cs) | 7 | Check missing and empty folders, extension filtering and order, the explicit absence skip, and failure for a missing specified file. |
| [src/HexManiac.Tests/README.md](src/HexManiac.Tests/README.md) | 42 | Document the optional-fixture discovery policy and distinguish absence skips from failures while loading supplied paths. |
| [tests/README.md](tests/README.md) | 12 | Point to the zero-failure first-project baseline while retaining the separate integration-project limitation. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 194 | Record the confirmed build-version and test-dispatcher causes, fixture policy, two green runs, and remaining coverage limits. |

Validation: Two complete Debug in-process upstream runs each discovered 2,609 cases with 1,561 passed, zero failed, 1,048 skipped, and zero engine errors; all eleven new regression cases pass, the three original metadata/warning assertions are unchanged, and the former no-data failure is one explicit absence skip rather than a claimed successful load.

Validation note: All fourteen Mac regression groups passed with 1,102 assertions on the full repeat; the first run hit the already recorded intermittent FocusAnimation clock assertion, which then passed isolated and in the full repeat without test changes.

Limitations: The 1,048 skips are unavailable optional-ROM cases, the separate HexManiac.Integration project remains unported, and full VSTest execution remains unavailable through this session's blocked localhost connection.

## 024 — Detect lost or duplicated metadata in reopening tests

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Tests/Integration/UpgradeTests.cs](src/HexManiac.Tests/Integration/UpgradeTests.cs) | 177 | Compare complete identity multisets before the existing reopening value assertions so missing or duplicated metadata cannot pass silently. |
| [src/HexManiac.Tests/MetadataReopenAssertionTests.cs](src/HexManiac.Tests/MetadataReopenAssertionTests.cs) | 6 | Add seventeen synthetic cases covering missing, extra, duplicated, reordered, and matching entries across the four collections checked by the ROM reopening test. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 232 | Record stronger first-project reopening assertions and their ROM-free regression evidence. |

Validation: Before strengthening the assertions, eight missing/duplicate scenarios were incorrectly accepted and four extra-entry scenarios raised lookup exceptions; afterward all seventeen regression cases passed, including reordered and matching metadata.

Validation note: The Debug in-process first-project run recorded 2,626 cases with 1,578 passed, zero failed, 1,048 optional-ROM skips, and zero engine errors (`artifacts/upstream-tests/run.Co2QcJ`).

Scope: Only test code changed; application metadata algorithms, ROM definitions, and historical-upgrade exemptions are unchanged, and the second test project remains deferred.

## 025 — Load uncached optional fixtures and explain missing paths

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Tests/Integration/AutoSearchTests.cs](src/HexManiac.Tests/Integration/AutoSearchTests.cs) | 927 | Allow isolated synthetic preload lists, load uncached files before copying models, and identify missing fixture paths in skips. |
| [src/HexManiac.Tests/Integration/UpgradeTests.cs](src/HexManiac.Tests/Integration/UpgradeTests.cs) | 34 | Include the required ROM path in both metadata-upgrade and reopening absence skips. |
| [src/HexManiac.Tests/AutoSearchFixtureTests.cs](src/HexManiac.Tests/AutoSearchFixtureTests.cs) | 8 | Add six synthetic checks for explained skips, cold-cache loading, independent copies, and preservation of the cached template and source bytes. |
| [src/HexManiac.Tests/README.md](src/HexManiac.Tests/README.md) | 55 | Document exact fixture filename expectations and distinguish the three disabled expansion methods from missing-ROM skips. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 247 | Record the fixture-loader defect, six regression checks, current counts, and unresolved disabled-feature coverage. |

Validation: The cold-cache copy case reproduced KeyNotFoundException and both AutoSearch absence checks exposed missing path diagnostics before the fix; afterward all six new cases passed, including unchanged isolation behavior.

Validation note: The final Debug in-process first-project run recorded 2,632 cases with 1,584 passed, zero failed, 1,048 optional-ROM skips, and zero engine errors (`artifacts/upstream-tests/run.sugxmj`); git diff whitespace checks passed.

Scope: Only tests and documentation changed; exceptions while loading supplied fixtures are not converted into skips, game definitions and application code are unchanged, and the second project remains deferred.

## 026 — Run the second upstream test project on Mac

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Integration/HexManiac.Integration.csproj](src/HexManiac.Integration/HexManiac.Integration.csproj) | 4 | Target the integration host at .NET 8 with relative references and shared initialization while replacing Windows build commands with transitive portable resources. |
| [src/HexManiac.Integration/HarnessTests.cs](src/HexManiac.Integration/HarnessTests.cs) | 7 | Check four required host resources without commercial ROMs. |
| [tools/UpstreamTestRunner/UpstreamTestRunner.csproj](tools/UpstreamTestRunner/UpstreamTestRunner.csproj) | 10 | Build and copy the second test assembly into the existing in-process host. |
| [tools/UpstreamTestRunner/Program.cs](tools/UpstreamTestRunner/Program.cs) | 5 | Select either the default first assembly or the explicitly requested integration assembly for xUnit discovery and execution. |
| [scripts/verify-upstream.sh](scripts/verify-upstream.sh) | 8 | Add explicit integration-suite selection and bounded single-node restore while retaining result validation and default first-project behavior. |
| [src/HexManiac.Integration/README.md](src/HexManiac.Integration/README.md) | 1 | Document integration commands, resources, fixture requirements, and coverage limits. |
| [tests/INTEGRATION_BASELINE.md](tests/INTEGRATION_BASELINE.md) | 1 | Record the original 41-case baseline, added resource checks, and the exposed stale script reference. |
| [tests/README.md](tests/README.md) | 14 | Point to the newly runnable second project's separate baseline. |
| [src/HexManiac.Tests/README.md](src/HexManiac.Tests/README.md) | 53 | Replace the obsolete unported-project note with the new integration selector and baseline. |

Validation: Portable Debug builds succeeded; the original 41 cases reported one stale-script failure and 40 absent-ROM skips, then four resource checks passed for a 45-case checkpoint with one failure, 40 skips, and zero engine errors (`artifacts/upstream-tests/run.qmzYAL`).

Scope: Original integration assertions and package versions are retained at this checkpoint; no application code or researched ROM definitions changed, and the failing baseline remains visible for the next isolated fix.

## 027 — Point the integration type-swap test at its current script fixture

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Integration/DataTests.cs](src/HexManiac.Integration/DataTests.cs) | 10 | Replace the nonexistent ability_type_swaps script path with the existing Pixilate fixture that declares the expected type-swap table. |
| [src/HexManiac.Integration/HarnessTests.cs](src/HexManiac.Integration/HarnessTests.cs) | 8 | Verify the exact fixture path used by the upgrade test and its required table declaration without a ROM. |
| [tests/INTEGRATION_BASELINE.md](tests/INTEGRATION_BASELINE.md) | 31 | Record the resolved stale reference and distinguish resource validation from still-skipped ROM migration coverage. |

Validation: The original missing-script failure was reproduced before the reference fix; afterward the Debug integration run reported 46 cases with five passed, zero failed, 41 absent-ROM skips, and zero engine errors (`artifacts/upstream-tests/run.EHaCxt`).

Scope: The existing table assertion and checked-in script contents are unchanged; the former failure now reaches its existing missing-ROM guard, so no successful ROM import or migration is claimed.

## 028 — Read the calculated dialogue pointer in integration overflow tests

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Integration/ScriptTests.cs](src/HexManiac.Integration/ScriptTests.cs) | 13 | Share the existing opcode-offset calculation between both dialogue scans and use its result when reading the text pointer. |
| [src/HexManiac.Integration/DialoguePointerTests.cs](src/HexManiac.Integration/DialoguePointerTests.cs) | 7 | Add seven ROM-free cases covering both pointer layouts, null pointers, unsupported commands, and source-byte preservation. |
| [tests/INTEGRATION_BASELINE.md](tests/INTEGRATION_BASELINE.md) | 44 | Record the reproduced preparemsg misread, existing command-definition evidence, and passing synthetic coverage. |

Validation: Both non-null preparemsg cases failed before the offset fix and all seven new cases passed afterward; the Debug integration run recorded 53 cases with twelve passed, zero failed, 41 absent-ROM skips, and zero engine errors (`artifacts/upstream-tests/run.LDHMpz`).

Scope: Only test code changed; opcode definitions, researched ROM addresses, parser behavior, and the original scans' range/buffer exclusions and overflow assertions are unchanged.

## 029 — Preserve default suite selection under macOS Bash

| File Name | Line | Summary |
| --- | --- | --- |
| [scripts/verify-upstream.sh](scripts/verify-upstream.sh) | 34 | Keep the required result-directory argument in the runner array so Bash nounset mode handles default suite selection without an empty-array error. |

Validation: Running the default first-project selector exposed an unbound-array error before test execution; afterward the complete first-project Debug run recorded 2,632 cases with 1,584 passed, zero failed, 1,048 skipped, and zero engine errors (`artifacts/upstream-tests/run.xBVE8W`).

Scope: This corrects the newly introduced shell invocation; test selection defaults, assertions, result validation, application code, and ROM definitions are unchanged.

## 030 — Assert integration model consistency in Debug and Release

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Integration/IntegrationTests.cs](src/HexManiac.Integration/IntegrationTests.cs) | 93 | Replace the conditional Debug diagnostic call with explicit model-type and consistency assertions. |
| [src/HexManiac.Integration/ModelConsistencyAssertions.cs](src/HexManiac.Integration/ModelConsistencyAssertions.cs) | 8 | Mirror the original consistency diagnostic through public inspection APIs and xUnit assertions while preserving its ROM-pointer exemptions. |
| [src/HexManiac.Integration/ConflictAssertionTests.cs](src/HexManiac.Integration/ConflictAssertionTests.cs) | 11 | Add fourteen synthetic cases for valid and corrupted pointer relationships, overlapping runs, dependent lengths, nonmutation, and permitted out-of-range pointers. |
| [tests/INTEGRATION_BASELINE.md](tests/INTEGRATION_BASELINE.md) | 64 | Record the reproduced Release no-op, final Debug/Release counts, first-project cross-check, and unavailable game-workflow coverage. |
| [src/HexManiac.Integration/README.md](src/HexManiac.Integration/README.md) | 26 | Explain synthetic versus ROM-dependent coverage and build-independent test assertions. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 6 | Point the first project's current scope note to the second project's separate baseline without rewriting historical results. |

Validation: Before the fix, Release accepted deliberately stale pointer metadata and overlapping runs; afterward all fourteen synthetic consistency cases passed in Debug and Release, with each integration run recording 67 cases, 26 passed, zero failed, 41 absent-ROM skips, and zero engine errors (`run.9rmBl7` Debug and `run.HQ2wDZ` Release under artifacts/upstream-tests).

Validation note: The default first-project selector retained 2,632 cases with 1,584 passed, zero failed, 1,048 skipped, and zero engine errors (`artifacts/upstream-tests/run.xBVE8W`); syntax and whitespace checks passed, and the diff confirms no Core or Avalonia application changes.

Scope: All 41 original integration cases still require optional ROM fixtures; only host/synthetic coverage is claimed, production diagnostics and researched ROM definitions are unchanged, and the Mac UI regression suite was not rerun for this test-only work.

## 031 — Coalesce script diagnostic painting on Mac

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs](src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs) | 149 | Coalesce error-decoration updates at render priority and resolve the current script when painting without delaying validation or changing diagnostic records. |
| [tests/ScriptEditorLayout/Program.cs](tests/ScriptEditorLayout/Program.cs) | 57 | Verify one rebuild for a 32-diagnostic burst while preserving all underlines, text, model records, clear behavior, and script-switch isolation. |
| [tests/ScriptEditorLayout/README.md](tests/ScriptEditorLayout/README.md) | 17 | Explain the real-renderer diagnostic coalescing checks and their scope. |
| [tests/SCRIPT_PERFORMANCE.md](tests/SCRIPT_PERFORMANCE.md) | 1 | Record the Windows-only symptom report, reproduced redundant Mac painting, measured rebuild reduction, and validation limits. |

Validation: The regression reproduced 33 actual underline-list rebuilds before the fix and one afterward; ScriptEditorLayout passed 102 assertions, and all fourteen Mac regression groups passed with 1,112 assertions (`/tmp/hma-diagnostic-coalescing-mac.log`).

Scope: Only Mac diagnostic presentation changes; the compiler, on-the-fly relocation/repointing, flag and variable allocation, validation rules, ROM models, and researched definitions are untouched, and end-to-end script latency has not been measured.

## 032 — Cache exact syntax-highlight dictionary lookups

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/TextEditorViewModel.cs](src/HexManiac.Core/ViewModels/TextEditorViewModel.cs) | 19 | Replace repeated keyword/constant scans with lazily cached ordinal lookups while retaining precedence, invalidation, indices, and the old bounds rejection. |
| [src/HexManiac.Tests/SyntaxHighlightingTests.cs](src/HexManiac.Tests/SyntaxHighlightingTests.cs) | 8 | Add twenty-four semantic cases covering coloring, dictionary changes, duplicates, source/caret preservation, large mixed-line-ending input, and quoted EOF. |
| [tools/ScriptHighlightBenchmark/ScriptHighlightBenchmark.csproj](tools/ScriptHighlightBenchmark/ScriptHighlightBenchmark.csproj) | 1 | Add a ROM-free Release benchmark host referencing the shared Core project. |
| [tools/ScriptHighlightBenchmark/Program.cs](tools/ScriptHighlightBenchmark/Program.cs) | 8 | Measure highlighting time and allocations at three sizes and record exact hashes of all six layers. |
| [tools/ScriptHighlightBenchmark/README.md](tools/ScriptHighlightBenchmark/README.md) | 1 | Document benchmark inputs, execution, output comparisons, and measurement limits. |
| [tests/SCRIPT_PERFORMANCE.md](tests/SCRIPT_PERFORMANCE.md) | 23 | Record measured before/after costs, identical layer output, the caught bounds regression, allocation tradeoff, and full regression evidence. |

Validation: The 2,000-line synthetic Release median fell from 197.034 ms to 94.978 ms with identical hashes for all six layers at every benchmark size; allocations rose from 21,558,024 to 21,902,200 bytes per large edit, so this checkpoint claims CPU savings only.

Validation note: The first upstream project passed 1,608 cases with 1,048 optional-ROM skips; the second passed 26 with 41 skips, both with zero failures or engine errors, and all fourteen Mac regression groups passed 1,112 assertions.

Scope: Compiler, relocation/repointing, empty-space allocation, unused flag/variable discovery, ROM caches, and game definitions are unchanged; end-to-end latency and real-ROM behavior remain unmeasured here.

## 033 — Remove temporary allocation from highlight whitespace passes

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/TextEditorViewModel.cs](src/HexManiac.Core/ViewModels/TextEditorViewModel.cs) | 304 | Use direct CR/LF/tab comparisons in the two highlighting whitespace loops to avoid per-character params-array allocations without changing their output. |
| [src/HexManiac.Tests/SyntaxHighlightingTests.cs](src/HexManiac.Tests/SyntaxHighlightingTests.cs) | 131 | Guard repeated whitespace passes with a warmed allocation budget and exact source/output preservation checks. |
| [tests/SCRIPT_PERFORMANCE.md](tests/SCRIPT_PERFORMANCE.md) | 56 | Record the reproduced allocation overhead, final exact-output benchmark, full regression results, and explicit compiler/model scope exclusions. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 9 | Update the current first-project counts and link to the highlighting performance evidence while retaining historical checkpoints. |

Validation: The new whitespace regression allocated 23,040,040 bytes before the fix and passed its 4,096-byte budget afterward; all six layer hashes remain identical at every benchmark size, and the combined 2,000-line Release median changed from 197.034 to 89.860 ms with allocations reduced from 21,558,024 to 3,332,424 bytes per edit.

Validation note: Final checks passed 1,609 first-project cases and 26 second-project cases with zero failures/engine errors, while retaining 1,048 and 41 absent-ROM skips respectively; all fourteen Mac groups passed 1,112 assertions.

Scope: Only highlighting character comparisons change; compiler, live script relocation, pointer updates, free-space allocation, unused flags/variables, undo/model behavior, game definitions, and compiler scheduling are untouched, and real-ROM/end-to-end stutter remains unmeasured.

## 034 — Reuse map bitmaps for zoom-only presentation changes

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TileImage.cs](src/HexManiac.Avalonia/Controls/TileImage.cs) | 250 | Route zoom and layout-scaling changes through layout sizing while preserving current bitmaps, pending frames, and the existing pixel-update path for actual changes. |
| [tests/MapRendering/MapRendering.csproj](tests/MapRendering/MapRendering.csproj) | 1 | Add an automatically discovered offscreen map regression group with the required reference resources. |
| [tests/MapRendering/SyntheticMapFixture.cs](tests/MapRendering/SyntheticMapFixture.cs) | 10 | Build fabricated binary map tables and two-color blocksets for the real model, renderer, editing, and history paths without a ROM. |
| [tests/MapRendering/Program.cs](tests/MapRendering/Program.cs) | 1 | Check three sizes for zoom reuse, exact pixels/bytes/metadata, painting, undo/redo, masks, selection overlays, sprites, and pending/context-switched frames. |
| [tests/MapRendering/README.md](tests/MapRendering/README.md) | 1 | Document the regression group and distinguish synthetic history/render checks from native-tab and NPC coverage. |
| [tools/MapRenderingBenchmark/MapRenderingBenchmark.csproj](tools/MapRenderingBenchmark/MapRenderingBenchmark.csproj) | 1 | Add a Release benchmark host sharing the same synthetic map fixture. |
| [tools/MapRenderingBenchmark/Program.cs](tools/MapRenderingBenchmark/Program.cs) | 1 | Record cold/cached rendering and repeated offscreen zoom processing with raw timings, UI allocations, immutable source checks, pixel hashes, and bitmap-publication counts. |
| [tools/MapRenderingBenchmark/README.md](tools/MapRenderingBenchmark/README.md) | 1 | Document benchmark execution, reproducibility, and component-level measurement limits. |
| [tests/MAP_PERFORMANCE.md](tests/MAP_PERFORMANCE.md) | 1 | Record the reproduced redundant zoom refresh, before/after benchmarks, exact-output checks, regression results, and untouched Core scope. |
| [tests/README.md](tests/README.md) | 22 | Link separate map and script benchmark evidence without implying full-editor latency measurements. |

Validation: At each of three map sizes, 70 zoom steps published 70 bitmaps before the fix and zero afterward, with identical rendered-pixel hashes; the largest fixture's ten-step offscreen median changed from 33.917 to 0.019 ms, excluding native layout, GPU work, and input latency.

Validation note: MapRendering passed 93 assertions, all fifteen Mac groups passed 1,205 assertions, and both upstream suites remained green with 1,609 and 26 passes plus their existing 1,048 and 41 optional-ROM skips; zero engine errors were reported.

Scope: No Core code, compiler, repointing, pointer definitions, flag/variable allocation, map-editing algorithms, cache invalidation, or undo implementation changed; real-ROM/NPC workflows and native interaction remain unverified.

## 035 — Benchmark Pokémon table preparation and production UI bindings

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/PokemonTableBenchmark/PokemonTableBenchmark.csproj](tools/PokemonTableBenchmark/PokemonTableBenchmark.csproj) | 1 | Add a standalone Release benchmark host with benchmark-only Avalonia.Headless services and existing model resources. |
| [tools/PokemonTableBenchmark/SyntheticPokemonFixture.cs](tools/PokemonTableBenchmark/SyntheticPokemonFixture.cs) | 7 | Construct fabricated names/base-stats tables using the existing stats schema, with optional unrelated tables and separate untimed model-access counters. |
| [tools/PokemonTableBenchmark/Program.cs](tools/PokemonTableBenchmark/Program.cs) | 17 | Measure real navigation, selector/field preparation, list discovery, production bindings/layout, and separate forced drawing while guarding visible values, bytes, metadata, and rename undo/redo. |
| [tools/PokemonTableBenchmark/README.md](tools/PokemonTableBenchmark/README.md) | 1 | Document reproducible benchmark execution, synthetic coverage, experimental choice reuse, and native-UI measurement limits. |
| [tests/TABLE_PERFORMANCE.md](tests/TABLE_PERFORMANCE.md) | 1 | Record baseline/repeat evidence, repeated table-discovery and lookup-token allocations, existing control reuse, and prioritized optimization requirements. |
| [tests/README.md](tests/README.md) | 23 | Link the table investigation alongside the existing script and map performance reports. |

Validation: The final Release build passed with zero warnings/errors and all four fixtures passed 1,304 correctness checks; repeated runs reproduced control-free navigation allocations exactly, including 28.49 MB versus 208.57 MB per 100 switches when 250 unrelated tables were added to the 412-species fixture.

Measurement note: Isolated section/table-list discovery allocated 174.81 MB per 100 paired reads with those extra tables; source inspection and a 1,360-byte lookup-token allocation probe identify repeated read-only discovery as the first optimization candidate, without claiming any editor speedup yet.

Scope: This checkpoint changes tools and documentation only; application code, game definitions/addresses, compiler, relocation/repointing, unused flag/variable discovery, undo, and existing tests are unchanged, while real-ROM workflows and native Mac input/compositor performance remain unmeasured.

## 036 — Reuse table-navigation discovery within each refresh

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Tools/TableTool.cs](src/HexManiac.Core/ViewModels/Tools/TableTool.cs) | 25 | Share lazily discovered names/sections during one synchronous refresh and use one discovery per external TableList read while preserving live getters, sorting, filtering, and notifications. |
| [src/HexManiac.Tests/TableNavigationTests.cs](src/HexManiac.Tests/TableNavigationTests.cs) | 10 | Add seven regression cases for discovery reuse, live getters, creation/removal, renames, matched-length formats, reload, and forced relocation with real pointer and undo/redo preservation. |
| [tools/PokemonTableBenchmark/Program.cs](tools/PokemonTableBenchmark/Program.cs) | 206 | Clarify the report scope so the unchanged benchmark workloads describe both baseline and optimized editor revisions accurately. |
| [tests/TABLE_PERFORMANCE.md](tests/TABLE_PERFORMANCE.md) | 159 | Record the refresh-scoped implementation, before/after measurements, unchanged outputs, regression results, and known offscreen animation-test instability. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 10 | Update first-project counts to include the seven new table-navigation regression cases. |

Validation: The final uncontended Release benchmark passed all 1,304 checks with identical fixture hashes/control counts; for 412 species plus 250 extra tables, 100-switch preparation changed from 57.336 to 29.889 ms and 208.574 to 80.245 MB allocated, while headless binding/layout changed from 923.608 to 674.841 ms and 636.998 to 327.007 MB allocated.

Validation note: The first upstream project passed 1,616 cases with 1,048 optional-ROM skips and the second passed 26 with 41 skips, both with zero failures/engine errors; all fifteen Mac groups passed 1,205 assertions on the complete repeat after the unchanged intermittent FocusAnimation clock check passed in isolation.

Scope: Only table-navigation preparation changes; no persistent ROM cache, lookup-token behavior, dropdown-choice cache, researched game addresses, compiler, relocation/repointing, pointer definitions, free-space/flag/variable allocation, or undo implementation changes, and native Mac/real-ROM responsiveness remains unmeasured.

## 037 — Investigate trainer presentation, validation, and template selection

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/TrainerTableBenchmark/TrainerTableBenchmark.csproj](tools/TrainerTableBenchmark/TrainerTableBenchmark.csproj) | 1 | Add a standalone Release trainer benchmark with benchmark-only headless services and existing format resources. |
| [tools/TrainerTableBenchmark/SyntheticTrainerFixture.cs](tools/TrainerTableBenchmark/SyntheticTrainerFixture.cs) | 9 | Build six fabricated trainer/team scenarios with the existing 40-byte schema, symbolic lists/partitions, optional uncompressed artwork, and occupied growth guards. |
| [tools/TrainerTableBenchmark/Program.cs](tools/TrainerTableBenchmark/Program.cs) | 20 | Measure navigation and component preparation while checking bindings, immutable data, real team resizing/repointing/history, and an isolated in-memory template-ordering experiment. |
| [tools/TrainerTableBenchmark/README.md](tools/TrainerTableBenchmark/README.md) | 1 | Document benchmark execution, current versus experimental UI behavior, measurement semantics, and real-ROM/default-command coverage limits. |
| [tests/TRAINER_PERFORMANCE.md](tests/TRAINER_PERFORMANCE.md) | 1 | Record trainer baseline measurements, the shadowed specialized template, discarded stream candidates, fuzzy validation, portrait preparation, and prioritized follow-up work. |
| [tests/README.md](tests/README.md) | 25 | Link the trainer investigation alongside the existing table, script, and map performance reports. |

Validation: The final Release build passed with zero warnings/errors and all six fixtures passed 7,831 checks, including exact read-only bytes/metadata, all unrelated team pointers, real growth relocation or shrinkage, and undo/redo restoration; repeated runs retained identical fixture/text/preview hashes.

Measurement note: Six-member explicit-team validation cost 84.054 ms / 106.562 MB per 100 teams, an unused generic stream candidate cost 85.472 ms / 109.148 MB, and preparing 64 portrait choices cost 87.171 ms / 171.853 MB per 100 dropdowns; these are isolated baseline costs, not shipped speed improvements.

Finding note: All production cases selected the generic stream TextBox; moving only the trainer template ahead of its base template in one in-memory probe revealed the specialized editor, four exact previews, and both default command buttons without writing model bytes.

Scope: Investigation tools/docs only; no application XAML, compiler, ROM definitions/addresses, write/repointing logic, cache, or undo implementation changes, while native responsiveness, shared teams, default command behavior, and real move legality remain unverified.

## 038 — Restore specialized trainer, sprite, and palette templates

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TabView.axaml](src/HexManiac.Avalonia/Controls/TabView.axaml) | 553 | Place the generic stream fallback after derived templates so Avalonia displays the intended trainer editor, previews/buttons, sprite controls, and palette controls. |
| [tests/StreamTemplates/StreamTemplates.csproj](tests/StreamTemplates/StreamTemplates.csproj) | 1 | Add an automatically discovered headless regression group sharing the existing synthetic trainer fixture. |
| [tests/StreamTemplates/Program.cs](tests/StreamTemplates/Program.cs) | 1 | Verify specialized controls and text fallback with real bindings, default-button fallback actions, read-only data preservation, and actual UI team growth/repointing/undo/redo. |
| [tests/StreamTemplates/README.md](tests/StreamTemplates/README.md) | 1 | Document production-template coverage and distinguish fixture default-command checks from real-ROM behavior. |
| [tests/TRAINER_PERFORMANCE.md](tests/TRAINER_PERFORMANCE.md) | 202 | Record the separately verified template restoration and establish that the optimization comparison must use the corrected UI. |
| [tests/README.md](tests/README.md) | 17 | Link the production stream-template regression coverage. |

Validation: The new regression reproduced the missing trainer TextEditor before the fix and passed 123 assertions afterward; all sixteen Mac groups passed 1,328 assertions with no skipped groups.

Scope: Template ordering only; template contents, Core preparation, compiler, ROM definitions/addresses, relocation/pointer algorithms, and history implementation are unchanged, while native appearance and real-ROM default-move/item behavior remain unverified.

## 039 — Avoid discarded generic stream preparation

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Tools/TableGroupViewModel.cs](src/HexManiac.Core/ViewModels/Tools/TableGroupViewModel.cs) | 232 | Select the trainer, palette, sprite, or text presentation before constructing it while preserving the original type precedence and pointer-validation/editing paths. |
| [src/HexManiac.Tests/TrainerPresentationTests.cs](src/HexManiac.Tests/TrainerPresentationTests.cs) | 7 | Guard warmed trainer-refresh allocation while preserving the live editor, exact text/layers/diagnostics, ROM bytes, metadata, and empty undo history. |
| [src/HexManiac.Tests/HexManiac.Tests.csproj](src/HexManiac.Tests/HexManiac.Tests.csproj) | 32 | Link the existing trainer fixture into the upstream test assembly without adding package dependencies. |
| [tools/TrainerTableBenchmark/SyntheticTrainerFixture.cs](tools/TrainerTableBenchmark/SyntheticTrainerFixture.cs) | 1 | Add explicit imports so the unchanged fixture can compile in the upstream project with implicit usings disabled. |
| [tools/TrainerTableBenchmark/Program.cs](tools/TrainerTableBenchmark/Program.cs) | 244 | Clarify report scope for implementation follow-ups without changing benchmark workloads. |
| [tests/TRAINER_PERFORMANCE.md](tests/TRAINER_PERFORMANCE.md) | 224 | Record the separate construction optimization, failing-before/passing-after allocation regression, corrected-UI comparison, and final verification. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 10 | Update first-project counts for the additional trainer-presentation regression case. |

Validation: The new regression failed before the change at 2,603,048 bytes per warmed refresh and passed the unchanged 1,800,000-byte budget afterward with exact presentation/data preservation; the first upstream suite passed 1,617 cases and the second passed 26, retaining 1,048 and 41 optional-ROM skips with zero failures/engine errors.

Validation note: All sixteen Mac groups again passed 1,328 assertions, including restored trainer/sprite/palette controls, actual default-button fallback actions, and bound text-box editing with forced team relocation and undo/redo.

Measurement note: Both corrected-UI benchmark revisions passed 7,831 checks with identical fixture/text/pixel hashes, UI summaries, and diagnostics; custom-six preparation changed from 282.514 to 196.459 ms and 379.810 to 270.662 MB allocated per 100 switches, while bound-phase timing varied across scenarios and does not support a uniform native/UI speedup claim.

Scope: Stream presentation construction only; specialized constructors, shared enum/move matching, portrait/palette caches, compiler, researched addresses, relocation/pointer algorithms, unused flag/variable discovery, and history implementation remain unchanged, while native responsiveness and real-ROM default behavior remain unverified.

## 040 — Validate trainer move tokens and align warning spans

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/Models/Runs/TrainerPokemonTeamRun.cs](src/HexManiac.Core/Models/Runs/TrainerPokemonTeamRun.cs) | 646 | Strip the move-list prefix before shared name matching and position unlearnable-move warnings on the actual token after indentation. |
| [src/HexManiac.Tests/TrainerMoveValidationTests.cs](src/HexManiac.Tests/TrainerMoveValidationTests.cs) | 11 | Add twenty-four cases with real synthetic learnsets covering matching variants, warning ranges, species/rename updates, immutable data, and a validation allocation budget. |
| [tests/TRAINER_PERFORMANCE.md](tests/TRAINER_PERFORMANCE.md) | 298 | Record the separate move-validation implementation, failing-before/passing-after evidence, benchmark comparison, and coverage limits for publishing. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 10 | Update first-project counts for the twenty-four trainer move-validation cases. |

Validation: The initial twenty-case pre-fix run reproduced two indented-warning span failures and 1,087,728 bytes per warmed validation against an 800,000-byte budget; the final twenty-four-case set passes with the unchanged budget, and both upstream suites pass with 1,641 and 26 cases plus their existing 1,048 and 41 optional-ROM skips and zero failures/engine errors.

Validation note: The complete Mac repeat passed sixteen groups and 1,328 assertions after the unchanged intermittent offscreen FocusAnimation clock check passed in isolation; no animation assertion was weakened or suppressed.

Measurement note: Both benchmark revisions passed 7,831 checks with matching fixture/text/preview hashes, UI summaries, and diagnostics; six-member custom-team validation changed from 83.076 to 14.971 ms and 106.562 to 27.391 MB allocated per 100 teams, while navigation preparation changed from 196.459 to 51.954 ms and 270.662 to 112.319 MB per 100 switches, without claiming those component ratios as whole-editor speedups.

Scope: Advisory trainer move validation only; shared enum matching, learnability-source algorithms, compiler/deserialization, ROM definitions/addresses, pointer/repointing and history logic, caches, emulator integration, and AI/custom features remain unchanged, while real-ROM legality and native responsiveness remain unverified.

## 041 — Benchmark portrait preparation and audit image freshness

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/TrainerPortraitBenchmark/TrainerPortraitBenchmark.csproj](tools/TrainerPortraitBenchmark/TrainerPortraitBenchmark.csproj) | 1 | Add a standalone Release portrait benchmark with test-only headless services and existing resources. |
| [tools/TrainerPortraitBenchmark/PortraitFixture.cs](tools/TrainerPortraitBenchmark/PortraitFixture.cs) | 10 | Isolate the real portrait enum path with fabricated palettes and 16/64/128 uncompressed or 64 compressed 64x64 images. |
| [tools/TrainerPortraitBenchmark/Program.cs](tools/TrainerPortraitBenchmark/Program.cs) | 20 | Measure candidate/navigation/popup preparation and existing image-cache reads while probing edit, palette, repoint, history, reload, and invalid-enum freshness. |
| [tools/TrainerPortraitBenchmark/README.md](tools/TrainerPortraitBenchmark/README.md) | 1 | Document reproducible execution, experimental-cache limits, recorded staleness, and native/import coverage exclusions. |
| [tests/PORTRAIT_PERFORMANCE.md](tests/PORTRAIT_PERFORMANCE.md) | 1 | Record portrait scaling, the existing label-only reuse defect, invalidation review, and a conservative correctness-first recommendation. |
| [tests/README.md](tests/README.md) | 29 | Link the portrait-specific performance and freshness evidence. |

Validation: The final Release build passed with zero warnings/errors and the repeated investigation passed 66 checks with identical source/pixel hashes and freshness findings; 64 uncompressed portrait candidates cost 88.263 ms / 171.856 MB per 100 constructions and 128 cost 176.679 ms / 343.125 MB, with first-case warmup and native-UI limits recorded separately.

Finding note: Sprite edits, palette edits, repoint-then-edit, and model reload produced correct fresh/model-cached pixels but retained stale live choice images when labels stayed unchanged; edit/repoint probes preserved exact bytes/pointers through undo/redo and invalid portrait 255 retained its numeric fallback.

Scope: Tools/docs only, with no application cache or behavior changes; the report recommends fixing existing visual-choice freshness before any bounded model-cache reuse experiment, and leaves item profiling, real-ROM/native behavior, image-import orchestration, and broader palette/format equivalence for follow-up work.

## 042 — Refresh edited portrait choices and retain Mac selection

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Tools/FilteringComboOptions.cs](src/HexManiac.Core/ViewModels/Tools/FilteringComboOptions.cs) | 13 | Compare visual content and enum indices before retaining choice collections so unchanged labels cannot hide edited portraits. |
| [src/HexManiac.Avalonia/Controls/AngleComboBox.cs](src/HexManiac.Avalonia/Controls/AngleComboBox.cs) | 181 | Restore the VM selection after replacing choice items without detaching the existing selection binding. |
| [src/HexManiac.Tests/VisualComboOptionsTests.cs](src/HexManiac.Tests/VisualComboOptionsTests.cs) | 6 | Add ten regression cases for visual changes, index changes, equal-image reuse, and text filtering. |
| [tests/PortraitFreshness/Program.cs](tests/PortraitFreshness/Program.cs) | 34 | Exercise actual bound bitmaps, popup state, selection, sprite/palette edits, shared-pointer repointing, reload, and exact-byte undo/redo for both sprite encodings. |
| [tests/PortraitFreshness/PortraitFreshness.csproj](tests/PortraitFreshness/PortraitFreshness.csproj) | 1 | Add a separately executable Mac regression group sharing the existing synthetic portrait fixture. |
| [tests/PortraitFreshness/README.md](tests/PortraitFreshness/README.md) | 1 | Document reproduced failures and the new control-level coverage and limits. |
| [tools/TrainerPortraitBenchmark/PortraitFixture.cs](tools/TrainerPortraitBenchmark/PortraitFixture.cs) | 1 | Add explicit imports so the unchanged fixture also supports projects without implicit usings. |
| [tests/PORTRAIT_PERFORMANCE.md](tests/PORTRAIT_PERFORMANCE.md) | 140 | Record the correctness checkpoint before attempting image reuse. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 10 | Update first-project counts for the ten visual-choice cases. |
| [tests/README.md](tests/README.md) | 29 | Link the new live portrait regression coverage. |

Validation: Both upstream suites pass with 1,651/26 cases, 1,048/41 optional-ROM skips, and zero failures/engine errors; the complete Mac repeat passes 17 groups and 1,396 assertions, including 68 new portrait assertions.

Validation note: The new test first reproduced stale live pixels, then a blank Mac selector after item replacement; both pass after the fixes, while an initial unrelated FocusAnimation timing failure passed unchanged in isolation and in the full repeat.

Scope: Choice freshness and Mac selection synchronization only; portrait rendering/cache behavior, ROM definitions and researched addresses, compiler, pointer/repointing algorithms, and history implementation remain unchanged, with native appearance and real-ROM/import orchestration still unverified.

## 043 — Reuse portrait enum rendering within the model cache lifetime

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/Models/Runs/ModelCacheScope.cs](src/HexManiac.Core/Models/Runs/ModelCacheScope.cs) | 106 | Retain original enum-rendered images by sprite/palette reference and enum index for known immutable-format runs while rendering other implementations fresh. |
| [src/HexManiac.Core/Models/Runs/ArrayRunElementSegment.cs](src/HexManiac.Core/Models/Runs/ArrayRunElementSegment.cs) | 246 | Reuse model-scoped portrait pixels after the existing palette lookup while preserving enum labels, dimensions, indices, and renderer policy. |
| [src/HexManiac.Tests/PortraitPreparationTests.cs](src/HexManiac.Tests/PortraitPreparationTests.cs) | 12 | Add sixteen regression cases for allocation, exact renderer equivalence, invalidation, metadata contexts, palette variants, aliases, model isolation, and custom sprite fallback. |
| [src/HexManiac.Tests/HexManiac.Tests.csproj](src/HexManiac.Tests/HexManiac.Tests.csproj) | 33 | Link the existing synthetic portrait fixture into the upstream test assembly. |
| [tools/TrainerPortraitBenchmark/Program.cs](tools/TrainerPortraitBenchmark/Program.cs) | 97 | Clarify freshness/report descriptions for implementation follow-ups without changing timed workloads. |
| [tools/TrainerPortraitBenchmark/README.md](tools/TrainerPortraitBenchmark/README.md) | 28 | Distinguish the untouched address-only-cache experiment from the implemented context-aware enum-image reuse. |
| [tests/PORTRAIT_PERFORMANCE.md](tests/PORTRAIT_PERFORMANCE.md) | 158 | Record the separate reuse change, pre-fix allocation failures, controlled measurements, repeat results, regression evidence, and retained-memory tradeoff. |
| [tests/UPSTREAM_BASELINE.md](tests/UPSTREAM_BASELINE.md) | 10 | Update first-project counts and coverage for the sixteen portrait-preparation cases. |

Validation: Both upstream suites pass 1,667/26 cases with 1,048/41 optional-ROM skips and zero failures/engine errors, and all 17 Mac groups pass 1,396 assertions including live portrait edits/repoint/reload/history checks; the unchanged 400,000-byte candidate budget failed before the change at 1,726,968/1,859,576 bytes and passes afterward.

Validation note: One intermediate run exceeded the existing whitespace-allocation test budget at 8,152 bytes; the full final run passes that unchanged test, with no assertion weakened or suppressed, and the Release build retains eight existing Core warnings with zero errors.

Measurement note: Before and both after benchmark runs pass 66 checks with identical fixture/pixel hashes and freshness results; 64 uncompressed candidate preparation changed from 77.406 to 3.014 ms and 171.856 to 13.136 MB per 100 constructions, with the repeat at 2.959 ms and identical allocation, while bound headless navigation changed from 435.055 to 324.568 ms and 383.108 to 220.846 MB without claiming a native or whole-editor multiplier.

Scope: A small context-keyed dictionary within the existing model-cache lifetime preserves the original enum renderer and existing invalidation, with no persistent/global cache or changes to the address-only image cache, researched addresses, compiler, pointer/repointing algorithms, or history implementation; retained RAM reduction, native responsiveness, real ROMs, and import orchestration remain unverified, and items are left for the next profiling task.


Validation follow-up for 043 — supplied ROM, 2026-11-02: The provided file identifies as
FireRed 1.1 (BPRE1), so its host copy uses the existing v1.1 fixture name; all 113 newly
enabled first-project cases pass, bringing that run to 1,780 passes/935 skips with zero
failures/engine errors, while integration remains 26 passes/41 skips because its ROM
workflows require FireRed 1.0 or Emerald. Both original and staged ROM hashes are unchanged;
no application/test assertions changed and no ROM was committed. Full evidence and limits
are recorded in tests/UPSTREAM_BASELINE.md rather than a separate change entry.

## 044 — Benchmark item preparation and bound presentation

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/ItemTableBenchmark/ItemTableBenchmark.csproj](tools/ItemTableBenchmark/ItemTableBenchmark.csproj) | 1 | Add a standalone Release item benchmark using the existing app and test-only headless services. |
| [tools/ItemTableBenchmark/ItemFixture.cs](tools/ItemTableBenchmark/ItemFixture.cs) | 15 | Provide fabricated grouped item fixtures and an optional read-only ROM loader with deferred timers controlled through both dispatcher paths. |
| [tools/ItemTableBenchmark/Program.cs](tools/ItemTableBenchmark/Program.cs) | 20 | Measure preparation, dropdowns, descriptions, icon/palette candidates, bound navigation, and selected-icon refresh while capturing integrity and presentation hashes. |
| [tools/ItemTableBenchmark/README.md](tools/ItemTableBenchmark/README.md) | 1 | Document workload sizes, optional ROM use, timer exclusions, notification diagnostics, and interpretation limits. |
| [tests/ITEM_PERFORMANCE.md](tests/ITEM_PERFORMANCE.md) | 1 | Record corrected baselines, discarded exploratory measurements, a modest notification-reduction candidate, and coverage limits. |
| [tests/README.md](tests/README.md) | 36 | Link item-specific performance evidence. |

Validation: The completed baseline passes 762 checks across four synthetic cases and the supplied BPRE1 ROM, preserving bytes/metadata/history and bound descriptions/icon dimensions; the Release build passes with zero errors, and no application code changed at this checkpoint.

Measurement note: Correctly deferred real-ROM foreground navigation costs 22.955 ms / 25.883 MB per ten selections and bound navigation/layout 213.020 ms / 107.215 MB; preliminary multi-GB runs forced normally delayed map searches because of an immediate shared dispatcher, are explicitly excluded from the baseline, and do not establish a native browsing regression.

Scope: Measurement tools/docs only; existing formats and researched ROM addresses, conditional item fields, decoding, palette lookup, pointer/repointing algorithms, and history remain unchanged, with delayed map-use latency and native/other-game workflows left for separate review.

## 045 — Avoid redundant item icon geometry notifications

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Tools/SpriteElementViewModel.cs](src/HexManiac.Core/ViewModels/Tools/SpriteElementViewModel.cs) | 276 | Notify width/height/scale only when published values change while retaining fresh pixel rendering and notifications. |
| [tests/ItemTablePresentation/Program.cs](tests/ItemTablePresentation/Program.cs) | 21 | Add 48 offscreen checks for actual icon bitmaps, dimension/scale transitions, bound item edits, forced description relocation, palette/sprite freshness, and exact-byte undo/redo. |
| [tests/ItemTablePresentation/ItemTablePresentation.csproj](tests/ItemTablePresentation/ItemTablePresentation.csproj) | 1 | Add the item presentation regression group using the existing app and shared fabricated item fixture. |
| [tests/ItemTablePresentation/README.md](tests/ItemTablePresentation/README.md) | 1 | Document the pre-fix failure, editing/history coverage, synthetic geometry-copy check, and deferred/native exclusions. |
| [tests/ITEM_PERFORMANCE.md](tests/ITEM_PERFORMANCE.md) | 75 | Record the separate notification change, unchanged pixels/format evidence, modest component savings, and mixed whole-table timings. |
| [tests/README.md](tests/README.md) | 36 | Link the new item editing and presentation regression coverage. |

Validation: The new regression first fails on unchanged dimensions requesting redundant uploads, then passes all 48 checks; both upstream suites pass 1,780/26 cases with 935/41 optional-ROM skips and zero failures/engine errors, and all 18 Mac groups pass 1,444 assertions without weakened thresholds or suppressed cases.

Measurement note: Before/after benchmark runs each pass 762 checks with identical fixture/description/pixel/bound-bitmap hashes, formats, and field counts; fixed-size diagnostics drop width/height/scale notifications from 103 synthetic or 13 ROM to zero while retaining every PixelData update, and synthetic selected-icon refresh changes from 1.257 to 0.833 ms and 0.848800 to 0.763200 MB per 100, a small component saving rather than a general browsing-speedup claim.

Repeat note: The second after run also passes 762 checks with identical integrity/presentation fields and stable allocation reductions, but synthetic icon-refresh times vary substantially (3.835/0.839/3.822 ms per 100 across the three icon cases), so the first paired speedup is not consistently reproduced and no stable elapsed-time gain is claimed.

Scope: Presentation notifications only, with no new image cache or changes to decoding, palette resolution, conditional item semantics, researched addresses, import/export, compiler, pointer/repointing algorithms, or history; mixed whole-table timing, native responsiveness, retained RAM, other games, and delayed map-use search remain outside the claimed improvement.

## 046 — Investigate delayed item map-usage searches

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/ItemUsageBenchmark/ItemUsageBenchmark.csproj](tools/ItemUsageBenchmark/ItemUsageBenchmark.csproj) | 1 | Add a standalone Release investigation using the actual Mac delay timer and offscreen UI synchronization context. |
| [tools/ItemUsageBenchmark/Program.cs](tools/ItemUsageBenchmark/Program.cs) | 20 | Measure real delayed callbacks and component costs while checking ordered event/pixel results and prototyping query-local repeated-root reuse. |
| [tools/ItemUsageBenchmark/README.md](tools/ItemUsageBenchmark/README.md) | 1 | Document real timer scheduling, controlled dispatcher limits, explicit ROM use, and benchmark-only experiment semantics. |
| [tests/ITEM_USAGE_PERFORMANCE.md](tests/ITEM_USAGE_PERFORMANCE.md) | 1 | Record UI-thread scan costs, closed-menu execution, correct coalescing, repeatable prototype savings, and implementation prerequisites. |
| [tests/ITEM_PERFORMANCE.md](tests/ITEM_PERFORMANCE.md) | 147 | Link the separately measured delayed-search follow-up. |
| [tests/README.md](tests/README.md) | 39 | Link usage-search performance and correctness evidence. |

Validation: Both final-code runs pass 71 checks with identical ROM identity, ordered results, thumbnail hashes, event/root counts, and hidden-item counts, preserving source/model bytes, metadata, and empty history; the final Release build passes with zero warnings/errors.

Measurement note: Repeated actual delayed callbacks cost roughly 186–449 ms and 240–623 MB of temporary allocation in the final repeat while the usage menu is closed; ten rapid selections correctly coalesce to one scan, and the tool-only repeated-root experiment preserves ordered matches while reducing matching from 142–146 to 82–85 ms and roughly 240 to 126 MB per query.

Scope: Tools/docs only; no application parser, researched addresses, script traversal limits, pointer/repointing/history algorithms, or cache behavior changed, and the approximately 40% matching-time/48% allocation experiment is not a shipped whole-editor improvement; the report requires cancellation, mutation, reload, relocation, and history regression coverage before implementation.

## 047 — Reuse shared script outcomes within each map-usage query

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Tools/ButtonArrayElementViewModel.cs](src/HexManiac.Core/ViewModels/Tools/ButtonArrayElementViewModel.cs) | 140 | Reuse script-match booleans and one opcode-filter buffer within a query while retaining per-event destinations/previews and cancellation checks. |
| [tests/MapUsageSearch/Program.cs](tests/MapUsageSearch/Program.cs) | 14 | Add 106 synthetic checks for ordered shared-root contexts, hidden/negative matches, branches/cycles/invalid roots, preview validity, edits/pointers/reload/history, cancellation, and warmed allocation. |
| [tests/MapUsageSearch/MapUsageSearch.csproj](tests/MapUsageSearch/MapUsageSearch.csproj) | 1 | Add a separately executable Mac regression group sharing the existing fabricated map fixture. |
| [tests/MapUsageSearch/README.md](tests/MapUsageSearch/README.md) | 1 | Document public-query coverage and the unchanged allocation budget's failing-before/passing-after evidence. |
| [tests/ITEM_USAGE_PERFORMANCE.md](tests/ITEM_USAGE_PERFORMANCE.md) | 156 | Record the separate application change, integrity checks, regression results, actual callback improvements, and remaining thumbnail/UI-thread cost. |
| [tests/README.md](tests/README.md) | 42 | Link the query-local freshness, cancellation, and allocation regression group. |
| [tools/ItemUsageBenchmark/README.md](tools/ItemUsageBenchmark/README.md) | 29 | Distinguish the original tool-side experiment from the implemented and separately measured application callback. |

Validation: Both upstream suites pass 1,780/26 cases with 935/41 optional-ROM skips and zero failures/engine errors, and the complete Mac repeat passes 19 groups/1,550 assertions; the new 2,000,000-byte allocation guard fails with the original implementation at 16,811,032 bytes and passes unchanged at about 1,178,552 bytes with reuse.

Validation note: An initial Mac run hit the previously observed FocusAnimation clock check; it passed unchanged in isolation and in the complete repeat, with no assertion weakened or case suppressed.

Measurement note: Baseline and both implementation benchmark runs pass 71 checks with identical ordered destinations, thumbnail pixels, ROM identity, and event/root counts; item-13 actual delayed callback execution changes from 224.856 to 160.445 ms (147.708 on repeat) and 302.558 to 187.903 MB per query, with approximately 115 MB less temporary allocation in all four queries and less benefit when eager thumbnail creation dominates.

Scope: A dictionary confined to one query preserves the original parser/traversal and current per-event behavior, without persistent caching or changes to researched addresses, compiler, pointer/repointing/history algorithms, flags/variables, debounce timing, thread assignment, or thumbnail scheduling; the measured 29–36% sparse-query execution saving is not a whole-editor/native responsiveness or retained-memory claim.

## 048 — Add full-editor wheel zoom and startup regression coverage

| File Name | Line | Summary |
| --- | --- | --- |
| [tests/MapZoom/MapZoom.csproj](tests/MapZoom/MapZoom.csproj) | 1 | Add a headless regression executable using the production editor window and shared fabricated map fixture. |
| [tests/MapZoom/Program.cs](tests/MapZoom/Program.cs) | 20 | Exercise fresh-tab wheel routing, border transitions, UI heartbeats, final rendering, and data integrity with synthetic data and optional local FireRed background initialization. |
| [tests/MapZoom/README.md](tests/MapZoom/README.md) | 1 | Document watchdog-bounded reproduction commands, passing scenarios, native diagnostic limits, and how to capture a recurring freeze. |
| [tests/README.md](tests/README.md) | 45 | Link the full-editor zoom checks and unresolved freeze investigation. |

Validation: The new project builds with zero warnings/errors; three synthetic sessions pass 1,098 checks and three supplied FireRed 1.1 sessions pass 1,102 checks, each scenario sending 360 routed wheel events and preserving model bytes, metadata, history availability, and the source ROM.

Scope: Tests and documentation only; no editor implementation or ROM addresses were changed, and the full pre-existing suites were not rerun for this test-only addition. The user's intermittent startup freeze remains unreproduced and unresolved; investigation stopped at the user's request after zoom worked normally on restart.

## 049 — Restore secondary-click and macOS Control-click on map arrows

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 622 | Restrict arrow resizing to primary clicks and explicitly open the connection menu for macOS Control-click while allowing Avalonia to handle ordinary secondary clicks. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 650 | Release arrow capture and complete resizing only for a primary drag that actually started so context-menu releases remain available. |
| [tests/MapZoom/Program.cs](tests/MapZoom/Program.cs) | 93 | Verify routed secondary and Control clicks, menu bindings, the real Create New Map bank prompt with cancellation, primary drag capture, and unchanged map data. |
| [tests/MapZoom/README.md](tests/MapZoom/README.md) | 30 | Document the arrow regression, original-code failure, passing synthetic and FireRed checks, and map-creation coverage limits. |

Validation: The menu-opening assertion fails against the original arrow handlers and passes with the fix; the final build has zero warnings/errors, the supplied FireRed scenario passes 1,162 checks, and the full Mac repeat passes 20 groups/2,708 assertions, including 1,158 checks in the expanded synthetic group.

Validation note: The first full Mac run hit the previously observed FocusAnimation timing failure; it passed unchanged in isolation and in the complete repeat, without weakening or suppressing any checks. The source ROM's SHA-256 remained unchanged.

Scope: Arrow input routing only; core map creation, connection editing, resizing, repointing, researched addresses, and undo algorithms are unchanged, and this is separate from the unresolved zoom freeze. Native macOS click behavior still needs a live check after relaunch.

## 050 — Match map tutorial modifier instructions to macOS input

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs](src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs) | 32 | Translate Control wording and Ctrl icons to Command at display time on macOS without modifying shared tutorial records. |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml](src/HexManiac.Avalonia/Controls/TutorialControl.axaml) | 42 | Apply platform-aware key icons and descriptions to tutorial cards so rectangle-fill and hide-events instructions match the existing input bindings. |
| [src/HexManiac.Avalonia/Resources/Icons.axaml](src/HexManiac.Avalonia/Resources/Icons.axaml) | 251 | Add a Cmd key graphic for Mac tutorial cards while preserving the Ctrl graphic for other platforms. |
| [tests/TutorialLayout/Program.cs](tests/TutorialLayout/Program.cs) | 74 | Check platform-adjusted descriptions, full card fit, modifier icons, and unchanged Core tutorial text. |

Validation: The editor and tutorial regression project build with zero warnings/errors, and all 332 tutorial layout, icon, content, and preview checks pass.

Scope: Display instructions only; Command-left-drag already invokes rectangle fill on macOS, and no gesture, block-writing, selection, pointer, or history logic changed. Rectangle fill paints with the selected block rather than selecting an arbitrary area.

## 051 — Clarify the single-block requirement for flood fill

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs](src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs) | 35 | Explain in the Paint Blocks card that double-click fill requires one selected block and does not support multiple-block selections. |
| [tests/TutorialLayout/Program.cs](tests/TutorialLayout/Program.cs) | 23 | Verify the clarified description is displayed and fits the tutorial card at both tested widths. |

Validation: The editor and tutorial project build with zero warnings/errors, and all 332 tutorial checks pass; an exploratory full-window synthetic test also fills a connected nine-cell region through two routed left clicks, including slight pointer movement.

Scope: Tutorial wording only; the user confirmed multiple blocks were selected, which the existing shared Core intentionally excludes from flood fill, so no painting algorithm or input handler was changed and the provisional input trace was removed.

## 052 — Route dropped HMA scripts to the ROM and restore file-drop acceptance

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/CoreSeam.cs](src/HexManiac.Avalonia/CoreSeam.cs) | 56 | Route HMA drops from map tabs to their owning ROM through the existing importer while rejecting missing, script-file, or busy targets and separating earlier undo transactions. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs](src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs) | 90 | Receive local-file drag/drop events even when child controls handle them so the window can process dropped scripts. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs](src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs) | 172 | Advertise copy for local file drags, recover from child drag rejection, and report copy on drop without moving source files. |
| [tests/FileDrop/Program.cs](tests/FileDrop/Program.cs) | 19 | Add 24 routed-drop checks for real HMA edits, map-to-ROM targeting, repeated imports, undo/redo, earlier edits, metadata, other-ROM isolation, rejected targets, and ordinary file/text behavior. |
| [tests/FileDrop/FileDrop.csproj](tests/FileDrop/FileDrop.csproj) | 1 | Add a separately executable headless file-drop group sharing the fabricated map fixture. |
| [tests/FileDrop/README.md](tests/FileDrop/README.md) | 1 | Document the original failure, copy and targeting behavior, test storage-wrapper setup, and native/script-compatibility limits. |
| [tests/README.md](tests/README.md) | 48 | Link the file-drop and HMA import regression group. |

Validation: The final build passes with zero warnings/errors, all 24 file-drop checks pass, and the full Mac repeat passes 21 groups/2,756 assertions with no skipped groups; the original handlers fail the same regression by opening an HMA as binary data without a ROM target.

Validation note: The initial full run hit the previously observed FocusAnimation timing failure, which passed unchanged in isolation and in the complete repeat without weakening or suppressing checks.

Scope: Avalonia drop routing and undo-transaction boundaries only; the shared HMA parser, game/version guards, relative-asset path context, researched addresses, repointing rules, and Core history implementation are unchanged. Synthetic data only was used, no user ROM or source script was modified, and native Finder operation and the user's unprovided script still need a live retry after relaunch.

User verification update: The user retried and reports that `.hma` dropping still does not work; this remains a partial fix rather than a resolved workflow and is deferred as HMA-001 in [FutureDevelopment.md](FutureDevelopment.md).

## 053 — Support macOS Control-left-drag selection of placed map blocks

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 220 | Route macOS Control-left-click and drag through the existing secondary-button map selection path while preserving Command rectangle fill and native secondary dragging. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 337 | Match release to the physical left button for Control selection so releasing Control early still completes the brush and event creation resets the gesture state. |
| [src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs](src/HexManiac.Avalonia/Controls/TutorialControl.axaml.cs) | 37 | Explain Control-click picking and Control-left-drag selection with subsequent stamping in the Mac tutorial cards. |
| [tests/MapSelection/Program.cs](tests/MapSelection/Program.cs) | 25 | Add 117 synthetic checks for exact block/collision copying, repeated stamps, selection without edits, early modifier release, two scales, metadata, undo/redo, and preserved Command rectangle fill. |
| [tests/MapSelection/MapSelection.csproj](tests/MapSelection/MapSelection.csproj) | 1 | Add a headless regression group using the production window and map controls with the fabricated map fixture. |
| [tests/MapSelection/README.md](tests/MapSelection/README.md) | 1 | Document gesture behavior, original-handler failure, render-timer setup, coverage, and native-device verification limits. |
| [tests/TutorialLayout/Program.cs](tests/TutorialLayout/Program.cs) | 23 | Verify the platform-specific selection instructions display in full within the tutorial cards. |
| [tests/README.md](tests/README.md) | 51 | Link the map selection and stamping regression group. |

Validation: The unchanged selection assertion fails on the original map handler and passes with the new mapping; the final build has zero warnings/errors, the selection group passes 117 checks, and the complete Mac suite passes 22 groups/2,873 assertions with no skipped groups.

Scope: Mac input mapping and tutorial wording only; selection reuses shared Core's existing brush capture, block/collision copying, stamping, clipping, and history behavior without changing researched addresses or editing/repointing algorithms. Native macOS device interaction still needs a live check after relaunch.

## 054 — Fix text-tool selection crash while editing map names

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TabView.axaml.cs](src/HexManiac.Avalonia/Controls/TabView.axaml.cs) | 188 | Apply bounded Core selection ranges to the Avalonia text box with a feedback guard that is restored even if an update fails. |
| [src/HexManiac.Avalonia/Controls/TabView.axaml.cs](src/HexManiac.Avalonia/Controls/TabView.axaml.cs) | 251 | Normalize directional text-box endpoints into a nonnegative Core start/length, bound them during text changes, and restore selection subscriptions in a finally block. |
| [tests/TextSelection/Program.cs](tests/TextSelection/Program.cs) | 17 | Add 41 checks for real PCS/name-table text, reversed and forward ranges, caret collapse, shorter replacements, neighboring names, selection without edits, and exact undo/redo. |
| [tests/TextSelection/TextSelection.csproj](tests/TextSelection/TextSelection.csproj) | 1 | Add a headless regression group using production TabView text selection and keyboard input. |
| [tests/TextSelection/README.md](tests/TextSelection/README.md) | 1 | Document the logged negative-length failure, original-code reproduction, fixture history baseline, and native-map retest limits. |
| [tests/README.md](tests/README.md) | 54 | Link text-tool selection and name-editing crash coverage. |
| [FutureDevelopment.md](FutureDevelopment.md) | 29 | Record the proposed translucent copied-brush preview as a separate follow-up after native verification of this crash fix. |

Validation: The original bridge reproduces ArgumentOutOfRangeException in PCSTool.UpdateSelectionFromTool with a negative substring length, matching the user's crash path; the corrected project builds with zero warnings/errors, all 41 text-selection checks pass, and the full Mac suite passes 23 groups/2,914 assertions with no skipped groups.

Scope: Avalonia text-selection synchronization only; shared Core's encoding, name-table updates, relocation, pointers, and undo algorithms are unchanged, and no user ROM was edited. The captured report was from the user's new map session, while automated reproduction uses synthetic text rather than map creation; native renaming still needs a retry after relaunch. The cursor stamp preview is planned, not implemented.

## 055 — Show a translucent map-brush preview at the stamp destination

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml](src/HexManiac.Avalonia/Controls/MapTab.axaml) | 1124 | Display the existing copied brush at 55% opacity within the map viewport, sharing map alignment and zoom while remaining behind outlines, arrows, and tutorials and passing input through. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 101 | Show ready single/multi-block brushes and hide previews during map gestures, cancellation, reveal-beneath mode, or absent selection without changing Core editing behavior. |
| [tests/MapSelection/Program.cs](tests/MapSelection/Program.cs) | 58 | Expand coverage to 211 checks including real copied bitmap pixels, translucency, destination alignment, fractional zoom, window resizing, bitmap reuse, clipping/layering, visibility, and unchanged stamping/history. |
| [tests/MapSelection/README.md](tests/MapSelection/README.md) | 33 | Document the overlay, expanded coverage, bitmap reuse, and preserved auto-tiling behavior. |
| [FutureDevelopment.md](FutureDevelopment.md) | 31 | Mark the copied-brush preview implemented with native visual confirmation pending. |

Validation: The final project builds with zero warnings/errors, all 211 map selection/preview checks pass, and the complete Mac suite passes 23 groups/3,008 assertions with no skipped groups; source bitmap identity/upload counts remain unchanged during pointer movement, zoom, and window resizing.

Scope: Avalonia display only; the existing brush image and hover footprint drive the preview while shared Core's copying, block/collision writes, clipping, pointer/repointing, and history algorithms remain unchanged. Synthetic data only was used, and native appearance on the user's tree selection still needs a visual check after relaunch.

User verification update: The user confirmed the translucent copied-block preview works well in the native Mac editor.

## 056 — Protect mixed-elevation map stamps through saving and undo/redo

| File Name | Line | Summary |
| --- | --- | --- |
| [tests/MapSelection/Program.cs](tests/MapSelection/Program.cs) | 34 | Exercise all copy/stamp gestures with elevation-4 ground, Cross Layers stairs, elevation-3 ground, and a wall using repeated graphics with distinct permissions. |
| [tests/MapSelection/Program.cs](tests/MapSelection/Program.cs) | 165 | Verify production Save and independent file/model reload preserve exact block IDs, collision bits, elevations, unrelated bytes, metadata, and undo/redo across saves using disposable synthetic files. |
| [tests/MapSelection/README.md](tests/MapSelection/README.md) | 50 | Document mixed-elevation persistence coverage, temporary-file isolation, and the distinction between editor data checks and in-game movement validation. |

Validation: The map selection project builds with zero warnings/errors and passes 340 checks on macOS; the complete Mac suite passes all 23 groups and 3,137 assertions with no skipped groups.

Scope: Regression coverage only; no application editing, researched addresses, block behaviors, collision/elevation rules, or repointing algorithms were changed, and no user ROM was opened or modified.

## 057 — Benchmark script editing and identify sparse variable-alias lookup costs

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/ScriptEditingBenchmark/ScriptEditingBenchmark.csproj](tools/ScriptEditingBenchmark/ScriptEditingBenchmark.csproj) | 1 | Add an independently runnable Release benchmark referencing production Core and Avalonia controls with cached headless dependencies and shipped script definitions. |
| [tools/ScriptEditingBenchmark/Program.cs](tools/ScriptEditingBenchmark/Program.cs) | 19 | Measure immediate edits, deferred compilation, and attached text-control presentation across synthetic command/comment/dialogue scripts while checking exact byte changes, deferred writes, stable addresses, and matching outputs. |
| [tools/ScriptEditingBenchmark/Program.cs](tools/ScriptEditingBenchmark/Program.cs) | 82 | Compare equivalent hexadecimal, decimal, and named-alias operands and isolate compiler/traversal/formatting costs with timing samples, managed allocations, timer owners, and output hashes. |
| [tools/ScriptEditingBenchmark/README.md](tools/ScriptEditingBenchmark/README.md) | 1 | Document reproduction commands, fixture isolation, measurement phases, report completeness, and native-input/relocation limits. |
| [tests/SCRIPT_EDIT_PIPELINE.md](tests/SCRIPT_EDIT_PIPELINE.md) | 1 | Record measured compiler and UI baselines, the sparse alias-list bottleneck, and required semantic/safety coverage before optimization. |
| [tests/SCRIPT_PERFORMANCE.md](tests/SCRIPT_PERFORMANCE.md) | 3 | Link the pipeline investigation from the earlier highlighting checkpoints to distinguish existing optimizations from newly measured costs. |
| [tests/README.md](tests/README.md) | 57 | Link the new pipeline measurements and benchmark instructions alongside the existing regression documentation. |

Validation: The Release benchmark builds successfully; all 18 pipeline cases, two component cases, and three equivalent-spelling cases finish with 1,596 checks, identical paired UI/Core layer hashes, and identical equivalent-spelling compiled-byte hashes. At 500 repeated commands, isolated compilation measures 702.73 ms/1,068,447,352 allocated bytes for hexadecimal variables versus 7.68 ms/24,153,352 bytes for equivalent decimal variables; these are diagnostic baselines, not an implemented speedup or retained-RAM measurements.

Scope: Investigation tooling and documentation only; application sources, script matching semantics, researched addresses, compiler/relocation/repointing, flag/variable allocation, and history are unchanged. No user ROM was opened or modified, and the user's prior Windows stuttering is not claimed reproduced or resolved.

## 058 — Protect script argument semantics and relocation before lookup optimization

| File Name | Line | Summary |
| --- | --- | --- |
| [tests/ScriptArgumentSafety/ScriptArgumentSafety.csproj](tests/ScriptArgumentSafety/ScriptArgumentSafety.csproj) | 1 | Add an automatically discovered portable regression group using production Core and shipped script definitions without a ROM or native UI. |
| [tests/ScriptArgumentSafety/Program.cs](tests/ScriptArgumentSafety/Program.cs) | 25 | Freeze exact/partial/quoted/duplicate and numeric-looking alias precedence, sparse names, widths, numeric prefixes, offsets, rejected arguments, and model/cache-boundary isolation. |
| [tests/ScriptArgumentSafety/Program.cs](tests/ScriptArgumentSafety/Program.cs) | 99 | Exercise hexadecimal, decimal, and named-variable script growth, forced relocation, shrinking, two callers, dialogue and neighbor preservation, exact history round-trips, and recovery from invalid input. |
| [tests/ScriptArgumentSafety/README.md](tests/ScriptArgumentSafety/README.md) | 1 | Document safety contracts, reproduction commands, existing relocation history/cache behavior, the non-undoable free-space cursor, and coverage limits. |
| [tests/README.md](tests/README.md) | 61 | Link the script argument and mutation safety group from the regression index. |
| [tests/SCRIPT_EDIT_PIPELINE.md](tests/SCRIPT_EDIT_PIPELINE.md) | 90 | Link the completed safety baseline while keeping argument lookup optimization explicitly unimplemented. |

Validation: The new project builds with zero warnings/errors in the final incremental build and passes 641 checks against unchanged application code; the complete Mac suite passes all 24 groups and 3,778 assertions with no skipped groups.

Scope: Tests and documentation only; argument matching, compiler/relocation/repointing, researched addresses, flags/variables, and history are unchanged, and no supplied ROM was opened or modified. Relocation's existing multiple history records are fully round-tripped; all bytes and serialized metadata must restore except the existing FreeSpaceSearch cursor, which is maintained outside undo, and option-list freshness is checked after the normal explicit cache invalidation boundary.

## 059 — Reuse script argument matches within each compilation without changing numeric interpretation

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/Models/Code/LabelLibrary.cs](src/HexManiac.Core/Models/Code/LabelLibrary.cs) | 12 | Add opt-in compilation-local reuse of the unchanged enum matcher's success/value, invalidated by model scope or read-only culture changes and bypassed for foreign models or mutable cultures. |
| [src/HexManiac.Core/Models/Code/ScriptArg.cs](src/HexManiac.Core/Models/Code/ScriptArg.cs) | 140 | Use the compilation context for repeated enum lookups while retaining numeric fallback order, alias precedence, offsets, emitted widths, and the separate pointer path. |
| [src/HexManiac.Core/Models/Code/ScriptParser.cs](src/HexManiac.Core/Models/Code/ScriptParser.cs) | 713 | Enable lookup reuse only in the fresh local-label context constructed for each compilation. |
| [tests/ScriptArgumentSafety/Program.cs](tests/ScriptArgumentSafety/Program.cs) | 102 | Expand safety coverage to 1,565 checks with cached/uncached differential cases, three cultures, explicit decimal/pointer distinctions, invalidation, and helper/production-compiler allocation budgets. |
| [tests/ScriptArgumentSafety/README.md](tests/ScriptArgumentSafety/README.md) | 61 | Document the original reference path, differential coverage, allocation budgets, and separately tracked existing sparse-match exception. |
| [tools/ScriptEditingBenchmark/Program.cs](tools/ScriptEditingBenchmark/Program.cs) | 31 | Read checkpoint identity from HMA_BENCHMARK_REVISION rather than falsely labeling new measurements as the original baseline. |
| [tools/ScriptEditingBenchmark/README.md](tools/ScriptEditingBenchmark/README.md) | 10 | Preserve historical baseline reports and document current-output names and revision labeling. |
| [tests/SCRIPT_ARGUMENT_LOOKUP_PERFORMANCE.md](tests/SCRIPT_ARGUMENT_LOOKUP_PERFORMANCE.md) | 1 | Record parsing invariants, complete before/after compiler and editor measurements, hash equivalence, test evidence, and repeated-operand/native-input limits. |
| [tests/SCRIPT_EDIT_PIPELINE.md](tests/SCRIPT_EDIT_PIPELINE.md) | 3 | Link the implemented follow-up while retaining the original investigation and safety checkpoints as historical evidence. |
| [tests/README.md](tests/README.md) | 65 | Link the argument-reuse performance and correctness report. |
| [FutureDevelopment.md](FutureDevelopment.md) | 47 | Record HMA-003 for an existing sparse-alias partial-match exception reproduced by both uncached and cached matching. |

Validation: All 24 Mac groups pass 4,702 assertions with no groups skipped; the first upstream project passes 1,780 cases with 0 failures/935 optional skips and the second passes 26 with 0 failures/41 optional skips, both with zero engine errors. All 1,596 benchmark invariants and original/current layer/compiled-byte hashes match; the isolated 500-command hexadecimal compile falls from 702.73 ms/1,068,447,352 temporary allocated bytes to 20.58 ms/26,623,472 bytes, while the attached 2,000-command deferred update falls from 2,795.41 ms to 90.19 ms.

Scope: No numeric syntax or alias-priority change, pointer parsing change, global/cross-edit result cache, relocation/history rewrite, researched-address change, or flag/variable allocation change; existing mutable-culture and model-refresh behavior are retained. Figures are synthetic Release medians and temporary managed allocation volumes, not retained RAM or native typing latency; UI/highlighting costs remain, HMA-003 is unchanged, and source/staged FireRed fixture hashes are unchanged after upstream verification.

## 060 — Benchmark 100–500-line NPC-style scripts and characterize autocomplete mapping

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/ScriptEditingBenchmark/Program.cs](tools/ScriptEditingBenchmark/Program.cs) | 22 | Add an NPC mode with ten warmups, exact source-line sizes, numeric/dialogue/comment edits, settled-address checks, and explicit fixture-size/hash metadata. |
| [tools/ScriptEditingBenchmark/Program.cs](tools/ScriptEditingBenchmark/Program.cs) | 44 | Generate compound NPC conversations with conditional shared-routine calls, varied variables/flags, comments, and fixed-address dialogue instead of only repeated commands. |
| [tools/ScriptEditingBenchmark/Program.cs](tools/ScriptEditingBenchmark/Program.cs) | 198 | Measure core command/variable completions separately and characterize routed production overlay results with first-line controls and explicit later-line mismatches. |
| [tools/ScriptEditingBenchmark/README.md](tools/ScriptEditingBenchmark/README.md) | 50 | Document realistic-size reproduction, measurement/setup boundaries, autocomplete controls, and native-input limits. |
| [tests/NPC_SCRIPT_BENCHMARK.md](tests/NPC_SCRIPT_BENCHMARK.md) | 1 | Record two complete runs, per-edit timing ranges, hash equivalence, fast core completions, and the confirmed headless Mac caret-mapping finding. |
| [tests/README.md](tests/README.md) | 68 | Link the NPC-style editing and autocomplete benchmark report. |
| [FutureDevelopment.md](FutureDevelopment.md) | 65 | Track HMA-004 for the original overlay's two-character newline assumption and the required offered/applied-completion regressions. |

Validation: The final Release benchmark builds with zero warnings/errors; two complete runs each pass 2,892 measurement/invariant checks across 18 edit cases and 24 completion probes, with identical paired UI/Core and cross-run source/layer hashes, and the legacy lookup mode still passes 58 checks. All six first-line overlay controls match Core in each run, while all 18 tested later-line probes disagree and remain explicitly unfixed; immediate 500-line edits measure about 10–13 ms, deferred updates 34–38 ms, and core autocomplete query medians 0.0135–0.0784 ms.

Scope: Benchmark tooling and documentation only; no application sources, numeric/alias parsing, script/compiler/relocation/history algorithms, or supplied ROMs were changed or opened. Measurements exclude native keyboard/full TabView bindings, initial source consolidation, the debounce wait, script growth, and new auto-stream allocation; later-line UI mismatches are characterization findings rather than successful autocomplete checks, and no native UI fix is claimed.

## 061 — Fix autocomplete caret mapping and exact line replacement on Mac

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs](src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs) | 90 | Map bounded caret positions to logical line spans using actual separators instead of assuming two-character host line endings. |
| [src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs](src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs) | 135 | Query the correct line/column, anchor the popup to the active caret line, and clear stale options at document start or when no suggestions apply. |
| [src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs](src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs) | 182 | Use the same bounded mapping for quote/space acceptance without negative substring offsets. |
| [src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs](src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs) | 231 | Splice only the active source line when applying a suggestion, preserving surrounding text, original separators, suffixes, and the correct caret. |
| [tests/AutocompleteMapping/AutocompleteMapping.csproj](tests/AutocompleteMapping/AutocompleteMapping.csproj) | 1 | Add an automatically discovered headless regression group using production controls and shipped script definitions. |
| [tests/AutocompleteMapping/Program.cs](tests/AutocompleteMapping/Program.cs) | 49 | Add 282 checks for LF/CRLF/mixed offered/applied completions, keyboard/mouse input, selection and boundary carets, quotes, live compiled bytes, caller pointers, and undo/redo. |
| [tests/AutocompleteMapping/README.md](tests/AutocompleteMapping/README.md) | 1 | Document the original-handler failure, focused coverage, controlled compilation timer, and native visual limits. |
| [tests/NPC_SCRIPT_BENCHMARK.md](tests/NPC_SCRIPT_BENCHMARK.md) | 3 | Record the fixed benchmark with all 24 UI/Core probes agreeing while preserving the original mismatch evidence and unchanged hashes/options. |
| [tools/ScriptEditingBenchmark/README.md](tools/ScriptEditingBenchmark/README.md) | 82 | Link the fixed benchmark checkpoint and retain the older mismatch reports. |
| [tests/README.md](tests/README.md) | 71 | Link the autocomplete mapping regression group. |
| [FutureDevelopment.md](FutureDevelopment.md) | 67 | Mark HMA-004 fixed with automated query/acceptance/history coverage while leaving native screen appearance for normal-use verification. |

Validation: The original handler fails the unchanged exact-suggestion regression; the corrected group builds with zero warnings/errors and passes 282 checks. The realistic NPC benchmark passes all 2,892 invariants, all 24 overlay probes match Core, and all 18 original source/layer hash sets and Core option arrays are unchanged; the complete Mac suite passes all 25 groups and 4,984 assertions with no skipped groups.

Scope: Avalonia autocomplete UI mapping/replacement only; shared-Core matching, decimal/hexadecimal and pointer interpretation, compilation, relocation, researched addresses, flag/variable allocation, and history algorithms are unchanged. Synthetic models only were used, no supplied ROM was opened or modified, and native screen appearance and every scroll/font configuration are not claimed validated.

## 062 — Fix sparse script aliases and preserve explicit hexadecimal values

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/Models/Runs/ArrayRunElementSegment.cs](src/HexManiac.Core/Models/Runs/ArrayRunElementSegment.cs) | 294 | Exclude implicit decimal names of unnamed slots from unquoted explicit-hex matches while preserving real alias precedence and quoted name queries. |
| [src/HexManiac.Core/Models/Runs/ArrayRunElementSegment.cs](src/HexManiac.Core/Models/Runs/ArrayRunElementSegment.cs) | 329 | Rank partial matches using null-safe numeric display names to prevent sparse-list exceptions. |
| [tests/ScriptArgumentSafety/Program.cs](tests/ScriptArgumentSafety/Program.cs) | 85 | Add crash and silent numeric-misinterpretation regressions, real/quoted alias controls, offsets, and exact end-to-end compiled bytes. |
| [tests/ScriptArgumentSafety/README.md](tests/ScriptArgumentSafety/README.md) | 81 | Explain HMA-003, the narrow matching correction, and the expanded safety coverage. |
| [FutureDevelopment.md](FutureDevelopment.md) | 47 | Mark HMA-003 fixed with reproduction details and verified scope. |

Validation: The original implementation fails the new regression; the corrected focused group passes 1,596 checks, and all 25 Mac groups pass 5,015 assertions with no skipped groups. The first upstream project passes 1,780 cases with zero failures and 935 optional skips; the second passes 26 with zero failures and 41 optional skips, both with zero engine errors. Source and staged FireRed fixture SHA-256 hashes are unchanged after verification.

Scope: The only application change is the shared enum matcher; real named alias precedence, decimal parsing, quoted queries, pointer parsing, compilation-local cache boundaries, relocation/history and researched addresses retain their existing behavior. Flag/variable scanning and allocation are unchanged.

## 063 — Document existing unused flag/variable helpers and characterize their limits

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/FlagVariableReview/FlagVariableReview.csproj](tools/FlagVariableReview/FlagVariableReview.csproj) | 1 | Add a standalone synthetic review tool referencing production scanners and template helpers without opening a supplied ROM. |
| [tools/FlagVariableReview/Program.cs](tools/FlagVariableReview/Program.cs) | 12 | Characterize source operands, stale usage snapshots, header conditions, exhausted ranges and session reservations with connected-root setup checks. |
| [tools/FlagVariableReview/README.md](tools/FlagVariableReview/README.md) | 1 | Document reproducible characterization commands and distinguish current findings from required regression behavior. |
| [tests/FLAG_VARIABLE_REVIEW.md](tests/FLAG_VARIABLE_REVIEW.md) | 1 | Explain existing insertion shortcuts, scan coverage, reservations, reproduced limitations and recommended future hardening with primary engine references. |
| [tests/README.md](tests/README.md) | 75 | Link the existing-helper review from the test documentation index. |

Validation: The review tool builds successfully with zero warnings/errors and completes all six synthetic characterization scenarios; connected destination-variable and detached header-root checks confirm the production scan paths are reached. The report is written to artifacts/script-safety/flag-variable-review.json.

Scope: Review tooling and documentation only; Flags.cs, EventTemplate.cs, insertion controls, allocator behavior, researched addresses and user ROMs are unchanged. These findings describe shared-Core behavior in this fork and do not establish conflicts in any actual user ROM or claim a comparison with the latest upstream revision.

## 064 — Investigate flag/variable usage detection before implementation

| File Name | Line | Summary |
| --- | --- | --- |
| [tests/FlagUsageSafety/FlagUsageSafety.csproj](tests/FlagUsageSafety/FlagUsageSafety.csproj) | 1 | Add an automatically discovered headless safety group linked to fabricated usage fixtures and the unchanged production scanners. |
| [tests/FlagUsageSafety/Program.cs](tests/FlagUsageSafety/Program.cs) | 12 | Protect supported detection, literal distinctions, native reserve controls, event roots, recursive cycles and read-only scans with 535 checks across five game identities. |
| [tests/FlagUsageSafety/README.md](tests/FlagUsageSafety/README.md) | 1 | Document regression commands, safety contracts and the distinction between supported behavior and diagnostic omissions. |
| [tools/FlagVariableReview/UsageDetectionFixture.cs](tools/FlagVariableReview/UsageDetectionFixture.cs) | 1 | Add fabricated event/header layouts and twenty command vectors with independently specified expected operand roles. |
| [tools/FlagVariableReview/UsageDetectionAudit.cs](tools/FlagVariableReview/UsageDetectionAudit.cs) | 14 | Report bounded engine-role coverage and real BPRE1 references while verifying model and source-ROM byte immutability. |
| [tools/FlagVariableReview/Program.cs](tools/FlagVariableReview/Program.cs) | 14 | Route optional usage-review arguments without changing the original helper-characterization mode. |
| [tools/FlagVariableReview/primary-source-manifest.json](tools/FlagVariableReview/primary-source-manifest.json) | 1 | Record primary engine reference URLs, content hashes and twelve verified byte-to-handler mappings for reproducible review. |
| [tools/FlagVariableReview/README.md](tools/FlagVariableReview/README.md) | 26 | Explain synthetic and read-only real-ROM review commands, scope and shared-decoder limits. |
| [tests/FLAG_USAGE_INVESTIGATION.md](tests/FLAG_USAGE_INVESTIGATION.md) | 1 | Record engine operand semantics, current reserve inventories, synthetic gaps, real-ROM corroboration and approval gates for future work. |
| [tests/FLAG_VARIABLE_REVIEW.md](tests/FLAG_VARIABLE_REVIEW.md) | 3 | Link the follow-up usage-detection evidence while retaining the original allocator characterization. |
| [tests/README.md](tests/README.md) | 79 | Link the new investigation and supported-behavior safety group from the test index. |
| [FutureDevelopment.md](FutureDevelopment.md) | 91 | Track HMA-005 as investigation complete with implementation explicitly awaiting approval. |

Validation: The new group passes 535 checks; the complete Mac runner passes all 26 groups / 5,550 assertions with no skipped groups. The review tool builds successfully and completes 100 synthetic command cases plus conditional-header cases across five identities. Twelve engine byte-to-handler mappings and operand roles were checked against FireRed/Emerald/Ruby references. The supplied BPRE1 scan examines 4,762 selected references across 3,979 decoded segments from 2,143 roots: no additional ordinary persistent ID is missing within that bounded coverage, and one special-variable ID (0x8013) is missed. No traversal-limit/unknown-command stops occur in that review; full native/command coverage is not claimed. Model bytes and both source/staged ROM hashes remain unchanged.

Scope: Tests, review tooling and documentation only; no production application sources, scanner/allocator/reservation behavior, researched addresses, script compilation, engine data or user ROM files are changed. Existing omissions are diagnostic findings rather than passing regression expectations; future scanner repairs and allocation changes require separate approval. Native UI/gameplay, modified-game conflicts, reserve-list completeness and unavailable real-game fixtures are outside the validation.

## 065 — Investigate realistic script editing, recovery and persistence workflows

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/ScriptWorkflowReview/ScriptWorkflowReview.csproj](tools/ScriptWorkflowReview/ScriptWorkflowReview.csproj) | 1 | Add a standalone production-CodeTool investigation executable with real headless Avalonia control probes. |
| [tools/ScriptWorkflowReview/WorkflowFixture.cs](tools/ScriptWorkflowReview/WorkflowFixture.cs) | 13 | Create fabricated scripts, callers, blocked neighbors, deterministic debounce and temporary atomic ROM/metadata saves shared with safety tests. |
| [tools/ScriptWorkflowReview/Program.cs](tools/ScriptWorkflowReview/Program.cs) | 1 | Characterize 23 error/recovery, pending-context/history/persistence and resource-write scenarios without changing application behavior. |
| [tools/ScriptWorkflowReview/README.md](tools/ScriptWorkflowReview/README.md) | 1 | Document review commands, evidence interpretation, temporary-file cleanup and native/full-shell limits. |
| [tests/ScriptWorkflowSafety/ScriptWorkflowSafety.csproj](tests/ScriptWorkflowSafety/ScriptWorkflowSafety.csproj) | 1 | Add an automatically discovered portable workflow safety group using the production compiler and save/history paths. |
| [tests/ScriptWorkflowSafety/Program.cs](tests/ScriptWorkflowSafety/Program.cs) | 1 | Protect supported LF/CRLF error correction, coalescing, script/dialogue relocation, exact model history, save/reopen and settled close with 548 checks. |
| [tests/ScriptWorkflowSafety/README.md](tests/ScriptWorkflowSafety/README.md) | 1 | Document safety contracts and explicitly disclose root reselection after history traversal while retaining the presentation finding separately. |
| [tests/SCRIPT_WORKFLOW_INVESTIGATION.md](tests/SCRIPT_WORKFLOW_INVESTIGATION.md) | 1 | Record confirmed wrong-target/undo callbacks, pending save/close gaps, partial resource writes and diagnostic loss with controls, provenance and proposed boundaries. |
| [tests/README.md](tests/README.md) | 83 | Link the supported workflow safety group and separate diagnostic investigation. |
| [FutureDevelopment.md](FutureDevelopment.md) | 112 | Track HMA-006 through HMA-008 as confirmed findings awaiting separate reviewed implementation. |

Validation: The new LF/CRLF group passes 548 checks; all 27 Mac groups pass 6,098 assertions with no groups skipped. Two complete 23-probe diagnostic runs produce identical observations with no unexpected exceptions, including actual TextEditor binding/focus-loss corroboration; final tool build has zero warnings/errors, and a non-JSON output guard rejects a fabricated .gba target without modifying it. Supported relocation, caller pointers, neighboring bytes, complete binary/serialized-metadata history (excluding the existing FreeSpaceSearch preference), temporary-file save/reopen and saved-tag round-trips pass.

Findings: Pending source can write another script after navigation, reapply after Undo and clear Redo, or miss a save/close boundary; existing-stream writes can precede a later compile failure and relocated-source replacement can clear its diagnostics. These remain unfixed and are tracked as HMA-006/007/008. History restores model bytes/metadata but does not automatically navigate the editor back to the root anchor; safety checks explicitly reselect it, and the presentation observation remains in the review rather than being counted as a UI pass.

Scope: Tests, review tooling and documentation only; no production application sources, compiler/repointing/history algorithms, researched addresses, flags/variables, engine behavior or supplied ROMs are changed. All model/file data is fabricated and temporary; diagnostic findings are not passing requirements that defects persist. Manual callback ordering is not native/full-shell timing, game execution or performance benchmarking, and no speedup or fixed defect is claimed.

## 066 — Keep pending script edits bound to their context and history

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Tools/CodeTool.cs](src/HexManiac.Core/ViewModels/Tools/CodeTool.cs) | 28 | Flush pending compilation before context reuse, invalidate stale callbacks by generation, and retain rejected drafts without reusing their bodies for another address. |
| [src/HexManiac.Core/ViewModels/ViewPort.cs](src/HexManiac.Core/ViewModels/ViewPort.cs) | 1152 | Settle source before Undo/Redo and enable Undo for pending or active rejected drafts without clearing a valid Redo branch later. |
| [tests/ScriptWorkflowSafety/Program.cs](tests/ScriptWorkflowSafety/Program.cs) | 94 | Add wrong-target, pending Undo/Redo, retained-draft, stale-callback and mode-switch regressions against production editor paths. |
| [tests/AutocompleteMapping/Program.cs](tests/AutocompleteMapping/Program.cs) | 134 | Check unchanged underlying ROM history while allowing the new public Undo behavior for pending source. |
| [tools/ScriptWorkflowReview/WorkflowFixture.cs](tools/ScriptWorkflowReview/WorkflowFixture.cs) | 108 | Expose captured scheduled callbacks so regressions can simulate callbacks already posted before timer cancellation. |
| [tests/ScriptWorkflowSafety/README.md](tests/ScriptWorkflowSafety/README.md) | 1 | Document the 563-check baseline and pending-context/history contracts. |
| [tests/SCRIPT_WORKFLOW_INVESTIGATION.md](tests/SCRIPT_WORKFLOW_INVESTIGATION.md) | 7 | Link the implemented HMA-006 follow-up without replacing the original failure evidence. |
| [FutureDevelopment.md](FutureDevelopment.md) | 112 | Mark HMA-006 fixed while keeping Save/Close and failed-resource compilation separately tracked. |

Validation: The new navigation regression fails against unchanged production code, then all 563 focused checks pass; the complete Mac suite passes all 27 groups / 6,113 assertions with no skipped groups. Existing numeric interpretation, caller pointers, script/dialogue relocation, neighboring data and full model history tests remain passing.

Scope: Editor scheduling, draft retention and command boundaries only; compiler, researched addresses, flag/variable allocation and engine behavior are unchanged. Synthetic data only; persistence barriers and failed-compilation resource rollback remain subsequent fixes.

## 067 — Include pending script source in persistence and reload decisions

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/ViewPort.cs](src/HexManiac.Core/ViewModels/ViewPort.cs) | 654 | Settle source before Save/Save As/Close and retain rejected drafts with an explanation instead of writing an older snapshot or silently closing. |
| [src/HexManiac.Core/ViewModels/ViewPort.cs](src/HexManiac.Core/ViewModels/ViewPort.cs) | 70 | Include uncompiled source in unsaved indicators, Save eligibility and both external-reload guards. |
| [src/HexManiac.Core/ViewModels/Tools/CodeTool.cs](src/HexManiac.Core/ViewModels/Tools/CodeTool.cs) | 35 | Identify rejected script addresses in persistence error messages. |
| [tests/ScriptWorkflowSafety/Program.cs](tests/ScriptWorkflowSafety/Program.cs) | 144 | Test pending-source Save/Save As, clean post-save callbacks, invalid-draft retention/discard, Close cancellation/discard and external-reload protection. |
| [tests/ScriptWorkflowSafety/README.md](tests/ScriptWorkflowSafety/README.md) | 1 | Record the 579-check persistence and context baseline. |
| [tests/SCRIPT_WORKFLOW_INVESTIGATION.md](tests/SCRIPT_WORKFLOW_INVESTIGATION.md) | 7 | Link the HMA-007 implementation while retaining original evidence. |
| [FutureDevelopment.md](FutureDevelopment.md) | 129 | Mark HMA-007 fixed and retain native full-shell and separate failed-resource-compilation boundaries. |

Validation: The new pending-source Save/unsaved-marker assertion fails before the fix; all 579 workflow checks pass after it. The complete Mac suite passes all 27 groups / 6,129 assertions with no skipped groups, including existing save failure, close cancellation, external file handling, numeric/relocation/history and UI regressions.

Scope: Editor persistence/dirty-state/reload boundaries only; no engine or flag/variable behavior changes, researched-address changes or supplied ROM edits. Invalid drafts require correction or explicit Undo before closing; valid drafts use the existing save/discard/cancel prompt. Compiler resource rollback remains HMA-008.

## 068 — Roll back rejected script/resource compilation and retain diagnostics

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/Models/Code/ScriptParser.cs](src/HexManiac.Core/Models/Code/ScriptParser.cs) | 519 | Compile resource edits into an isolated delta, merge it only on success and restore source, allocation state and model state on failure. |
| [src/HexManiac.Core/Models/ModelDelta.cs](src/HexManiac.Core/Models/ModelDelta.cs) | 43 | Merge already-applied deltas while preserving earliest before-images for bytes, lengths and metadata. |
| [src/HexManiac.Core/Models/IDataModel.cs](src/HexManiac.Core/Models/IDataModel.cs) | 200 | Capture byte-change highlighting for restoration after a rejected attempt. |
| [src/HexManiac.Core/ViewModels/Tools/CodeTool.cs](src/HexManiac.Core/ViewModels/Tools/CodeTool.cs) | 490 | Extend the transaction through placement, relocation, recompilation and formatting, then publish one history record and move notifications after success. |
| [tests/ScriptWorkflowSafety/Program.cs](tests/ScriptWorkflowSafety/Program.cs) | 8 | Add late-error, raw-parser, allocation/expansion, placement, exception, diagnostics, correction and history regressions. |
| [tests/ScriptWorkflowSafety/README.md](tests/ScriptWorkflowSafety/README.md) | 1 | Document transaction coverage and the assertion-count effect of fewer history records. |
| [tests/SCRIPT_WORKFLOW_INVESTIGATION.md](tests/SCRIPT_WORKFLOW_INVESTIGATION.md) | 7 | Preserve historical findings and identify the implemented HMA-008 follow-up. |
| [FutureDevelopment.md](FutureDevelopment.md) | 146 | Mark HMA-008 implemented with its tested scope and native limitations. |

Evidence: The new late-resource atomicity assertion fails against untouched commit 3aa58ab7 in a temporary snapshot and passes after the fix. The workflow group passes 598 assertions. All 23 current diagnostic probes complete without exceptions; late errors leave model bytes/metadata unchanged, correction restores the baseline and the real headless TextEditor retains its error location.

Mac validation: All 27 groups pass 6,112 assertions with zero failed or skipped groups. Compared with 6,129, existing workflow traversal checks execute 42 fewer assertions and argument-history traversal checks execute 36 fewer because successful compilation has fewer intermediate history records; 61 new explicit workflow assertions are added. No existing assertion is removed or weakened.

Upstream verification: 1,779 passed / 1 failed / 935 skipped / zero engine errors. The sole failure, ErrorInScript_ClickOff_ResetScriptEditor, also fails with the identical source mismatch on untouched 3aa58ab7; its discard-on-navigation expectation predates HMA-006 draft retention and is left unchanged. That temporary snapshot has 1,666 passed / 1 failed / 1,048 skipped because optional ROM fixtures are absent. The separate integration suite passes 26 cases with 41 optional-fixture skips and zero failures.

Scope: Synthetic regressions and existing local test fixtures; no fixture edits, numeric-rule changes, researched-address changes or game-engine changes. Successful editor compilation is one undoable operation; history traversal loops therefore execute fewer assertions without removing or weakening existing checks. Native full-window timing and gameplay remain unverified.

## 069 — Remember an external emulator for the Mac Play button

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/MacPlatform/ExternalEmulatorLauncher.cs](src/HexManiac.Avalonia/MacPlatform/ExternalEmulatorLauncher.cs) | 12 | Remember a selected application, prompt again when missing and launch saved ROMs through explicit macOS application arguments with save/draft barriers. |
| [src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs](src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs) | 95 | Offer a native application-bundle picker for first-use emulator selection. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs](src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs) | 543 | Route F5 and click handlers through the external launcher and offer Save and Launch or Cancel for unsaved work. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml](src/HexManiac.Avalonia/Windows/MainWindow.axaml) | 74 | Label and wire the Run menu and triangle as Launch in External Emulator while retaining F6 for the built-in emulator. |
| [src/HexManiac.Avalonia/Windows/EmulatorWindow.axaml](src/HexManiac.Avalonia/Windows/EmulatorWindow.axaml) | 3 | Update the F5 comparison comment to describe explicit external-emulator selection. |
| [tests/ExternalEmulatorLaunch/Program.cs](tests/ExternalEmulatorLaunch/Program.cs) | 1 | Exercise first-use cancellation, remembered/missing applications, path arguments, pending/rejected drafts, save failures and launch errors with temporary data. |
| [tests/ExternalEmulatorLaunch/ExternalEmulatorLaunch.csproj](tests/ExternalEmulatorLaunch/ExternalEmulatorLaunch.csproj) | 1 | Add the automatically discovered Mac regression group using the existing fabricated workflow fixture. |
| [tests/ExternalEmulatorLaunch/README.md](tests/ExternalEmulatorLaunch/README.md) | 1 | Document automated coverage, preference location and remaining native mGBA checks. |
| [tests/README.md](tests/README.md) | 17 | Link the external emulator launch regression. |

Validation: The new group passes 18 assertions and the Mac app builds with zero warnings/errors. Tests inject process launch and use disposable synthetic ROM/application files; no real emulator, user preference or user ROM is changed. Native application-bundle picking and actual mGBA launch remain for manual confirmation after rebuilding.

Complete Mac validation: All 28 groups pass 6,130 assertions with zero failed or skipped groups, including the 598-check script workflow group and the existing save, file-drop and external-reload protections.

Scope: Mac launch UI and a separate app preference only; shared Core RunFile behavior, Finder associations and built-in F6 emulation are unchanged. Pending script edits are saved through the existing HMA-007 barrier; cancellation, rejected drafts and failed saves never start the emulator.

## 070 — Restore object script creation and select native emulator applications

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml](src/HexManiac.Avalonia/Controls/MapTab.axaml) | 268 | Restore the Windows object-panel Create Script plus button with Core's existing command and visibility condition. |
| [src/HexManiac.Avalonia/MacPlatform/ExternalEmulatorPicker.cs](src/HexManiac.Avalonia/MacPlatform/ExternalEmulatorPicker.cs) | 9 | Select an application alias through macOS Standard Additions and normalize its path while handling cancellation and errors. |
| [src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs](src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs) | 95 | Replace the file-filter application picker that disabled installed apps on the user's Mac. |
| [tests/MapSelection/Program.cs](tests/MapSelection/Program.cs) | 221 | Exercise the live object-panel button, attached script bytes/pointers and exact-byte Undo/Redo with fabricated data. |
| [tests/ExternalEmulatorLaunch/Program.cs](tests/ExternalEmulatorLaunch/Program.cs) | 10 | Check application-chooser arguments, cancellation, errors, spaces and alias trailing-slash normalization. |
| [tests/MapSelection/README.md](tests/MapSelection/README.md) | 6 | Document object-panel creation coverage and fixture limits. |
| [tests/ExternalEmulatorLaunch/README.md](tests/ExternalEmulatorLaunch/README.md) | 3 | Describe the system application chooser and remaining native selection/launch confirmation. |

Focused validation: MapSelection passes 348 checks and ExternalEmulatorLaunch passes 24 checks. The native chooser script compiles through macOS osacompile without executing a dialog or launching an emulator. Restored object creation allocates the existing lock/faceplayer/release/end starter, attaches its pointer and preserves Undo/Redo without changing Core algorithms.

Complete Mac validation: All 28 groups pass 6,144 assertions with zero failed or skipped groups. The 14 additional checks cover the restored object button and application chooser behavior.

Scope: UI restoration and native application selection; no user ROM or preference is modified by tests. Actual mGBA selection/launch still requires confirmation after saving work and restarting the editor. The new chooser lists registered applications and supports Browse, avoiding the file-filter path observed to disable app bundles.

Native confirmation, 2026-11-03: The user confirmed that the restored object-script creation button and external mGBA launch both work. mGBA was absent from the chooser's initial application list, but Browse allowed selection of its application bundle and the ROM launched successfully; that Browse fallback remains part of the supported flow.

## 071 — Keep wild encounter editing focused during table refresh

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Tools/TableTool.cs](src/HexManiac.Core/ViewModels/Tools/TableTool.cs) | 476 | Avoid replacing a retained stream group with itself so Avalonia preserves its active text editor and autocomplete. |
| [tests/WildEncounterEditing/Program.cs](tests/WildEncounterEditing/Program.cs) | 1 | Exercise continuous level/species typing, live focus/caret retention, mouse/Enter autocomplete, exact bytes/metadata, Undo/Redo and navigation with synthetic encounters. |
| [tests/WildEncounterEditing/WildEncounterEditing.csproj](tests/WildEncounterEditing/WildEncounterEditing.csproj) | 1 | Add an automatically discovered headless regression group referencing the production Mac editor. |
| [tests/WildEncounterEditing/README.md](tests/WildEncounterEditing/README.md) | 1 | Document the failing-before reproduction, input coverage and deterministic headless limits. |
| [tests/README.md](tests/README.md) | 19 | Link wild-encounter editing regression coverage. |

Evidence: The new first-keystroke assertion fails before the fix, recording a same-group Replace notification, detached/replaced text box and lost focus while the view model remains unchanged. After the reference-identity guard, all 31 focused checks pass across LF/CRLF; existing species matching and parser output drive the visible suggestions without new parsing rules.

Complete Mac validation: All 29 groups pass 6,175 assertions with zero failed or skipped groups, including the new 31-check wild-encounter group and existing script/persistence, trainer, text and table presentation checks.

Upstream verification: 1,779 passed / 1 failed / 935 skipped / zero engine errors; the sole ErrorInScript_ClickOff_ResetScriptEditor failure is the identical HMA-006 draft-retention baseline mismatch documented in entry 068, with no new failed cases.

Scope: Table presentation lifecycle only; genuine group changes still replace controls and ordinary refresh/model notifications still run. Encounter parsing, numeric interpretation, relocation, history and engine behavior are unchanged. All new data is fabricated; native interaction on the user's ROM still needs confirmation after relaunch.

## 072 — Check continuous editing across encounter tables, scripts and map names

| File Name | Line | Summary |
| --- | --- | --- |
| [tests/WildEncounterEditing/Program.cs](tests/WildEncounterEditing/Program.cs) | 22 | Expand the focus/autocomplete matrix to production grass, surf, tree and fishing formats with their slot counts and comment sections. |
| [tests/AutocompleteMapping/Program.cs](tests/AutocompleteMapping/Program.cs) | 128 | Exercise per-character variable typing and retained focus after compilation inside the actual TabView script template. |
| [tests/TextSelection/Program.cs](tests/TextSelection/Program.cs) | 61 | Check per-character PCS/fixed-name typing and the real map-name pointer-table format through its stream editor and complete history. |
| [tests/WildEncounterEditing/README.md](tests/WildEncounterEditing/README.md) | 7 | Document the expanded encounter formats and fabricated species source. |
| [tests/AutocompleteMapping/README.md](tests/AutocompleteMapping/README.md) | 6 | Record the expanded 292-check script/autocomplete group and live tab coverage. |
| [tests/TextSelection/README.md](tests/TextSelection/README.md) | 10 | Record 63 text/name checks and distinguish map-name editing from map-header dropdown selection. |

Focused validation: All 124 encounter checks, 292 script/autocomplete checks and 63 text/name checks pass. Grass (12), surf (5), tree (5) and fishing (10) run across LF/CRLF, including percentage/rod comments; continuous input, visible offered/applied species suggestions, untouched slots/rate/pointers/metadata and history remain passing. Script compilation preserves the live focused editor, and map-name edits preserve neighboring names, pointer bytes and exact Undo/Redo states.

Complete Mac validation: All 29 groups pass 6,300 assertions with zero failed or skipped groups; the broader coverage adds 125 assertions without changing application code.

Scope: Tests and coverage documentation only; no further application change was needed beyond entry 071. Headless synthetic fixtures exercise actual TabView templates and production parsers; native map-header dropdown interaction, every game engine/script mode and real-ROM behavior are not claimed.

## 073 — Keep mouse autocomplete selection from dismissing its own popup

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml](src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml) | 17 | Prevent suggestion buttons from taking keyboard focus so the editor's LostFocus handler cannot dismiss the list before release completes Click. |
| [tests/WildEncounterEditing/Program.cs](tests/WildEncounterEditing/Program.cs) | 79 | Replace direct Click invocation with pointer press/release and check focus plus popup lifetime across all encounter formats. |
| [tests/AutocompleteMapping/Program.cs](tests/AutocompleteMapping/Program.cs) | 91 | Exercise popup pointer selection in script autocomplete while preserving keyboard completion coverage. |
| [tests/WildEncounterEditing/README.md](tests/WildEncounterEditing/README.md) | 20 | Document the pointer-event reproduction and the earlier direct-handler coverage gap. |
| [tests/AutocompleteMapping/README.md](tests/AutocompleteMapping/README.md) | 6 | Record stronger script mouse-acceptance coverage and updated checks. |

Evidence: The user's mouse-only failure reproduces when the test delivers real headless pointer press/release: the focusable suggestion button makes the text editor lose focus, which clears the popup before Click. Prior direct Click tests bypassed this focus transition. The shared non-focusable completion-button style preserves editor focus and allows the existing Click handler to apply the option; arrows/Enter remain handled by the editor as before.

Validation: All 29 Mac groups pass 6,306 assertions with zero failed or skipped groups, including 128 encounter checks and 294 script/autocomplete checks. The pointer press assertion requires editor focus and an open list before release; accepted values, other encounter slots, pointers, metadata and Undo/Redo remain passing.

Scope: Shared autocomplete presentation only; matching, selected values, numeric parsing, compilation, ROM writes and history algorithms are unchanged. Pointer tests use synthetic encounters and scripts in headless popup windows; native mouse confirmation remains for the user's next relaunch.

## 074 — Pan the Mac map with Option and primary drag

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 234 | Route macOS Option-left-drag through the existing middle-button pan path while tracking primary-button release independently of modifier state. |
| [tests/MapSelection/Program.cs](tests/MapSelection/Program.cs) | 133 | Add exact pan-distance, modifier-release ordering, copied/single brush retention, byte/metadata/history integrity and post-pan painting regressions. |
| [tests/MapSelection/README.md](tests/MapSelection/README.md) | 21 | Explain the trackpad gesture, preserved brush behavior and headless/native verification boundary. |

Evidence: The new Option-drag position assertion fails before the UI mapping is added. Afterward the focused map group passes 394 checks, including 46 added pan/post-pan assertions. Option-left and middle drag move by the expected screen pixels at 1x/2x zoom without modifying any ROM bytes, metadata or saved/Undo/Redo state. The brush stays selected, release completes even when Option was released first, and ordinary painting remains undoable afterward.

Complete Mac validation: All 29 groups pass 6,352 assertions with zero failed or skipped groups; the build introduces no new compiler warnings.

Scope: Mac pointer mapping and physical release tracking only; shared Core painting/panning/history, Control-click selection, Command rectangle painting, arrow-key navigation and Space reveal behavior are unchanged. All fixtures are fabricated; native trackpad confirmation remains for the user after relaunch.

## 075 — Open tab menus with Control-click and reveal files in Finder

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs](src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs) | 757 | Open the clicked tab's menu before Control-click can start drag capture and route file reveal to its owning saved file with visible errors. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml](src/HexManiac.Avalonia/Windows/MainWindow.axaml) | 429 | Connect Show in Finder to the Mac reveal handler instead of the shared Windows Explorer method. |
| [src/HexManiac.Avalonia/MacPlatform/MacFileReveal.cs](src/HexManiac.Avalonia/MacPlatform/MacFileReveal.cs) | 7 | Provide an injectable reveal capability and build/run macOS open -R requests using separate path arguments. |
| [src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs](src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs) | 45 | Implement the native file-reveal capability on the Mac file system. |
| [tests/TabMenus/Program.cs](tests/TabMenus/Program.cs) | 1 | Test Control/secondary-click, menu targeting, duplicate/reorder/close, ROM/map Finder routing, arguments and error handling without launching Finder. |
| [tests/TabMenus/TabMenus.csproj](tests/TabMenus/TabMenus.csproj) | 1 | Add an automatically discovered headless group using fabricated map data. |
| [tests/TabMenus/README.md](tests/TabMenus/README.md) | 1 | Document event coverage, Finder substitution and remaining native checks. |
| [tests/README.md](tests/README.md) | 21 | Link tab menu and file-reveal coverage. |

Evidence: The Control-click assertion fails before the handler change. All 23 focused checks pass afterward, including both Control release orders, unchanged tab order during menu gestures, targeted duplication, ordinary left-drag reordering and middle-click close. Finder argument construction preserves spaces/quotes; the actual menu routes ROM and map tabs to their owning file, handles missing/untitled files and surfaces launch failures without invoking explorer.exe.

Complete Mac validation: All 30 groups pass 6,375 assertions with zero failed or skipped groups, including the new 23-check tab menu/Finder group.

Scope: Mac tab UI and platform reveal only; Windows/shared Core file-location commands, diff/patch implementations, ROM data and history algorithms are unchanged. Tests use synthetic files/models and a reveal recorder. Native trackpad/menu behavior and Finder selecting the real file remain for user confirmation after relaunch.

## 076 — Route clipboard commands to the focused text editor

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Windows/MainWindow.Editing.cs](src/HexManiac.Avalonia/Windows/MainWindow.Editing.cs) | 12 | Route editing commands to the focused TextBox, guard read-only operations, and use existing tab commands when focus is outside text input. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs](src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs) | 89 | Initialize shared editing commands and intercept text clipboard shortcuts before they can reach binary editing. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml](src/HexManiac.Avalonia/Windows/MainWindow.axaml) | 97 | Replace direct ROM clipboard bindings with the commands installed by the window for the native Edit menu. |
| [tests/ClipboardRouting/ClipboardRouting.csproj](tests/ClipboardRouting/ClipboardRouting.csproj) | 1 | Add a full-window headless regression group with synthetic ROMs and the headless clipboard. |
| [tests/ClipboardRouting/Program.cs](tests/ClipboardRouting/Program.cs) | 53 | Exercise routed clipboard shortcuts, native menu commands, script rejection/correction/history, ordinary text templates and preserved binary paste. |
| [tests/ClipboardRouting/README.md](tests/ClipboardRouting/README.md) | 1 | Document the original ROM-writing reproduction, automated coverage and remaining native UI checks. |
| [tests/README.md](tests/README.md) | 23 | Link the new clipboard focus regression group. |

Evidence: With the script TextBox focused, the original routed Cmd+C copied hex data and Cmd+V wrote clipboard bytes `AA BB` directly into the synthetic ROM, replacing opcode `6A` with `AA` and removing source focus. Both window shortcuts and native Edit menu commands were bound directly to the selected ROM's commands. They now share focus routing through the native TextBox operations; an unavailable text operation never falls through to binary editing. Select All, Cut, legacy Cmd+B, Deep Copy and Delete receive the same text-focus guard.

Validation: The complete Mac suite passes 31 groups and 6,453 assertions with zero failures or skipped groups. The new group passes 78 assertions, including backward selection paste, LF/CRLF source, focus retention, byte/metadata preservation on rejection, retained diagnostics, blocked Save/Save As, correction/retry and actual Cmd+Z/Shift+Cmd+Z round trips. Dialogue, fixed name-table text and all four encounter stream sizes retain text clipboard routing; native-menu binary paste remains undoable. The new group was also rebuilt and rerun after strengthening keyboard-history and paste-event checks. Builds report zero warnings and errors.

Scope: Mac presentation/command routing only; compilation, numeric parsing, pointer relocation, ROM history and game-engine behavior are unchanged. Existing workflow checks continue to cover HMA-006/007/008 protections. Headless tests use synthetic files and their own clipboard; AppKit menu-bar event delivery and clipboard exchange with another application remain for user confirmation after relaunch.

## 077 — Keep map event shortcuts out of focused text fields

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 62 | Guard map key commands at both availability and execution so event text fields receive Delete, Backspace, arrows and Escape without invoking map actions. |
| [tests/MapRendering/SyntheticMapFixture.cs](tests/MapRendering/SyntheticMapFixture.cs) | 39 | Add an optional fabricated Script Event table while preserving the existing default map fixture. |
| [tests/MapEventEditing/MapEventEditing.csproj](tests/MapEventEditing/MapEventEditing.csproj) | 1 | Add a full-window headless map event editing regression group. |
| [tests/MapEventEditing/Program.cs](tests/MapEventEditing/Program.cs) | 47 | Deliver routed editing keys to actual Script Event fields and verify event retention, byte/metadata safety and normal map command history. |
| [tests/MapEventEditing/README.md](tests/MapEventEditing/README.md) | 1 | Document the deletion reproduction, automated coverage and native keyboard checks. |
| [tests/README.md](tests/README.md) | 25 | Link the map event keyboard editing regression group. |

Evidence: On a fabricated map, both Backspace and forward Delete removed the Script Event while its Script Address TextBox had focus, reducing scriptCount from 1 to 0. The map's ancestor KeyBindings invoked DeleteCommand before the textbox handled those keys and bypassed the text-focus guard in the separate key event handler. The bindings now decline map commands while any supported text entry control has focus, and recheck focus before execution; their existing map-focused commands and parameters are retained.

Validation: The new group passes 63 assertions for character and boundary deletion, caret arrows, Escape, stable field/focus, event count, unchanged pointers/bytes/metadata when removing redundant address zeroes, a second event field, address clearing with existing null-pointer parsing and Undo, map-focused deletion with exact Undo/Redo, and map-focused panning/cancellation. The first complete-suite attempt stopped on the unrelated FocusAnimation movement timing assertion; that group passed all 13 assertions on an immediate standalone rerun. The fresh complete-suite rerun passes all 32 groups and 6,516 assertions with zero failures or skipped groups; builds report zero warnings and errors.

Scope: Mac map keyboard command routing only; pointer parsing, event data layouts, compilation, history and game-engine behavior are unchanged. Tests use synthetic maps without user ROMs. Native Delete/Fn+Delete interaction remains for user confirmation after relaunch.

## 078 — Make dialogue overflow and script error underlines visible

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/DiagnosticUnderlineLayer.cs](src/HexManiac.Avalonia/Controls/DiagnosticUnderlineLayer.cs) | 15 | Draw warning/error paths using the actual textbox layout and viewport so blank syntax placeholders cannot suppress them. |
| [src/HexManiac.Avalonia/Controls/TextEditor.axaml](src/HexManiac.Avalonia/Controls/TextEditor.axaml) | 33 | Add a non-interactive diagnostic layer above the transparent editor without changing text input or focus. |
| [src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs](src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs) | 188 | Preserve comment text and coalesced diagnostic publication while replacing whitespace TextDecorations with the independent painter. |
| [tests/DialogueDiagnostics/DialogueDiagnostics.csproj](tests/DialogueDiagnostics/DialogueDiagnostics.csproj) | 1 | Add headless synthetic dialogue rendering checks using the real compiler and editor. |
| [tests/DialogueDiagnostics/Program.cs](tests/DialogueDiagnostics/Program.cs) | 49 | Check actual underline pixels for the reported script, correction, manual line breaks, scrolling, whitespace/error spans and existing Enter encoding. |
| [tests/DialogueDiagnostics/README.md](tests/DialogueDiagnostics/README.md) | 1 | Document the rendering defect, pixel coverage, Enter behavior and real-ROM limits. |
| [tests/ScriptEditorLayout/Program.cs](tests/ScriptEditorLayout/Program.cs) | 77 | Retain diagnostic coalescing and context-switch checks against the new painter's diagnostic snapshot. |
| [tests/ScriptEditorLayout/README.md](tests/ScriptEditorLayout/README.md) | 17 | Distinguish diagnostic publication checks from the new rendered-pixel verification. |
| [tests/README.md](tests/README.md) | 27 | Link the visible dialogue diagnostic regression group. |

Evidence: The exact reported `msgbox.npc <auto>` dialogue generated an overflow warning but rendered no underline. The original decoration was attached to blank syntax placeholders, whose trailing-whitespace TextDecorations Avalonia omits. Earlier diagnostic-record/decorated-Run counts did not verify visible pixels. The new path renderer uses actual text-position rectangles, clips to the scroll viewport, and retains theme warning/error colors without modifying source or adding prompts.

Validation: The new group passes 27 assertions covering the screenshot's exact sentence with LF/CRLF, visible pixels below the overflowing suffix, disappearing warnings after correction or manual breaks, both scroll directions, whitespace-only warnings, red error underlines, stale/unsorted ranges, retained focus and unchanged source/ROM bytes/metadata. A rendered image of the example was also inspected visually. Existing PCS behavior is characterized unchanged: Enter inserts a line break, subsequent single breaks scroll, and a blank line inserts a paragraph/page break. The complete Mac suite passes 33 groups and 6,543 assertions with zero failures or skipped groups; builds report zero warnings and errors.

Scope: Mac diagnostic presentation only; width estimation, compilation, relocation, history, text encoding and game-engine behavior are unchanged. No popup or automatic wrapping is added. The synthetic probe uses the fallback font-width path; in-game appearance with a real ROM remains for user confirmation after relaunch.

## 079 — Select short field values when navigating with Tab

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TabView.axaml](src/HexManiac.Avalonia/Controls/TabView.axaml) | 566 | Attach keyboard focus handling to the shared stream field template. |
| [src/HexManiac.Avalonia/Controls/TabView.axaml.cs](src/HexManiac.Avalonia/Controls/TabView.axaml.cs) | 618 | Select complete single-line PCS table entries only when focus arrives through Tab navigation. |
| [src/HexManiac.Avalonia/Controls/AngleTextBox.cs](src/HexManiac.Avalonia/Controls/AngleTextBox.cs) | 158 | Preserve the navigation reason and modifiers when a proxy field creates its real textbox so scalar fields retain native Tab selection. |
| [tests/ShortFieldTabSelection/ShortFieldTabSelection.csproj](tests/ShortFieldTabSelection/ShortFieldTabSelection.csproj) | 1 | Add headless short-field selection regressions using the actual table and scalar controls. |
| [tests/ShortFieldTabSelection/Program.cs](tests/ShortFieldTabSelection/Program.cs) | 37 | Exercise multiple-choice replacements, pointers, neighboring bytes, history, proxy activation and exclusions for large editors. |
| [tests/ShortFieldTabSelection/README.md](tests/ShortFieldTabSelection/README.md) | 1 | Document keyboard-only selection scope, automated coverage and native checks. |
| [tests/README.md](tests/README.md) | 29 | Link the short-field Tab selection regression group. |

Evidence: The multiple-choice fields use a shared multiline-capable TextBox, which does not select everything on Tab as a native single-line textbox does. The handler is limited to PCS streams containing one source line; whole lists/tables and dedicated script/dialogue editors are excluded. AngleTextBox also lost Tab's focus reason during proxy activation, and subscribed to attachment after assigning Content; it now subscribes before attachment and forwards the original navigation reason without forcing mouse or programmatic selection.

Validation: The new group passes 21 assertions for Tab/Shift+Tab, typing replacement, mouse placement, programmatic focus, refresh without reselection, unchanged neighboring bytes/pointers, exact Undo/Redo, scalar proxy activation, multiline/list exclusions, script indentation and dedicated dialogue exclusion. The complete-suite attempt stopped on the previously observed FocusAnimation movement timing assertion, which also failed on an isolated rerun; no production animation code changed. All remaining 25 groups were then freshly built and executed using the documented build/run procedure and passed 5,502 assertions. Across all 34 groups, 33 passed 6,551 assertions, one group failed (FocusAnimation), and none were skipped. Builds report zero warnings and errors; a completely passing suite is not claimed.

Scope: Mac field focus/selection only; numeric parsing, text encoding, relocation, history and game-engine behavior are unchanged. Native multiple-choice and scalar Tab navigation remain for user confirmation after relaunch.

## 080 — Restore script automatic pointers and unused-ID assistance

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TabView.axaml](src/HexManiac.Avalonia/Controls/TabView.axaml) | 860 | Bind the script control's owning CodeBody explicitly and correct the Mac unused-ID shortcut hints. |
| [src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs](src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs) | 143 | Synchronize reentrantly assisted source into the native textbox before applying its requested caret. |
| [src/HexManiac.Core/ViewModels/Tools/CodeTool.cs](src/HexManiac.Core/ViewModels/Tools/CodeTool.cs) | 346 | Use a guarded absolute caret target after automatic pointer insertion and remove a redundant second insertion attempt. |
| [tools/ScriptWorkflowReview/WorkflowFixture.cs](tools/ScriptWorkflowReview/WorkflowFixture.cs) | 104 | Allow UI dispatch to be queued in synthetic editor tests while retaining deterministic compilation debounce. |
| [tests/ScriptAssistance/ScriptAssistance.csproj](tests/ScriptAssistance/ScriptAssistance.csproj) | 1 | Add full-window script typing assistance checks with fabricated ROM data. |
| [tests/ScriptAssistance/Program.cs](tests/ScriptAssistance/Program.cs) | 21 | Exercise real Space/Cmd+U input, helper buttons, visible source/caret synchronization and history round trips. |
| [tests/ScriptAssistance/README.md](tests/ScriptAssistance/README.md) | 1 | Document UI coverage, investigator isolation and existing unused-ID scanner limitations. |
| [tests/README.md](tests/README.md) | 31 | Link the script assistance regression group. |

Evidence: The script TextEditor's Tag resolved to its nested TextEditorViewModel instead of CodeBody, so selection/key handlers returned early and the model caret stayed at zero. Fixing that binding also exposed a reentrant two-way binding issue: `<auto>` existed in model source while the native textbox retained old text, and a relative deferred caret increment could position incorrectly. The restoration synchronizes the source before a guarded absolute caret request. Flag/variable eligibility, investigator allocation and normal compilation remain existing Core behavior.

Validation: Five focused groups pass 1,133 assertions: ScriptAssistance 61, ScriptWorkflowSafety 598, AutocompleteMapping 294, ClipboardRouting 78 and ScriptEditorLayout 102. Builds report zero warnings/errors on this verification run. Actual typed msgbox/msgbox.npc/preparemsg insert visible `<auto>`; setflag/checkflag and setvar/addvar expose the proper helper and insert one explicit operand through button or Cmd+U, with exact Undo/Redo. The fixture's predictable investigator verifies UI routing, not real-ROM scan completeness.

Scope: Restore existing behavior in its own commit; dialogue scaffolding is a separate subsequent change. Numeric parsing, allocator/scanner rules, ROM relocation, encoding and history algorithms are unchanged. Native typing remains for user confirmation after relaunch.

## 081 — Create complete dialogue scaffolds instead of completing missing braces

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Tools/CodeBody.cs](src/HexManiac.Core/ViewModels/Tools/CodeBody.cs) | 282 | Insert `<auto>` plus one complete dialogue block for an unambiguous final text operand and return its editable-body caret target. |
| [src/HexManiac.Core/ViewModels/Tools/CodeTool.cs](src/HexManiac.Core/ViewModels/Tools/CodeTool.cs) | 346 | Use the scaffold's absolute caret target and replace immediate legacy missing-brace completion with retained source and compiler diagnostics. |
| [src/HexManiac.Core/Models/Code/ScriptParser.cs](src/HexManiac.Core/Models/Code/ScriptParser.cs) | 555 | Normalize complete inline blocks without inventing missing closers and atomically reject unfinished resource blocks with a diagnostic. |
| [src/HexManiac.Tests/CodeToolTests.cs](src/HexManiac.Tests/CodeToolTests.cs) | 758 | Replace the obsolete automatic-close expectation with rejected-source, diagnostic and unchanged-byte assertions. |
| [tests/DialogueScaffolding/DialogueScaffolding.csproj](tests/DialogueScaffolding/DialogueScaffolding.csproj) | 1 | Add dedicated synthetic dialogue scaffold, compilation and persistence safety checks. |
| [tests/DialogueScaffolding/Program.cs](tests/DialogueScaffolding/Program.cs) | 9 | Cover seven command forms, three newline styles, exact allocation/caller/history behavior, opt-outs, rejected drafts and stale caret callbacks. |
| [tests/DialogueScaffolding/README.md](tests/DialogueScaffolding/README.md) | 1 | Document the new typing flow, replacement of runtime brace completion and verification limits. |
| [tests/ScriptAssistance/Program.cs](tests/ScriptAssistance/Program.cs) | 45 | Verify real typing creates exactly one scaffold, places the caret inside and immediately accepts dialogue text. |
| [tests/ScriptAssistance/README.md](tests/ScriptAssistance/README.md) | 6 | Update the keyboard-flow description for the new caret destination. |
| [tests/README.md](tests/README.md) | 33 | Link the scaffold regression group. |

Behavior: A space before a supported final dialogue pointer now creates `<auto>` followed by opening/closing braces with an editable blank line, and positions the caret on that line. Common msgbox variants, preparemsg and loadpointer's text operand are covered. Existing blocks and following commands are preserved; multiple-text-pointer commands and non-text resources retain ordinary pointer assistance. Existing automatic-insertion and Spartan-mode opt-outs remain. Source assistance itself writes no ROM bytes; allocation and relocation occur through the normal compiler/history transaction.

Replacement: The previous immediate and compiler-time missing-brace completion paths are disabled in the application. Incomplete manually written blocks remain rejected drafts with visible diagnostics, including Save/Save As/Close protection and correction/retry. Complete inline blocks still normalize normally. The public legacy normalization utility retains its compatibility mode for direct callers; application compilation explicitly disables completion.

Validation: DialogueScaffolding passes 261 assertions and real-window ScriptAssistance passes 64. Together with ScriptWorkflowSafety's 598 assertions, focused final checks pass 923 assertions. Tests verify LF/CRLF/CR, end-of-editor caret timing, exact text bytes, relocated callers, complete Undo/Redo, existing-block exclusion, opt-outs, stale callbacks after Undo/navigation, and rollback of earlier resource relocation when a later block is unclosed. All 36 Mac groups were exercised: 35 pass 6,876 assertions, FocusAnimation fails its previously observed movement timing assertion, and no groups are skipped. The standard runner stopped at that failure; the remaining 26 groups were freshly built/run separately, and the final expanded scaffold checks were rebuilt/rerun afterward. Existing Core compiler warnings remain; no build errors occurred.

Upstream: After replacing the obsolete brace-closing expectation, in-process verification reports 1,779 passed, one failed and 935 skipped from 2,715 discovered cases. The remaining failure is the previously documented ScriptTests.ErrorInScript_ClickOff_ResetScriptEditor expectation, which conflicts with retained rejected source. Results are retained in artifacts/upstream-tests/run.Me9kcz. A fully passing overall suite is not claimed.

Scope: Source scaffolding and explicit unfinished-block rejection only; valid-script encoding, numeric parsing, unused-ID scanning/allocation rules, pointer relocation, ROM history and game-engine behavior are unchanged. No user ROM was used. Native typing remains for user confirmation after relaunch.

## 082 — Scaffold player, camera and other movement blocks

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Tools/CodeBody.cs](src/HexManiac.Core/ViewModels/Tools/CodeBody.cs) | 306 | Extend complete source scaffolding to an unambiguous final movement pointer while retaining dialogue support and other resource exclusions. |
| [tests/DialogueScaffolding/Program.cs](tests/DialogueScaffolding/Program.cs) | 87 | Verify player/camera/NPC/applymovement scaffolds, exact movement bytes, actor operands, pointers, history and rejected-draft protection. |
| [tests/ScriptAssistance/Program.cs](tests/ScriptAssistance/Program.cs) | 21 | Exercise actual move.player/move.camera typing, inside-body caret placement and subsequent movement input. |
| [tests/DialogueScaffolding/README.md](tests/DialogueScaffolding/README.md) | 10 | Document movement eligibility and movement-specific encoding checks. |
| [tests/ScriptAssistance/README.md](tests/ScriptAssistance/README.md) | 6 | Document movement typing coverage alongside dialogue and unused-ID helpers. |
| [tests/README.md](tests/README.md) | 33 | Include movement in the scaffold regression coverage description. |

Behavior: Typing move.player or move.camera followed by a space now inserts `<auto>` plus a complete movement block and positions the caret on its editable blank line. The same final-movement-operand rule supports move.npc and applymovement after their actor argument. Existing movement blocks are preserved, and other resource types such as marts retain pointer-only assistance. Movement bodies continue through the existing enum-based movement encoder with FE termination; camera/player macro requirements and actor operands are unchanged.

Validation: Four focused groups pass 2,622 assertions: DialogueScaffolding 384, ScriptAssistance 80, ScriptWorkflowSafety 598 and ScriptArgumentSafety 1,560. New coverage includes four movement command forms across LF/CRLF/CR, exact instruction bytes and terminator, preserved actor IDs/following end command, known callers after relocation, complete byte/metadata Undo/Redo, existing-block exclusion and unclosed-draft save protection. Actual window typing checks both player and camera macros. Builds complete without errors; existing Core warnings are unchanged. The complete suites were not repeated for this narrow extension; their most recent limitations remain recorded in entry 081.

Scope: Movement source scaffolding only; compilation/encoding, pointer relocation, history, numeric parsing and game-engine behavior are unchanged. Tests use synthetic data and no user ROMs. Native movement typing remains for user confirmation after relaunch.

## 083 — Keep cold map discovery and rendering on the UI thread

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TileImage.cs](src/HexManiac.Avalonia/Controls/TileImage.cs) | 324 | Queue/coalesce live map preparation on the UI thread instead of reading lazy model state inside Task.Run, and marshal worker refresh notifications safely. |
| [src/HexManiac.Avalonia/Controls/TileImage.cs](src/HexManiac.Avalonia/Controls/TileImage.cs) | 255 | Cancel queued model preparation on detachment or context replacement and refresh the current context on reattachment. |
| [tests/MapRendering/SyntheticMapFixture.cs](tests/MapRendering/SyntheticMapFixture.cs) | 43 | Add an optional real NPC event whose unformatted script/text pointers force lazy metadata discovery without altering default fixtures. |
| [tests/MapThreadSafety/MapThreadSafety.csproj](tests/MapThreadSafety/MapThreadSafety.csproj) | 1 | Add headless thread-ownership regression checks with the actual map renderer and command subscriber. |
| [tests/MapThreadSafety/Program.cs](tests/MapThreadSafety/Program.cs) | 23 | Reproduce the reported cold-map Save-button failure and verify UI ownership, metadata/history, cancellation, painting and responsiveness. |
| [tests/MapThreadSafety/README.md](tests/MapThreadSafety/README.md) | 1 | Document the precise reproduction, root cause, fixed ownership and real-ROM performance limits. |
| [tests/README.md](tests/README.md) | 35 | Link the cold NPC map/thread-safety regression group. |

Evidence: Opening the map editor could report InvalidOperationException, "Call from invalid thread", while still allowing subsequent interaction. PixelImage read BlockMapViewModel.PixelData inside Task.Run, but that getter can lazily discover NPC scripts/text, add formatting metadata and update history. The resulting Save.CanExecuteChanged reached an Avalonia Button from the worker thread. A synthetic NPC map with unformatted text and a real bound Save button reproduced the same stack before the fix. Earlier map checks either had no such NPC data or pre-read pixels, missing cold discovery.

Behavior: Map preparation now runs as a deferred, coalesced UI-thread pass. Model metadata/history changes and bitmap publication share UI ownership; stale or detached requests cannot prepare abandoned models. Worker refresh notifications are marshalled before touching controls. Zoom still changes layout without regenerating pixels, and reattachment refreshes the current context. The fix does not suppress the exception or merely dispatch the final Save notification.

Validation: Six focused groups pass 1,798 assertions, including 22 new thread-safety checks. The complete Mac suite passes 37 groups and 7,050 assertions with zero failures or skipped groups. The new group was rebuilt/rerun after strengthening direct history-thread verification. Checks require successful bitmap publication and actual NPC annotation, unchanged ROM bytes, metadata-only history semantics, UI-owned history/Save notifications, one publication per refresh burst, worker requests, canceled/null/replaced/detached contexts, reattachment and painting Undo/Redo. Cold synthetic 36x36/96x64 renders measured roughly 19–32 ms on this host; higher-priority UI work ran before the queued cold render. Builds report zero errors.

Scope: Mac pixel presentation/thread ownership only; NPC discovery, metadata tracking, ROM bytes, script compilation, encoding, relocation, history algorithms and game-engine behavior are unchanged. No user ROM was opened. Synthetic timings are not guarantees for complex real maps; native map entry, zoom and painting remain for user confirmation after relaunch. Any future background map rendering must use immutable prepared state rather than live model getters.

## 084 — Run script assistance after accepting command suggestions

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs](src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs) | 231 | Publish script completions with their intended caret for Enter and suggestion clicks while preserving physical Space input. |
| [src/HexManiac.Core/ViewModels/TextEditorViewModel.cs](src/HexManiac.Core/ViewModels/TextEditorViewModel.cs) | 95 | Add an atomic content/caret replacement for completion observers. |
| [src/HexManiac.Core/ViewModels/Tools/CodeBody.cs](src/HexManiac.Core/ViewModels/Tools/CodeBody.cs) | 367 | Refresh operand help and unused-ID eligibility after accepting a completion. |
| [tests/ScriptAssistance/Program.cs](tests/ScriptAssistance/Program.cs) | 20 | Exercise nine commands through typed Space, Enter and actual suggestion clicks, including compilation and history. |
| [tests/ScriptAssistance/README.md](tests/ScriptAssistance/README.md) | 3 | Document completion acceptance coverage. |

Evidence: The expanded real-window regression failed before the fix: accepting a partial command changed source while ScriptChanged still saw the old caret. The trailing space supplied by the completion therefore missed pointer/block assistance. Selection also needed to refresh flag/variable helper eligibility.

Behavior: Enter and mouse acceptance use the same existing pointer/scaffold rules as typing, including dialogue and movement bodies and their interior caret. Flag/variable completions expose their existing helpers. Physical Space acceptance retains its following text-input event; operand completions and source suffixes retain their existing mapping.

Validation: ScriptAssistance 258, AutocompleteMapping 294, DialogueScaffolding 384 and ScriptWorkflowSafety 598 pass (1,534 assertions). The regression reproduced missing auto insertion before the fix. Tests use synthetic data. Existing Core build warnings are unchanged. Compilation, numeric parsing, allocator rules, ROM encoding, relocation and delayed recovery protections are unchanged.

## 085 — Restore map event context menus on Mac

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml](src/HexManiac.Avalonia/Controls/MapTab.axaml) | 24 | Restore the event menu resource with shared command labels and parameters. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 274 | Release map capture before opening an event menu and open it explicitly only when an event is hit. |
| [tests/MapRendering/SyntheticMapFixture.cs](tests/MapRendering/SyntheticMapFixture.cs) | 54 | Add an optional synthetic warp without changing default map fixtures. |
| [tests/MapEventMenus/MapEventMenus.csproj](tests/MapEventMenus/MapEventMenus.csproj) | 1 | Register the headless event-menu regression group. |
| [tests/MapEventMenus/Program.cs](tests/MapEventMenus/Program.cs) | 53 | Exercise real secondary/Control-clicks, warp commands, pointer release, canceled creation, tabs and map history. |
| [tests/MapEventMenus/README.md](tests/MapEventMenus/README.md) | 1 | Document routing coverage and map-allocation limits. |
| [tests/README.md](tests/README.md) | 28 | Link the event-menu regression group. |

Evidence: Shared Core already supplied Create New Map, Open in New Tab and Delete Warp, and SelectDown already prioritized visible events. The Mac map surface had no context menu for ShowMenu to open. The new actual-pointer regression failed before the restoration.

Behavior: Secondary-click and Mac Control-click on a visible warp open those original commands before underlying tile selection. Other supported event types reuse their existing shared menus. Opening a menu releases capture immediately. Explicit opening prevents empty cells from displaying a stale event menu; their tile-selection gestures remain available.

Validation: MapEventMenus 68, MapSelection 394 and MapZoom 1,158 pass (1,620 assertions). Coverage includes both gestures at 1x/2x zoom, early Control release, canceled bank selection with unchanged bytes/metadata/history, destination tabs, deletion Undo/Redo and painting/panning exclusions. The existing map-selection group verifies exact repeated stamps and Option-pan. Map creation allocation/relocation algorithms and game behavior are unchanged; the new routing test cancels before allocation and uses no user ROM.

## 086 — Stabilize object sprite previews and recover recycled selector state

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml](src/HexManiac.Avalonia/Controls/MapTab.axaml) | 36 | Fit complete ordinary/dynamic overworld images into fixed-size previews and stable popup rows. |
| [src/HexManiac.Avalonia/Controls/AngleComboBox.cs](src/HexManiac.Avalonia/Controls/AngleComboBox.cs) | 141 | Add opt-in selection recovery from a current one-way model index after option replacement, with coalesced queued updates. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 320 | Let focused combo boxes handle their keys without triggering map commands. |
| [tests/MapSpriteSelection/MapSpriteSelection.csproj](tests/MapSpriteSelection/MapSpriteSelection.csproj) | 1 | Register the headless object sprite-selector regression group. |
| [tests/MapSpriteSelection/Program.cs](tests/MapSpriteSelection/Program.cs) | 66 | Reproduce layout/selection failures and verify large-image transitions, real input, virtualized rows, history and context replacement. |
| [tests/MapSpriteSelection/README.md](tests/MapSpriteSelection/README.md) | 1 | Document root causes, coverage, sample-ROM observation and native confirmation limits. |
| [tests/README.md](tests/README.md) | 30 | Link the sprite-selector regression group. |

Evidence: A read-only inspection of the local FireRed sample found sprite 108 is 64x64 while 106/107/109 are 32x32. Existing 2x previews expanded 108 to 128x128 and moved the surrounding fields; a synthetic regression reproduced the layout jump. History/reopen also reproduced a blank selector: UI index -1 with the ROM graphics byte still 108. Replacing option objects after selection evaluation coerced the UI selection; ignoring its negative write protected the ROM but left the unchanged model index undisplayed. A separate actual-key check showed map arrow commands intercepting selector navigation.

Behavior: Full sprite images fit into 64x64 thumbnails without cropping or altering their source bitmaps. Ordinary and dynamic graphics pickers use stable rows. An opt-in one-way model index restores the current selection after option changes settle, and generation checks prevent abandoned queued replacements from restoring stale IDs. Existing two-way selection remains responsible for actual graphics edits. Focused combo boxes retain their native key handling.

Validation: MapSpriteSelection passes 180 assertions. The tests require stable layout, complete bitmap dimensions, fresh pixels across small/large transitions (including an expanded 96x128 image), actual keyboard/wheel/pointer selection, populated virtualized rows around 108, popup reopen, unchanged unrelated ROM bytes and pointer/format metadata, exact Undo/Redo, and rapid switches between objects with different graphics IDs. The pre-fix stable-layout and history/reopen assertions failed as described above. The complete Mac suite, rebuilt with `scripts/verify-mac.sh --no-restore`, passes 39 groups and 7,476 assertions with zero failures and zero skipped groups across all three fixes. No new build warnings were introduced.

Scope: Mac selector presentation, selection recovery and keyboard routing only. Sprite bytes, numeric IDs, dynamic graphics rules, map rendering, engine behavior, compilation, pointers and shared history algorithms are unchanged. Automated tests use synthetic data; the sample ROM was read without saving. The user's exact native trackpad/blank-list sequence remains for confirmation after relaunch.

## 087 — Eliminate complex NPC hover-preview flicker

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapHoverPreview.cs](src/HexManiac.Avalonia/Controls/MapHoverPreview.cs) | 23 | Replace the pointer tooltip with one input-transparent, tile-anchored overlay that retains content and fits complete summaries to the map viewport. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml](src/HexManiac.Avalonia/Controls/MapTab.axaml) | 1190 | Add a clipped hover layer below editor panels/tutorials without intercepting map input. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 148 | Translate the highlighted tile into overlay coordinates and dismiss previews when wheel zoom invalidates their position. |
| [tests/MapHoverPreview/MapHoverPreview.csproj](tests/MapHoverPreview/MapHoverPreview.csproj) | 1 | Register the synthetic hover-feedback and real-map integration regression group. |
| [tests/MapHoverPreview/Program.cs](tests/MapHoverPreview/Program.cs) | 35 | Reproduce the original complex-tooltip loop and verify stable pointer routing, complete content, fitting, lifecycle, zoom and data/history invariants. |
| [tests/MapHoverPreview/README.md](tests/MapHoverPreview/README.md) | 1 | Document the reproduction, new ownership, fitting behavior and native confirmation limits. |
| [tests/TutorialLayout/Program.cs](tests/TutorialLayout/Program.cs) | 127 | Preserve source ownership, dismissal, cached re-entry and empty-cell clearing checks with the new overlay contract. |
| [tests/README.md](tests/README.md) | 40 | Link the stable NPC hover-preview regression group. |

Evidence: A synthetic five-text/five-image preview reproduced 38 open/close transitions during 20 pointer movements; a simple summary stayed stable. The old hit-testable ToolTip used pointer placement, and fitting a tall summary could put it under the pointer. Map exit removed the tooltip; re-entry reopened cached content in a new instance, creating the feedback loop. Existing tests checked source/cache bookkeeping without routing pointer input over a large rendered preview.

Behavior: NPC summaries now use one noninteractive view inside the map, anchored beside the hovered tile. The preview cannot steal pointer input or invoke automatic/native tooltip behavior. Same-cell movement reuses the view/content; deliberate map exit hides it, same-cell re-entry restores it, and empty cells or changed contexts clear the cache. Placement fits within the map, updates after actual layout/viewport changes, and scales down very tall summaries with every text/image row retained. Tutorials and editor panels remain above it. Wheel zoom dismisses the old-position preview.

Validation: MapHoverPreview passes 134 assertions, including zero visibility oscillation or spurious pointer exits for both preview sizes, actual click-through input, all five image bitmaps, complete summary content, viewport corners, small-window resizing, detach/reattach, context replacement, and the real map's NPC summaries at two zoom levels. Real-map checks also require unchanged ROM bytes, pointer/format metadata and undo history. The pre-fix regression fails on the reported feedback loop. The complete freshly built Mac suite passes 40 groups and 7,610 assertions with zero failures or skipped groups (`scripts/verify-mac.sh --no-restore`); no new build warnings were introduced.

Scope: Mac NPC preview presentation and ownership only. Existing summary/scanner limits, dialogue content, sprite decoding, compilation, pointer relocation, ROM edits and shared history algorithms are unchanged. Tests use synthetic data; no user ROM is opened or changed. Very tall summaries may display smaller text to retain the complete preview. Native trackpad confirmation remains after relaunch.

## 088 — Make image-canvas Control-click sample instead of paint

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/ImageEditorView.axaml.cs](src/HexManiac.Avalonia/Controls/ImageEditorView.axaml.cs) | 87 | Route Mac Control-left-click through the existing eyedropper and retain the physical button for a matching release. |
| [tests/ImageEditorInput/ImageEditorInput.csproj](tests/ImageEditorInput/ImageEditorInput.csproj) | 1 | Register the real-window image input regression group. |
| [tests/ImageEditorInput/SyntheticImageFixture.cs](tests/ImageEditorInput/SyntheticImageFixture.cs) | 13 | Fabricate compressed/uncompressed sprites with valid palette pointers and a real editor window. |
| [tests/ImageEditorInput/Program.cs](tests/ImageEditorInput/Program.cs) | 17 | Reproduce unintended painting and verify sampling, release, drawing, bitmap freshness and Mac history shortcuts. |
| [tests/ImageEditorInput/README.md](tests/ImageEditorInput/README.md) | 1 | Document reproduction and input/history coverage. |
| [tests/README.md](tests/README.md) | 30 | Link the image editor input regression group. |

Evidence: The input audit found that only a physical right button entered the image eyedropper. A real-window synthetic regression confirmed Control-left-click changed a sprite pixel from 1 to the selected drawing color 7. Physical secondary-click correctly sampled it.

Behavior: Mac Control-click now uses the same shared sampling path as secondary-click. Releasing Control first does not alter the gesture; only the original physical mouse button ends the captured interaction. Primary drawing and middle-button behavior retain their existing strategies.

Validation: ImageEditorInput 38, PortraitFreshness 68 and StreamTemplates 123 pass (229 assertions). Sampling preserves all ROM bytes, metadata and history; normal drawing plus actual Cmd+Z/Cmd+Shift+Z preserves exact model snapshots and refreshes bound bitmaps for compressed/uncompressed sprites. The pre-fix Control-click regression fails as described. No user ROM is used; encoding, palette values, pointer relocation and history algorithms are unchanged.

## 089 — Propagate named swatch colors into palette edits

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/Swatch.axaml.cs](src/HexManiac.Avalonia/Controls/Swatch.axaml.cs) | 40 | Decode Avalonia color names as well as existing bare/hash-prefixed theme hex before publishing swatch changes. |
| [tests/ColorEditorInput/ColorEditorInput.csproj](tests/ColorEditorInput/ColorEditorInput.csproj) | 1 | Register actual color-field input regressions with the shared synthetic image fixture. |
| [tests/ColorEditorInput/PalettePopup.cs](tests/ColorEditorInput/PalettePopup.cs) | 12 | Locate and drive the real code-created palette popup through its input root. |
| [tests/ColorEditorInput/Program.cs](tests/ColorEditorInput/Program.cs) | 16 | Reproduce failed White input and verify named/numeric colors, channel encoding, focus and unrelated data. |
| [tests/ColorEditorInput/README.md](tests/ColorEditorInput/README.md) | 1 | Document the serialization mismatch and input preservation checks. |
| [tests/README.md](tests/README.md) | 30 | Link the color-field regression group. |

Evidence: Entering 7FFF into the real palette field displayed White but did not update the ROM. Avalonia Color.ToString serializes familiar colors as names; Swatch used a shared hex-only theme decoder and returned without raising ResultChanged. The existing UI already advertised and accepted color words, making this a broken propagation path.

Behavior: The swatch uses Avalonia's color decoder for the UI strings it receives, retaining bare hex support for theme bindings. Familiar colors now follow the same existing palette write/refresh path as other colors. Invalid drafts do not publish edits.

Validation: ColorEditorInput passes 93 assertions across six 16-bit codes, eight named colors, exact raw-ROM/rendered channel ordering, unchanged unrelated bytes and metadata, retained popup/focus, all previously supported theme-hex forms and invalid input. The 7FFF regression fails before the fix. Core script numeric parsing, palette encoding/quantization, relocation and history algorithms are unchanged; no user ROM or theme settings are modified.

## 090 — Close stale palette editors after history and context changes

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/PaletteControl.axaml.cs](src/HexManiac.Avalonia/Controls/PaletteControl.axaml.cs) | 59 | Follow the current palette's content replacements and close stale popups after external refresh, with UI dispatch and source checks. |
| [src/HexManiac.Avalonia/Controls/PaletteControl.axaml.cs](src/HexManiac.Avalonia/Controls/PaletteControl.axaml.cs) | 446 | Guard own swatch edits so their synchronous refresh retains the active field and popup; release subscriptions on detach. |
| [tests/PaletteHistory/PaletteHistory.csproj](tests/PaletteHistory/PaletteHistory.csproj) | 1 | Register palette history regressions using shared image/popup fixtures. |
| [tests/PaletteHistory/Program.cs](tests/PaletteHistory/Program.cs) | 17 | Reproduce stale Undo fields and protect Redo, correction, model/context replacement and live bitmap refresh. |
| [tests/PaletteHistory/README.md](tests/PaletteHistory/README.md) | 1 | Document the stale baseline and lifecycle coverage. |
| [tests/README.md](tests/README.md) | 30 | Link the palette history regression group. |

Evidence: After a valid 1234 color edit, Undo restored the ROM but left the real open popup showing 1234. Palette refresh replaced its color objects without resetting the popup's editable values or local delta baseline. The failing-before regression exercised the actual field and shared Undo command.

Behavior: External palette replacement, including Undo/Redo, reload and context changes, closes the old editor. Reopening reads current colors. The control follows only its current palette, marshals delayed notifications and unsubscribes on detachment. Own swatch writes are guarded so their existing refresh does not dismiss the popup during typing.

Validation: PaletteHistory passes 36 assertions on compressed/uncompressed image editors; ColorEditorInput still passes all 93 focus/input checks. Coverage includes actual Cmd+Z, exact ROM/metadata and bitmap restoration, preserved redo during inspection, correction after Undo, reload, context changes, detached-source notifications and reattachment. No user ROM is used. Core palette writes, numeric/channel rules, relocation and history algorithms remain unchanged.

## 091 — Keep palette clipboard commands on their focused target

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/PaletteControl.axaml.cs](src/HexManiac.Avalonia/Controls/PaletteControl.axaml.cs) | 62 | Resolve clipboard filesystem parameters at invocation, refresh menu availability, and consume disabled palette operations without ancestor fallback. |
| [src/HexManiac.Avalonia/Controls/ImageEditorView.axaml.cs](src/HexManiac.Avalonia/Controls/ImageEditorView.axaml.cs) | 45 | Decline canvas shortcuts while fields/palettes own focus and resolve image clipboard parameters after attachment. |
| [src/HexManiac.Avalonia/Windows/MainWindow.Editing.cs](src/HexManiac.Avalonia/Windows/MainWindow.Editing.cs) | 16 | Route native Edit commands to focused palette colors while retaining text priority and image/hex fallback elsewhere. |
| [tests/PaletteClipboard/PaletteClipboard.csproj](tests/PaletteClipboard/PaletteClipboard.csproj) | 1 | Register palette clipboard regressions with shared real-window fixtures. |
| [tests/PaletteClipboard/Program.cs](tests/PaletteClipboard/Program.cs) | 14 | Reproduce null-parameter crashes and native-menu misrouting, and protect disabled operations, text priority and history. |
| [tests/PaletteClipboard/README.md](tests/PaletteClipboard/README.md) | 1 | Document routing, command initialization and preservation coverage. |
| [tests/README.md](tests/README.md) | 30 | Link the palette clipboard regression group. |

Evidence: Cmd+C in the actual palette crashed in PaletteCollection.ExecuteCopy because FileSystem was captured during control construction, before it had a TopLevel. The image clipboard bindings had the same initialization flaw. Once isolated, native Edit-menu Copy used the image/tab fallback rather than the focused palette. A disabled palette Paste also reached an ancestor image binding instead of remaining disabled.

Behavior: Palette/image clipboard bindings resolve their current filesystem at execution. Canvas keys defer to focused palette/text/combo/slider controls. Palette commands consume unavailable operations; null/invalid color text cannot become an image or binary paste. Native Edit commands share palette focus routing, including selecting palette colors; unsupported palette Cut remains a no-op. Text fields keep native text clipboard priority. Context-menu enabled state refreshes when opened.

Validation: PaletteClipboard 40, ClipboardRouting 78, ImageEditorInput 38, PaletteHistory 36 and ColorEditorInput 93 pass (285 assertions). Checks preserve existing color clipboard format and paste-selection advance, exact palette-only writes, sprite/pointer bytes, metadata and Undo/Redo, while verifying keyboard/native/context paths, invalid clipboard safety, popup text and image fallback. Core parsing/encoding and history algorithms are unchanged; no user ROM or real image clipboard is used.

## 092 — Complete Control-click routes for hex and block editing

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/HexContent.cs](src/HexManiac.Avalonia/Controls/HexContent.cs) | 372 | Route Mac Control-click to existing hex context actions, preserve the physical release and cancel abandoned capture. |
| [src/HexManiac.Avalonia/Controls/SelectedBlockEditor.axaml.cs](src/HexManiac.Avalonia/Controls/SelectedBlockEditor.axaml.cs) | 37 | Treat Control-left-click as secondary tile sampling before the primary painting path. |
| [src/HexManiac.Avalonia/Controls/TabView.axaml](src/HexManiac.Avalonia/Controls/TabView.axaml) | 90 | Wire byte-summary press/release routing. |
| [src/HexManiac.Avalonia/Controls/TabView.axaml.cs](src/HexManiac.Avalonia/Controls/TabView.axaml.cs) | 548 | Make Copy Bytes reachable through Control-click, retain early-release state and invoke the existing command with the attached filesystem. |
| [tests/MacSecondaryActions/MacSecondaryActions.csproj](tests/MacSecondaryActions/MacSecondaryActions.csproj) | 1 | Register real-view hex/summary and formatted blockmap gesture regressions. |
| [tests/MacSecondaryActions/Program.cs](tests/MacSecondaryActions/Program.cs) | 22 | Reproduce missing hex menus and block painting, and verify equivalent actions, capture cancellation, exact clipboard/data behavior and block history. |
| [tests/MacSecondaryActions/README.md](tests/MacSecondaryActions/README.md) | 1 | Document initialization, scope and native confirmation limits. |
| [tests/README.md](tests/README.md) | 30 | Link remaining secondary-action coverage. |

Evidence: Hex and byte-summary handlers checked only physical right-button releases. The block editor checked only a physical right press before its primary painting branch. After initializing a real block tile and formatted blockmap, the old Control-click route failed by retaining drawing tile 1 rather than sampling tile 0. The original hex route also failed the actual Control-click menu assertion.

Behavior: Mac Control-click now invokes the original hex actions, Copy Bytes menu and block sampling. Hex/summary gestures retain a left-button release even if Control is released first; capture loss cancels the hex gesture. Secondary sampling does not enter the painting path. Ordinary primary editing remains available.

Validation: MacSecondaryActions 31, MapSelection 394, MapEventMenus 68 and TextSelection 63 pass (556 assertions). Checks preserve identical menu labels, all model bytes/metadata, exact FF FF plus trailing-space clipboard output, normal primary block writes and complete-byte Undo/Redo. The initialized block test was also run against the original input handler and failed before restoring the fix. No user ROM is used; command semantics, encoded blocks, numeric parsing and history algorithms are unchanged.

## 093 — Invalidate NPC summaries when map data or history changes

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs](src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs) | 313 | Invalidate presentation summaries on refresh, primary-map/data changes and event visibility, and rebuild even when pointer coordinates stay in the same cell. |
| [src/HexManiac.Avalonia/Controls/MapHoverPreview.cs](src/HexManiac.Avalonia/Controls/MapHoverPreview.cs) | 41 | Drop abandoned cached content when its map revision changes. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 105 | Observe summary revisions and hide stale previews immediately. |
| [tests/MapHistoryRefresh/MapHistoryRefresh.csproj](tests/MapHistoryRefresh/MapHistoryRefresh.csproj) | 1 | Register real-map history freshness checks. |
| [tests/MapHistoryRefresh/Program.cs](tests/MapHistoryRefresh/Program.cs) | 60 | Reproduce stale dialogue after Undo and verify same-cell summaries, child editors, flags, pointers, metadata and Redo. |
| [tests/MapHistoryRefresh/README.md](tests/MapHistoryRefresh/README.md) | 1 | Document the cache contract, fixture and preservation scope. |
| [tests/README.md](tests/README.md) | 30 | Link map history freshness coverage. |

Evidence: Editing the real NPC dialogue field changed both bytes and the hover summary. Undo restored the original bytes/metadata but left the edited summary visible. Core deliberately returned null for an unchanged hover cell, while the view retained that cell's previous content; neither side represented data revision changes.

Behavior: Refresh/history, primary-map changes, map pixel/data invalidation and event visibility changes invalidate the summary cache. The view hides abandoned content immediately; the next same-cell move rebuilds it from the current model. Ordinary same-cell motion still reuses one stable input-transparent preview. This also prevents hidden events from reopening cached NPC content.

Validation: MapHistoryRefresh passes 23 assertions and MapHoverPreview retains all 134 flicker/input checks. Actual map selection and dialogue/flag/script-address fields verify complete byte/metadata restoration, current visible field values, same-cell Undo/Redo summaries, child editors, hidden-event behavior and preserved Redo. The old summary assertion fails before the fix. Only presentation cache validity changes in shared Core; ROM parsing, compilation, numeric rules, pointer writes/relocation and history algorithms remain unchanged. No user ROM is used.

## 094 — Open palette actions with Control-click

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/PaletteControl.axaml.cs](src/HexManiac.Avalonia/Controls/PaletteControl.axaml.cs) | 280 | Distinguish Mac Control-click from primary color editing, retain physical release state, suppress context-gesture dragging and open the existing palette menu. |
| [tests/MacSecondaryActions/Program.cs](tests/MacSecondaryActions/Program.cs) | 69 | Reproduce the wrong popup and verify physical/Control menu equivalence, early release, selected-color Copy and model/history preservation. |
| [tests/MacSecondaryActions/README.md](tests/MacSecondaryActions/README.md) | 3 | Document the expanded palette secondary-action coverage. |

Evidence: The physical right-click opened palette actions, but the same rendered palette received Mac Control-left-click as primary input and opened the color editor instead. The new actual-pointer assertion fails before the normalization.

Behavior: Control-click opens the palette's existing secondary menu for the clicked color. It cannot start primary color editing or reorder colors while held. The gesture retains its physical left-button release if Control is released first and cancels correctly on capture loss. Ordinary primary color editing and existing physical secondary actions remain available.

Validation: MacSecondaryActions now passes 43 assertions; PaletteHistory 36, ColorEditorInput 93 and PaletteClipboard 40 also pass (212 assertions in these four groups). Actual menu Copy resolves the clicked color without changes to ROM bytes, metadata or history. No Core color/encoding/relocation/history algorithms or user ROMs are changed.

## 095 — Record the Mac input and history audit with complete regression evidence

| File Name | Line | Summary |
| --- | --- | --- |
| [tests/MAC_INPUT_HISTORY_AUDIT.md](tests/MAC_INPUT_HISTORY_AUDIT.md) | 1 | Record failing-before evidence, seven focused fix commits, input/history coverage, preservation constraints and deliberately unclaimed native/legacy paths. |
| [tests/README.md](tests/README.md) | 16 | Link the technical audit report without replacing the planned end-user guide. |

Outcome: The first audit pass fixes image/block Control-click painting, missing hex/byte/palette secondary actions, named-color propagation, stale palette editors after history/reload, palette/image clipboard initialization and focus routing, and stale NPC summaries after map Undo/Redo. Each fix has its own commit and changelog entry (088–094). Existing NPC fields, current bitmaps, correction/retry and clipboard/secondary input are protected by real-view regressions.

Validation: `scripts/verify-mac.sh --no-restore` freshly builds and passes all 46 regression groups and 7,883 assertions, with zero failures and zero skipped groups. Six new groups contribute 273 assertions: ImageEditorInput 38, ColorEditorInput 93, PaletteHistory 36, PaletteClipboard 40, MacSecondaryActions 43 and MapHistoryRefresh 23. Existing script workflow, compilation/resource failure safety, source retention, save/reload, map selection/zoom/events, portraits, trainer streams, text/names and encounters continue to pass. The separate upstream xUnit/optional-ROM suite was not rerun and is not included in these counts.

Scope and limits: No user ROM, personal settings, emulator preference, real image clipboard or engine address is changed. Shared Core changes only presentation-cache validity for map summaries; numeric parsing, encoding, compilation, resource allocation, pointer relocation and history algorithms are unchanged. The audit explicitly leaves custom grouped-field legacy/stub paths, dormant unused controls and unrepresented hardware/native/image-mode combinations as follow-up coverage rather than claiming exhaustive parity. This is an implementation/audit report; the user guide remains separate work.

## 096 — Measure NPC hover event construction and document a targeted optimization

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/MapHoverBenchmark/MapHoverBenchmark.csproj](tools/MapHoverBenchmark/MapHoverBenchmark.csproj) | 1 | Add a standalone offscreen performance tool using the existing synthetic map fixture and production Core paths. |
| [tools/MapHoverBenchmark/Program.cs](tools/MapHoverBenchmark/Program.cs) | 1 | Measure lookup, event construction, script summaries and hover across twelve fabricated maps with sample-level timing/allocation output and preservation checks. |
| [tests/MAP_HOVER_PERFORMANCE.md](tests/MAP_HOVER_PERFORMANCE.md) | 1 | Record repeated Release measurements, a benchmark-only coordinate-first prototype and implementation requirements for events, metadata, relocation and history. |
| [tests/README.md](tests/README.md) | 19 | Link the NPC hover investigation separately from regression and native-performance claims. |

Finding: Moving to a new tile constructs every event editor before matching coordinates, including script analysis for unrelated NPCs. On a synthetic 32-NPC branching map, repeated Release processes measured empty lookup at 4.510/4.532 ms and 5,325.55 KiB per call, versus 0.044/0.044 ms and 54.42 KiB for a coordinate-first NPC-only prototype. Single-script summary cost was about 0.056 ms. The recommended first optimization is filtering current event rows before editor construction; persistent summary caching and background model access are deferred.

Validation: Both Release benchmark runs pass 120 checks each, including expected counts/branches, prototype summary parity and unchanged warm-hover bytes, metadata and clean history. Existing MapHoverPreview 134, MapHistoryRefresh 23 and MapThreadSafety 22 freshly pass (179 assertions, zero failures). The tool builds in Debug and Release; the complete Mac suite was not repeated for tooling-only changes. Eight pre-existing Core compiler warnings appear on fresh dependency builds.

Scope: Investigation/tooling only; no production optimization, game ROM, settings, compilation, numeric parsing, pointer relocation or history algorithm changes. Synthetic/offscreen timing excludes native input, rendering/layout and real graphics-table costs and is not a frame-rate promise.

## 097 — Avoid constructing unrelated map event editors during hover

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs](src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs) | 1495 | Route hover through coordinate-filtered current event groups while retaining ordinary selection and full map rendering behavior. |
| [src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs](src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs) | 2525 | Share event-group creation and existing callbacks while filtering object, warp, script and signpost rows before editor construction. |
| [src/HexManiac.Core/ViewModels/Map/IEventViewModel.cs](src/HexManiac.Core/ViewModels/Map/IEventViewModel.cs) | 114 | Filter fly spawn coordinates before editor construction without changing bank/map filtering or original flight indices. |
| [tests/MapHoverLookup/MapHoverLookup.csproj](tests/MapHoverLookup/MapHoverLookup.csproj) | 1 | Add a discovered regression group using a fabricated map with all five event kinds. |
| [tests/MapHoverLookup/Program.cs](tests/MapHoverLookup/Program.cs) | 1 | Verify matching order, borders/zoom, visibility, selected-editor preservation, constructor work and immediate position changes across Undo/Redo. |
| [tests/MapHoverLookup/README.md](tests/MapHoverLookup/README.md) | 1 | Document deterministic failing-before evidence and the division between lookup, actual-view and thread-safety coverage. |
| [tests/MAP_HOVER_PERFORMANCE.md](tests/MAP_HOVER_PERFORMANCE.md) | 1 | Record implemented production results, repeated Release evidence, preservation checks and remaining native-performance limits. |
| [tests/README.md](tests/README.md) | 19 | Link the coordinate-first hover regression alongside the performance investigation. |

Evidence: The new regression fails before the fix because an empty-tile hover constructs unrelated NPC sprite selectors. Afterward it constructs none, and a two-NPC stack does exactly twice the selector work of a single matching NPC. Filtered event types, order and row addresses match full enumeration, including stacked objects/warps/scripts/signposts/fly spawns and map-specific flight filtering.

Performance: On the same synthetic 32-NPC branching fixture, Release empty lookup falls from 4.510 ms/5,325.55 KiB to 0.065 ms/84.21 KiB, and NPC lookup from 4.528 ms/5,326.79 KiB to 0.205 ms/249.93 KiB. Full Hover for an empty→NPC pair falls from 9.155 to 0.399 ms. A second independent Release run reproduces 0.065/0.203/0.400 ms respectively, with identical allocations; each benchmark run passes 120 checks. These are synthetic CPU/allocation measurements, not native frame-rate claims.

Validation: `scripts/verify-mac.sh --no-restore` freshly builds and passes all 47 regression groups and 7,933 assertions, with zero failures and zero skipped groups. The new group contributes 50 assertions; existing actual-view hover, NPC dialogue/flag/script-pointer history, cold metadata/thread ownership, map input/rendering and script compilation/resource/source-retention/save/reload protections continue to pass. The separate upstream xUnit suite was not rerun. Build warnings remain the eight pre-existing Core warnings.

Scope: Tables are reacquired for each hover and no persistent cache or background work is introduced. Existing same-cell invalidation, selected editors, callbacks and full rendering/discovery remain intact. No user ROM, settings, numeric parsing, compilation, resource allocation, pointer relocation, history algorithm or game-engine behavior is changed. Native connected-map hover and hardware responsiveness remain manual coverage.

## 098 — Reproduce stale suggestions and refresh script responsiveness evidence

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/AutocompleteLifecycleProbe/AutocompleteLifecycleProbe.csproj](tools/AutocompleteLifecycleProbe/AutocompleteLifecycleProbe.csproj) | 1 | Add a standalone offscreen characterization tool against the production script editor and suggestion popup. |
| [tools/AutocompleteLifecycleProbe/Program.cs](tools/AutocompleteLifecycleProbe/Program.cs) | 1 | Record focus, bound-source refresh, context replacement and visibility/detachment behavior with positive controls and synthetic byte/metadata preservation. |
| [tests/SCRIPT_EDITOR_REVIEW.md](tests/SCRIPT_EDITOR_REVIEW.md) | 1 | Document concrete stale-list reproductions, current NPC edit/highlighting measurements and a focused fix/profiling order. |
| [FutureDevelopment.md](FutureDevelopment.md) | 167 | Track HMA-009 as a reproduced but unimplemented suggestion lifecycle defect with explicit native/list-type limits. |
| [tests/README.md](tests/README.md) | 22 | Link the scripting performance and stale-suggestion investigation. |

Findings: Suggestions close on ordinary focus loss but a subsequent real bound source update reopens the popup while its editor remains unfocused. Replacing the overlay's context with an empty CodeBody retains old options. Hiding its parent leaves Popup.IsOpen true and restoring it without typing retains suggestions; this is offscreen popup-state evidence, not a claim about native Cocoa visibility. The probe includes six successful focused-offer preparations and records ten lifecycle states; synthetic bytes and metadata remain unchanged. Target/editor-context replacement and detachment controls that correctly dismiss the list are reported separately rather than claimed as failures.

Performance: The current Release NPC benchmark completes 18 cases/2,892 measurement checks with all 24 completion probes matching Core. At 500 actual lines, attached-editor immediate edits take about 10 ms, deferred compilation 33–46 ms and separate presentation phases 23–40 ms; these exclude the debounce wait and are not additive native typing latency. Isolated comment-heavy highlighting measures 1.366/10.403/89.439 ms at 100/500/2,000 lines, identifying full-document presentation as a next profiling candidate rather than justifying a compiler rewrite.

Validation and scope: Existing AutocompleteMapping freshly passes 294 assertions, exposing lifecycle coverage missing from the prior acceptance/mapping checks. No production code, ROM, settings, compiler/history algorithms or normal test groups are changed; the full suite is not rerun for tooling/documentation only, and its preceding checkpoint remains 47 groups/7,933 assertions. HMA-009 remains outstanding. Native input/window sequencing, other dropdown implementations, full TabView cost attribution and real-ROM latency are unverified.

## 099 — Dismiss stale suggestions when their editor loses eligibility

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs](src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs) | 44 | Guard offering and accepting suggestions by current focus, visibility, enabled state and active window while clearing stale options across context, target and visual lifetime changes. |
| [src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs](src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs) | 114 | Track and clean up target/ancestor/window subscriptions and dismiss outside clicks without consuming or replaying the intended action. |
| [tests/AutocompleteDismissal/AutocompleteDismissal.csproj](tests/AutocompleteDismissal/AutocompleteDismissal.csproj) | 1 | Add a discovered real-control regression group using fabricated script data. |
| [tests/AutocompleteDismissal/Program.cs](tests/AutocompleteDismissal/Program.cs) | 1 | Cover 85 focus/context/visibility/detachment/deactivation and actual outside-click checks with positive opening controls and exact byte/metadata preservation. |
| [tests/AutocompleteDismissal/README.md](tests/AutocompleteDismissal/README.md) | 1 | Document failing-before evidence, one-click action delivery, existing acceptance coverage and native-event limits. |
| [tests/AutocompleteMapping/Program.cs](tests/AutocompleteMapping/Program.cs) | 41 | Focus synthetic positive controls explicitly so normal completion tests exercise eligible editor interaction. |
| [tools/ScriptEditingBenchmark/Program.cs](tools/ScriptEditingBenchmark/Program.cs) | 219 | Focus the attached completion target before probing offerings under the corrected eligibility rules. |
| [FutureDevelopment.md](FutureDevelopment.md) | 167 | Mark HMA-009 implemented and describe dismissal behavior and remaining native/non-overlay scope. |
| [tests/SCRIPT_EDITOR_REVIEW.md](tests/SCRIPT_EDITOR_REVIEW.md) | 6 | Link the implemented follow-up while retaining the original failing-before investigation evidence. |
| [tests/README.md](tests/README.md) | 22 | Link the shared suggestion dismissal regression alongside the investigation. |

Evidence: The new regression first fails when a real bound source update reopens suggestions after focus moves away. It additionally reproduces outside clicks on a non-focusable action retaining the list without the outside-dismissal observer; the fixture confirms the action is actually hit. The final implementation dismisses in both cases, and the action executes exactly once while editor focus can remain unchanged. No pointer event is handled, captured or replayed by dismissal.

Behavior: Suggestions close on focus/context/target changes, hidden or disabled hosts, detachment, window deactivation and outside pointer presses. Unfocused, hidden, detached or inactive-window updates cannot reopen them. Restoring visibility/attachment/activation alone does not restore old options; focused current input can still offer and accept fresh suggestions. Existing non-focusable suggestion buttons retain mouse press/release acceptance, and keyboard completion/navigation and script scaffolding remain intact. The fix affects the shared AutocompleteOverlay, not every native ComboBox menu.

Validation: `scripts/verify-mac.sh --no-restore` freshly builds and passes all 48 groups and 8,018 assertions, with zero failures and zero skipped groups. AutocompleteDismissal contributes 85 checks; AutocompleteMapping retains 294, including actual mouse acceptance and live compilation/history round-trips, and WildEncounterEditing retains 128. Compilation/resource atomicity, rejected source/diagnostics, correction/retry, clipboard, save/reload, HMA-006/007 protections and Undo/Redo continue to pass. The existing script benchmark compatibility run passes 2,892 measurement checks with all 24 focused completion probes matching Core. The separate upstream xUnit suite was not rerun; fresh dependency builds retain eight pre-existing Core warnings.

Scope: No Core, compiler, numeric parsing, allocation/relocation, ROM, settings or history algorithm is changed. Window deactivation is exercised through Avalonia's callback in the headless regression; native OS delivery, the user's exact original trigger and unrelated dropdown implementations remain manual coverage. The changelog records the completed fix separately from the investigation.

## 100 — Advance comment searches without rescanning cleared script prefixes

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/TextEditorViewModel.cs](src/HexManiac.Core/ViewModels/TextEditorViewModel.cs) | 134 | Resume ordinary comment searches after the previous match while preserving phase order and legacy rescanning for markers containing spaces. |
| [tests/CommentHighlighting/CommentHighlighting.csproj](tests/CommentHighlighting/CommentHighlighting.csproj) | 1 | Add a discovered Core-only highlighting safety group with a frozen preformatter baseline. |
| [tests/CommentHighlighting/Program.cs](tests/CommentHighlighting/Program.cs) | 1 | Compare all six layers and source indices with an independent legacy oracle over 2,320 configurations plus retained preformatter diagnostics. |
| [tests/CommentHighlighting/PreformatterBaseline.json](tests/CommentHighlighting/PreformatterBaseline.json) | 1 | Preserve six complete preformatter layer snapshots captured before optimization at 769825a9. |
| [tests/CommentHighlighting/README.md](tests/CommentHighlighting/README.md) | 1 | Document equivalence coverage, unusual delimiter compatibility and before-change golden fixture provenance. |
| [tools/CommentScanBenchmark/CommentScanBenchmark.csproj](tools/CommentScanBenchmark/CommentScanBenchmark.csproj) | 1 | Add an isolated scan benchmark against the production mutable-string helpers. |
| [tools/CommentScanBenchmark/Program.cs](tools/CommentScanBenchmark/Program.cs) | 1 | Attribute restart-at-zero versus cursor scan cost independently of tokenization, compilation and layout while checking exact output pairs. |
| [tests/COMMENT_HIGHLIGHT_PERFORMANCE.md](tests/COMMENT_HIGHLIGHT_PERFORMANCE.md) | 1 | Record isolated and full-highlighter timing, six-layer hash equality, preservation constraints and complete verification. |
| [tests/SCRIPT_PERFORMANCE.md](tests/SCRIPT_PERFORMANCE.md) | 3 | Link the latest comment-search checkpoint without replacing older performance evidence. |
| [tests/README.md](tests/README.md) | 25 | Link the new performance report and equivalence group. |

Evidence: Repeated comment searches restart at zero over progressively cleared text. Isolated 2,000-line scans measured 45.259→0.217 ms for line comments and 52.514→0.155 ms for blocks, with identical basic/comment outputs. Space-containing openers can appear earlier after blanking, so they deliberately keep the original rescanning semantics instead of receiving an unsafe shortcut.

Performance: The unchanged Release full-highlighter benchmark measures 1.341→0.660 ms at 100 lines, 10.468→2.505 ms at 500 and 90.044→7.785 ms at 2,000, with all six layer hashes matching every size. Temporary allocation is essentially unchanged; the 2,000-line run differs by 88 bytes around 3.33 MB. These are Core-only synthetic measurements, not native typing latency or compiler speedups.

Validation: The new CommentHighlighting group passes 4,652 assertions. `scripts/verify-mac.sh --no-restore` freshly builds and passes all 49 groups and 12,670 assertions, with zero failures and zero skipped groups. Shared script compilation/resource atomicity, source retention, numeric/name interpretation, save/reload, autocomplete, clipboard and history protections continue to pass. The separate upstream xUnit suite was not rerun; fresh builds retain eight pre-existing Core warnings.

Scope: Only presentation comment-search positions change. Tokenization, numeric parsing, compiler/resource allocation/relocation, game-engine behavior, ROMs, metadata, history and caret algorithms are unchanged. The subsequent document-width/layout investigation is separate.

## 101 — Attribute script width/layout cost and repeated queued measurements

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/ScriptWidthBenchmark/ScriptWidthBenchmark.csproj](tools/ScriptWidthBenchmark/ScriptWidthBenchmark.csproj) | 1 | Add a standalone headless production-control investigation without application instrumentation. |
| [tools/ScriptWidthBenchmark/Program.cs](tools/ScriptWidthBenchmark/Program.cs) | 1 | Time the actual document-width handler, compare same-size omission controls and record burst-source identity, rendered layers, geometry and diagnostics-only behavior. |
| [tools/ScriptWidthBenchmark/README.md](tools/ScriptWidthBenchmark/README.md) | 1 | Document reproduction, exact-handler attribution, fixed-width prototype limits and the need to update instrumentation when queued work is introduced. |
| [tests/SCRIPT_WIDTH_LAYOUT_INVESTIGATION.md](tests/SCRIPT_WIDTH_LAYOUT_INVESTIGATION.md) | 1 | Record repeated measurements and a safe coalescing/regression plan while leaving production layout unchanged. |
| [tests/README.md](tests/README.md) | 27 | Link the width/layout investigation separately from the completed highlighting optimization. |

Findings: At application checkpoint 26ec1e5c, the complete width handler takes about 3.34 ms/1.34 MB for 500 short lines, 21.96 ms/6.53 MB for 2,000 and 15.64 ms/4.94 MB for 500 Unicode dialogue lines in the second Release run. It is nested within queued layout/render work, not an additive extra phase or the whole remaining editor cost. Same-size omission controls retain final source/layer hashes and measured widths/corner geometry, but do not establish safety for growing/shrinking documents.

Confirmed redundancy: Ten source updates queued before UI draining invoke ten width calculations, all reading the same final source in every production arm. The 500/2,000-short-line bursts spend about 45/178 ms in those width handlers; diagnostics-only bursts invoke none. Coalescing actual measurements to one current-state UI request is the recommended next implementation. Isolated settled edits already measure once, so the proposal targets bursts rather than promising removal of ordinary per-edit shaping cost.

Validation and scope: Two independent Release runs each pass 315 source/rendered-layer/handler/diagnostic invariants; all five endpoint geometry comparisons agree, and the second run verifies one distinct measured source across every ten-call production burst. No production width/layout, compiler, settings, ROM or history code changes. The full suite was not repeated for this tooling-only step; the preceding optimization freshly passed 49 groups/12,670 assertions, zero failures/skips. Full TabView/compiler/native input costs and dynamic-size behavior remain outside the probe. The plan requires caret, scrolling, fonts/Unicode, dimensions, context/lifetime and recent suggestion protections before any implementation.

## 102 — Coalesce script document-width calculations while preserving scroll geometry

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs](src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs) | 111 | Queue one current-state width calculation per UI burst and invalidate old work across context, template and attachment changes. |
| [src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs](src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs) | 150 | Retain the existing measurement/corner routine and preserve the precedence of native scroll-extent widths after deferred requests. |
| [tests/ScriptWidthUpdates/ScriptWidthUpdates.csproj](tests/ScriptWidthUpdates/ScriptWidthUpdates.csproj) | 1 | Add a discovered headless production-control regression group. |
| [tests/ScriptWidthUpdates/Program.cs](tests/ScriptWidthUpdates/Program.cs) | 1 | Count actual measurements and compare text, layer widths/transforms, native extents, caret/selection, corner sizing and lifetime behavior with an immediate-scheduling reference. |
| [tests/ScriptWidthUpdates/README.md](tests/ScriptWidthUpdates/README.md) | 1 | Document 448 preservation checks and the deterministic ten-to-one failing-before regression. |
| [tools/ScriptWidthBenchmark/Program.cs](tools/ScriptWidthBenchmark/Program.cs) | 18 | Observe actual calculation calls instead of queue notifications and record seven repeated bursts with output/geometry evidence. |
| [tools/ScriptWidthBenchmark/README.md](tools/ScriptWidthBenchmark/README.md) | 1 | Document the paired before/after protocol and replace the obsolete reflection/omission procedure with actual-measurement observation. |
| [tests/SCRIPT_WIDTH_COALESCING.md](tests/SCRIPT_WIDTH_COALESCING.md) | 1 | Record the implemented change, paired timing/allocation results, repeated after measurements and full verification. |
| [tests/SCRIPT_WIDTH_LAYOUT_INVESTIGATION.md](tests/SCRIPT_WIDTH_LAYOUT_INVESTIGATION.md) | 6 | Link the implementation while retaining the original investigation baseline. |
| [tests/README.md](tests/README.md) | 29 | Link implemented width coalescing and its regression group. |

Evidence: The first new regression fails before optimization because ten queued edits perform ten real width calculations. They now perform one calculation of the latest text; diagnostics-only updates perform none. Differential geometry checks caught deferred measurement overwriting a later native extent width, so the implementation tracks scroll ordering and preserves the immediate path's final dimensions. Old generations cannot paint abandoned context/template geometry, and reattachment measures current source.

Benchmark: The same actual-measurement observer runs before queueing and in two independent after Release processes. For ten-update bursts, 500-short-line width work falls from 39.598 ms to 3.340/3.296 ms, 2,000 lines from 192.364 to 25.401/28.525 ms and Unicode dialogue from 154.286 to 15.949/12.440 ms. Actual calls fall from ten to one in every workload; all six layer hashes, widths and corner dimensions match the baseline. Width allocations fall from 17.01→1.74 MB, 68.97→6.94 MB and 54.90→4.95 MB respectively. Second-run queued drain times improve from 69.853→28.559, 329.930→146.493 and 209.278→73.393 ms; width time is nested within these drain figures and must not be added to them. Settled single edits still calculate once, without a general typing-latency claim.

Validation: All three benchmark reports pass 190 invariants. The new ScriptWidthUpdates group passes 448 assertions covering burst counts, latest source, growth/shrink/empty text, Unicode/emoji/CJK, line endings/tabs/trailing spaces, long lines, both scrollbars, resize, fonts/culture, interleaved scroll/source changes, queued context/template replacement, detachment and diagnostics. `scripts/verify-mac.sh --no-restore` freshly builds and passes all 50 groups and 13,118 assertions, zero failures and zero skipped groups, including live script input/autocomplete, resource atomicity, retained drafts/diagnostics, save/reload, clipboard, HMA-006/007 and Undo/Redo protections. The separate upstream xUnit suite was not rerun; fresh builds retain eight existing Core warnings.

Scope: Presentation work only, using the same FormattedText and corner-sizing rules on the UI thread; no character-width approximation, background work or extra compile debounce. Core/source editing, numeric/name interpretation, compilation, allocation/relocation, metadata, history algorithms, game-engine behavior, settings and user ROMs are unchanged. Measurements are synthetic/offscreen, not native keyboard/compositor latency.

## 103 — Hide optional Claude and built-in emulator controls from the normal Mac UI

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/OptionalFeatureVisibility.cs](src/HexManiac.Avalonia/OptionalFeatureVisibility.cs) | 5 | Centralize two off-by-default UI switches and null optional native-menu gestures while retaining compiled implementations. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml](src/HexManiac.Avalonia/Windows/MainWindow.axaml) | 75 | Hide and disable built-in emulator and Claude/chat/MCP toolbar/menu controls, including the optional menu separator, while retaining external emulator access. |
| [src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs](src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs) | 136 | Skip the F6 binding while the built-in emulator UI is hidden without deleting its launch implementation. |
| [tests/TabMenus/Program.cs](tests/TabMenus/Program.cs) | 38 | Verify compiled toolbar/native-menu states, absent optional accelerators, inert F6 and retained external emulator menu/button/F5 access. |
| [tests/TabMenus/README.md](tests/TabMenus/README.md) | 8 | Document the optional-control checks and their restoration switches. |

Behavior: The Claude chat button, built-in emulator button, their native menu entries and Claude MCP/setup menu entries are hidden and disabled. Optional native accelerators are absent and F6 has no window binding; Command-Shift-C is no longer advertised by hidden chat. The external emulator play button, native menu and F5 remain available. Flipping the two centralized UI switches restores the retained controls and shortcuts.

Validation: Five freshly built targeted groups pass 204 assertions with zero failures: TabMenus 33, ExternalEmulatorLaunch 24, ClipboardRouting 78, WelcomeScreen 27 and StartupUpdates 42. Checks inspect actual MainWindow controls/native menu items and press F6 without opening an emulator; external launch tests use an injected process starter. The complete Mac suite is not repeated for this UI-only change; its preceding baseline remains 50 groups/13,118 assertions. Fresh dependency builds retain eight existing Core warnings.

Scope: Claude/chat/MCP and embedded emulator implementations are left intact, including existing explicit programmatic/CLI entry points; this is UI availability, not backend removal. Verification launches no emulator core or assistant service and modifies no user ROM or personal settings. External mGBA preferences/launch behavior, Core, scripting, compilation, allocation/relocation and history remain unchanged.

## 104 — Verify mixing-bag controls, correct Mac help and record fill permission defect

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 34 | Supply platform-correct mixing-bag shortcut help for the existing Command-on-Mac/Control-elsewhere input mapping. |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml](src/HexManiac.Avalonia/Controls/MapTab.axaml) | 139 | Replace misleading literal Ctrl-click help with the actual modifier, bounded wrapping and explicit bag-member brush instructions. |
| [tools/BlockBagReview/BlockBagReview.csproj](tools/BlockBagReview/BlockBagReview.csproj) | 1 | Add standalone real-control review tooling using the existing fabricated map fixture. |
| [tools/BlockBagReview/Program.cs](tools/BlockBagReview/Program.cs) | 1 | Verify scaled pointer bag controls, selected brush, single-cell permission/history preservation and separately characterize preserve-mode Core fill. |
| [tools/BlockBagReview/README.md](tools/BlockBagReview/README.md) | 1 | Explain the distinction between passing positive controls and the recorded failed fill-preservation characterization. |
| [tests/BLOCK_BAG_REVIEW.md](tests/BLOCK_BAG_REVIEW.md) | 1 | Document confirmed Mac usage, scope, reproducible fill bytes and prioritized follow-up areas. |
| [FutureDevelopment.md](FutureDevelopment.md) | 187 | Track HMA-010 as a reproduced but unimplemented preserve-permission mixing-bag flood-fill defect. |
| [tests/README.md](tests/README.md) | 31 | Link the mixing-bag control/help review and outstanding fill finding. |

Finding and help correction: The feature was already ported: Command-click in the Mac Blocks picker toggles bag members and Command-click on the bag clears it. Literal Windows Ctrl text made it look unavailable. The tooltip now states the real modifier and explains that selecting a bag member activates random drawing; raw 8×8 tile editing is a separate picker. No shortcut or random-selection algorithm changes.

Verification: The review passes 16 positive assertions, including actual Command/Control pointer contrasts, add/remove/clear, unchanged bytes/metadata/history from brush state, deterministic one-member stamping with movement bits preserved and exact Undo/Redo. The existing MapSelection group freshly builds and passes 394 assertions after the tooltip change. Full Mac/upstream suites were not repeated for tooltip/investigation-only work; fresh dependency builds retain eight existing Core warnings. Verification uses fabricated ROM data, launches no emulator and modifies no personal settings.

Separate outstanding defect: With CollisionIndex=-1 and a one-member bag, a valid first stamp writes 4000→4001, but Core double-click fill changes neighboring matching cells to FC01. PaintBlockBag packs the negative preservation sentinel into movement bits. The review explicitly prints FLOOD_FILL_PRESERVES_MOVEMENT:False and 4001,FC01,FC01; this failed characterization is not included as a passing preservation assertion. HMA-010 remains unimplemented, with explicit-permission, region/boundary, multi-member and exact history regressions recommended before changing map writes.

Scope: Only user-facing bag help changes in the application. Core map writing, collision/elevation, randomness, history, scripting/compilation and game-engine behavior remain unchanged. Follow-up priorities are the confirmed fill defect, Mac shortcut/help consistency, combined map-tool/history gestures and attributed full script-tab rendering work.

## 105 — Preserve movement permissions during mixing-bag flood fill (HMA-010)

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs](src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs) | 1319 | Resolve negative bag-fill permission selection from the original region, matching ordinary PaintBlock. |
| [tests/BlockBagFill/BlockBagFill.csproj](tests/BlockBagFill/BlockBagFill.csproj) | 1 | Add the synthetic fill group to automatic Mac regression discovery. |
| [tests/BlockBagFill/Program.cs](tests/BlockBagFill/Program.cs) | 1 | Protect permissions, random member validity, full-word region boundaries, exact bytes/metadata and Save/Undo/Redo/reload. |
| [tests/BlockBagFill/README.md](tests/BlockBagFill/README.md) | 1 | Describe the 80-case matrix, original failure and synthetic/in-memory scope. |
| [tools/BlockBagReview/Program.cs](tools/BlockBagReview/Program.cs) | 109 | Require the formerly failed fill-preservation characterization to pass. |
| [tools/BlockBagReview/README.md](tools/BlockBagReview/README.md) | 5 | Document the now-enforced preservation check. |
| [tests/BLOCK_BAG_REVIEW.md](tests/BLOCK_BAG_REVIEW.md) | 41 | Retain historical reproduction and record the subsequent fix and regression coverage. |
| [FutureDevelopment.md](FutureDevelopment.md) | 189 | Mark HMA-010 fixed and link the regression evidence. |
| [tests/README.md](tests/README.md) | 33 | Link the implemented fill regression group. |

Behavior: With a one-member bag and preserved movement selection, connected cells containing 4000 now become 4001 rather than FC01. The fill reads movement bits from the original full-word region value, using the same fallback as ordinary PaintBlock. Explicit permission selection, exact region matching, random selection, first-stamp behavior and history algorithms are unchanged.

Evidence: The regression was run before changing Core and failed on a neighboring preserve-mode cell with actual FC01. The fixed matrix passes 2,081 assertions over original movement values 0/1/16/63, preserve and explicit 0/1/16/62 settings, one/two-member bags, connected/disconnected regions, differing permissions, all map edges and scaled/translated coordinates. Checks compare every unrelated byte and all metadata, save/reload in memory at edited/undone/redone states, and require Redo to restore the exact random result. Empty-bag fill remains inert.

Validation: `scripts/verify-mac.sh --no-restore` freshly builds and passes all 51 groups and 15,209 assertions, with zero failures and zero skipped groups. This includes the 2,081 new fill assertions plus existing map and scripting protections. The separately rebuilt real-control BlockBagReview passes 17 checks and prints FLOOD_FILL_PRESERVES_MOVEMENT:True with 4001,4001,4001. The upstream xUnit suite was not rerun; fresh dependency builds retain eight existing Core warnings.

Scope: One Core fallback line; no changes to numerical parsing, script compilation, allocation, pointer relocation, metadata/history algorithms, bag shortcuts or user ROMs. Synthetic fixtures only; Save is captured in memory and independently reloaded. Native double-click timing remains outside the headless check; the standalone pointer review invokes explicit Core press kinds for its final fill assertion.

## 106 — Audit map-tool transitions, capture loss and history without changing application behavior

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/MapTransitionReview/MapTransitionReview.csproj](tools/MapTransitionReview/MapTransitionReview.csproj) | 1 | Add standalone synthetic MainWindow/MapTab audit tooling outside automatic regression discovery. |
| [tools/MapTransitionReview/Program.cs](tools/MapTransitionReview/Program.cs) | 62 | Run isolated single-click, capture-loss, focus, tab/map, Escape and completed-gesture probes, separating observations from positive setup controls. |
| [tools/MapTransitionReview/README.md](tools/MapTransitionReview/README.md) | 1 | Provide reproduction commands and explain that completed audit processes can contain recorded failed invariants. |
| [tests/MAP_TRANSITION_AUDIT.md](tests/MAP_TRANSITION_AUDIT.md) | 1 | Record exact outcomes, causes, prioritized implementation/regression plans and native/combined-tool limits. |
| [FutureDevelopment.md](FutureDevelopment.md) | 203 | Track HMA-011 through HMA-014 as reproduced but unimplemented map transition findings. |
| [tests/README.md](tests/README.md) | 34 | Link the audit separately from passing implementation regressions. |

Repository: Before investigating, push completed branch codex/mac-input-history-audit through 2be6d838 to GitHub and establish its upstream. No merge or branch switch. The new audit changes are a separate local commit.

Findings: Same-cell painting after Undo and same-cell permission editing after Redo can be skipped by persistent coordinate suppression. Explicit map/background/picker capture loss leaves stale gestures, allowing button-free painting/panning or incorrect selection and swallowed clicks. A tab interruption can merge independent strokes into one Undo step. Escape clears brush fields without ending the active drawing path, permitting further map writes. Space reveal can remain active after focus/tab changes. No fixes are included; HMA-011/012/013/014 record the separate causes and required preservation checks.

Validation: Fourteen independent real-control synthetic scenarios complete with 69 positive setup/preservation controls and 25 observations: 11 passing and 14 failing. The failed observations are recorded findings, not counted as passing assertions or added to the normal runner. Capture owner, real release, independent click count and focus changes are explicitly verified. Six freshly built existing groups pass 2,763 assertions, zero failures/skips: MapSelection 394, MapHistoryRefresh 23, MapHoverPreview 134, MapEventEditing 63, MapEventMenus 68 and BlockBagFill 2,081. The complete Mac suite was not rerun for investigation-only changes; its immediately preceding verified baseline remains 51 groups/15,209 assertions at 2be6d838. The separate upstream suite was not rerun; fresh dependencies retain eight existing Core warnings.

Scope: New tooling and documentation only. Production source, existing tests, ROM fixtures and personal settings remain unchanged. No user ROM or emulator is used. Framework capture loss is explicitly induced, not a native hardware reproduction. Alias navigation passes its three gesture/history observations but changes the metadata snapshot; the audit deliberately claims neither metadata preservation nor damage for that scenario. Independent ROM/map isolation, event creation/drag interruption, border/connection/9-grid/wave-function transitions and native deactivation remain coverage gaps.

## 107 — Finish interrupted map gestures without ghost edits or merged Undo steps (HMA-012)

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 189 | Track pointer ownership and last valid coordinates; finish map/background/picker/border/connection gestures on capture loss, detach or old-context replacement. |
| [src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs](src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs) | 817 | Finish already-applied Core edits idempotently and signal interruption before explicit map navigation, without placing pending events or launching wave fill. |
| [tests/MapGestureLifecycle/MapGestureLifecycle.csproj](tests/MapGestureLifecycle/MapGestureLifecycle.csproj) | 1 | Add automatically discovered isolated real-control lifecycle regressions using fabricated map data. |
| [tests/MapGestureLifecycle/Program.cs](tests/MapGestureLifecycle/Program.cs) | 20 | Enforce capture-loss, tab/map/context, history/save, release endpoint, random bag, event and pending-template invariants. |
| [tests/MapGestureLifecycle/README.md](tests/MapGestureLifecycle/README.md) | 1 | Document child-process isolation, synthetic scope and native/combined-tool limits. |
| [tests/MAP_TRANSITION_AUDIT.md](tests/MAP_TRANSITION_AUDIT.md) | 125 | Retain historical failures and link implemented lifecycle protection. |
| [FutureDevelopment.md](FutureDevelopment.md) | 213 | Mark HMA-012 fixed; separately record HMA-015 preserve-mode non-bag rectangle packing found while constructing a regression. |
| [tests/README.md](tests/README.md) | 34 | Link the enforced lifecycle group separately from original audit findings. |

Behavior: Losing capture or leaving the map tab/context ends the owning gesture, freezes valid applied edits and completes their Undo boundary once. Button-free movement no longer continues painting/panning, picker selection does not become stranded, and a later stroke has its own history. Explicit navigation interrupts before changing maps. Pending event/wave placement is abandoned without starting work or opening a template popup. Normal release is disarmed before dropping capture, retains its actual endpoint and identifies the released physical button rather than incorrectly ending the stroke when another button is released.

Evidence and validation: The enforced ghost-paint regression fails before implementation on a byte change after capture loss. Seventeen isolated scenarios now pass 148 assertions, including actual capture owners, early modifier release, normal release without a final Move, unrelated-button release, connected rectangle edits with explicit permissions, multi-member bag results, pending object/wave templates, Script Event dragging, tab interruption, alias navigation and independent-document context replacement. Save captures synthetic bytes/metadata in memory, reloads an independent model and checks exact saved-state transitions through two separate Undo/Redo entries; random Redo restores the exact original outcome. Three freshly built existing groups also pass: MapSelection 394, MapEventMenus 68 and MapArrowIcons 98. Total targeted validation: four groups / 708 assertions, zero failures/skips. Complete-suite verification follows the separate Escape fix; the upstream suite is not rerun. Fresh dependency builds retain eight existing Core warnings.

Scope: Input lifecycle and completion boundaries only; existing numeric parsing, script compilation, relocation, history algorithms and map painting/fill rules are unchanged. No user ROM/settings or emulator is used. Native OS deactivation, physical trackpad delivery and border/connection resizing are not directly exercised by the new group. HMA-011 same-cell suppression, HMA-013 Escape and HMA-014 reveal state remain outstanding in this commit. The separate HMA-015 rectangle preserve-mode packing finding remains unimplemented; the rectangle lifecycle test uses explicit permission 0 to isolate completion behavior.

## 108 — End the active gesture before Escape clears map brush state (HMA-013)

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs](src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs) | 1712 | Route Cancel through interaction interruption before clearing the brush, completing Core/UI state and valid edit history first. |
| [tests/MapGestureLifecycle/Program.cs](tests/MapGestureLifecycle/Program.cs) | 20 | Add nine enforced Escape/direct-API scenarios across drawing, rectangles, events, picker, pan, selection and pending object/wave templates. |
| [tests/MapGestureLifecycle/README.md](tests/MapGestureLifecycle/README.md) | 23 | Document capture release, retained applied edits, exact Undo/Redo and independent next-stroke requirements. |
| [FutureDevelopment.md](FutureDevelopment.md) | 225 | Mark HMA-013 fixed while preserving the separate remaining HMA-011/014/015 findings. |
| [tests/MAP_TRANSITION_AUDIT.md](tests/MAP_TRANSITION_AUDIT.md) | 133 | Link the subsequent Escape implementation without rewriting original failed evidence. |
| [tests/README.md](tests/README.md) | 34 | Identify both implemented lifecycle and Escape protection. |

Behavior: Escape and direct MapEditorViewModel.Cancel now finish the owning interaction before clearing selection/brush fields. The existing HMA-012 notification releases the actual captured map/picker/background gesture, prevents pending placement or wave work, and closes valid applied edits as one Undo entry. Continued held movement/release cannot re-enter the no-brush swap path, paint, move events or restore an old rectangle backup. Already-applied edits are retained, not automatically rolled back.

Evidence: Before this change, the enforced Escape regression fails because the pointer remains captured after brush clearing. Nine new scenarios pass after the change, totaling 125 additional assertions. They verify actual keyboard Escape routing and direct API cancellation, pointer release, brush clearing, unchanged bytes after held movement/release, no pending template popup, no empty Undo entries for non-writing operations, exact Undo/Redo for valid edits and an independent following stroke/rectangle. The first implementation remains its own preceding commit.

Validation: `scripts/verify-mac.sh --no-restore` freshly builds and passes all 52 regression groups / 15,482 assertions, zero failures and zero skipped groups. The expanded MapGestureLifecycle group contributes 273 assertions across 26 isolated scenarios. Existing bag-fill permissions, map selection/menus, clipboard/field routing, script compilation/resource atomicity, retained drafts and HMA-006/007/history protections remain passing. The separate upstream xUnit suite was not rerun; fresh dependency builds retain eight existing Core warnings.

Scope: One production call added to Cancel, using the shared lifecycle implementation from entry 107. No changes to numerical parsing, script compilation, allocation/relocation, map writing rules or history algorithms. Synthetic fixtures only; no personal ROM/settings or emulator is used. HMA-011 same-cell-after-history, HMA-014 Space reveal and HMA-015 non-bag rectangle preserve packing remain separate outstanding work.
