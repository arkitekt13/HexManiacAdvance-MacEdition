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
