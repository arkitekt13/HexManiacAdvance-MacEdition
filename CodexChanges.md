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
