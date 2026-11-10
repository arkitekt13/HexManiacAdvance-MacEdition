# Codex Changes

This log records changes we make in this fork, starting with our local work; it does not repeat changes inherited from the original projects.

Utilities entries 215-227 describe work on the unmerged `codex/ikarus-visual-upgrade` preview branch; publishing this log and feature overview does not merge that application code.

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

## 109 — Preserve destination movement permissions for single-block rectangles (HMA-015)

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs](src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs) | 1068 | Route single-cell rectangle brushes through existing RepeatBlock with separate art/permission values, preserving destination movement bits in preserve mode. |
| [tests/MapGestureLifecycle/Program.cs](tests/MapGestureLifecycle/Program.cs) | 20 | Add an enforced mixed-permission rectangle matrix plus growth/shrink, copied-pattern, permission-only, Save/reload and exact history controls. |
| [tests/MapGestureLifecycle/README.md](tests/MapGestureLifecycle/README.md) | 42 | Describe the rectangle permission matrix and retained copied-source semantics. |
| [FutureDevelopment.md](FutureDevelopment.md) | 246 | Mark HMA-015 implemented and link regression evidence. |
| [tests/MAP_TRANSITION_AUDIT.md](tests/MAP_TRANSITION_AUDIT.md) | 150 | Retain the original failure and record its implemented follow-up. |

Behavior: A non-bag single-block rectangle with preserve permissions now changes only block art, retaining each destination cell's collision/elevation bits. Explicit permissions still replace those bits. One/two-member bag rectangles retain the same RepeatBlock path, while copied multi-cell patterns retain their exact packed-source repetition semantics. Permission-only rectangle edits retain block IDs.

Evidence: Before the fix, the enforced preserve-mode non-bag case fails at cell 6,6 with actual FC01. The new rectangle scenario passes 288 assertions across 12 preserve/explicit permission and bag configurations, mixed original permissions 0/1/16/63, grow/shrink restoration, valid art membership, unrelated byte/pointer preservation, exact random Undo/Redo, Save/reload metadata and copied/permission-only controls. Freshly built MapSelection and BlockBagFill also pass 394 and 2,081 assertions respectively, zero failures/skips. Complete-suite validation follows the subsequent independent history/reveal fixes.

Scope: Reuse existing fill rules rather than packing a negative permission sentinel into ROM data. No changes to numeric parsing, compilation, pointer relocation, history algorithms, user ROMs or settings. Tests use fabricated data and in-memory Save; native hardware behavior is not measured.

## 110 — Scope duplicate map-position suppression to a fresh stroke (HMA-011)

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs](src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs) | 950 | Initialize cached coordinates as invalid and expose a coordinate-only BeginDrawStroke reset, retaining the original-region value used by fill. |
| [src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs](src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs) | 873 | Reset coordinate suppression across visible maps before a new non-double stroke, leaving same-stroke suppression intact. |
| [tests/MapGestureLifecycle/Program.cs](tests/MapGestureLifecycle/Program.cs) | 322 | Enforce same-cell repaint after Undo, permission changes after Redo, art replacement, saved-state boundaries, random suppression and the initial top-left edit. |
| [tests/MapGestureLifecycle/README.md](tests/MapGestureLifecycle/README.md) | 50 | Document fresh-stroke and original-fill-value guarantees. |
| [FutureDevelopment.md](FutureDevelopment.md) | 203 | Mark HMA-011 fixed with regression evidence. |
| [tests/MAP_TRANSITION_AUDIT.md](tests/MAP_TRANSITION_AUDIT.md) | 159 | Record the implemented follow-up while retaining original failed observations. |

Behavior: A new stroke at the previously edited cell now applies after Undo/Redo and after changing art or movement permissions. Coordinates start invalid so a direct initial draw at (0,0) also works. Repeated movement at the same location within one stroke remains suppressed; lastDrawVal is retained so a following ordinary/bag double-click fill still matches the original packed region.

Evidence and validation: Before implementation, the new same-cell scenario fails when the post-Undo repaint remains unchanged. It now passes 23 assertions covering exact bytes/metadata, independent history, saved-state transitions, permission/art replacement, same-position random suppression and exact random Redo. The separately rebuilt BlockBagReview passes 17 controls and prints True / 4001,4001,4001; BlockBagFill freshly builds and passes 2,081 assertions. Complete-suite validation follows the remaining independent reveal-state fix.

Scope: Coordinate-cache lifetime only. Numeric parsing, fill-region matching, compilation, pointer relocation, history algorithms and random choice remain unchanged. Verification uses fabricated data and in-memory Save, with no personal ROM/settings or emulator.

## 111 — Clear transient map reveal state when keyboard ownership changes (HMA-014)

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Avalonia/Controls/MapTab.axaml.cs](src/HexManiac.Avalonia/Controls/MapTab.axaml.cs) | 51 | Pair window-deactivation subscriptions with visual attachment, clear transient reveal/Command state across focus/context/lifetime/navigation boundaries and align the reveal transition to the new context. |
| [tests/MapGestureLifecycle/Program.cs](tests/MapGestureLifecycle/Program.cs) | 352 | Enforce focus, tab, independent-context, navigation and normal-release reveal ownership while preserving native text Space and Command event hiding. |
| [tests/MapGestureLifecycle/README.md](tests/MapGestureLifecycle/README.md) | 57 | Document reveal ownership checks and explicit native activation/compositor limits. |
| [FutureDevelopment.md](FutureDevelopment.md) | 240 | Mark HMA-014 fixed and record supported lifecycle boundaries. |
| [tests/MAP_TRANSITION_AUDIT.md](tests/MAP_TRANSITION_AUDIT.md) | 166 | Record the implemented reveal follow-up without rewriting original audit failures. |
| [tests/README.md](tests/README.md) | 34 | Describe the original map findings and link all implemented lifecycle/history/permission/reveal follow-ups. |

Behavior: Space reveal no longer remains active when focus moves to a field, another tab/context or a replacement map before key release. Lost focus, detachment, context replacement, explicit navigation/cancellation and window deactivation clear transient reveal/Command state. The window subscription is removed on detach. Old-context property notifications cannot animate the replacement context, and its reveal transition is explicitly aligned. Space in a focused text field remains native text input; Command event hiding/release still works.

Evidence: Before implementation, the enforced focus regression fails immediately after the toolbar field takes focus because map reveal remains true. Five headless scenarios now pass 61 assertions, covering actual focus movement, tab detach/return, independent-document context replacement, alias navigation, normal release/reuse, native Space text input, Command state and unchanged ROM bytes/metadata. Alias navigation deliberately excludes equality to pre-navigation metadata, as documented by the original audit.

Validation: `scripts/verify-mac.sh --no-restore` freshly builds and passes all 52 regression groups / 15,854 assertions, zero failures and zero skipped groups. MapGestureLifecycle now passes 645 assertions across 33 isolated scenarios; the three independent follow-ups add 372 assertions to its prior baseline. Existing map selection, bag fill, interrupted gestures/Escape, native clipboard/field routing, script compilation/resource atomicity, retained drafts, HMA-006/007 and Undo/Redo protections remain passing. The separate upstream suite was not rerun; fresh dependency builds retain eight existing Core warnings.

Scope: Transient Mac UI keyboard state only. No changes to parsing, compilation, allocation/relocation, map writing/history algorithms or user ROM/settings. Native macOS activation delivery and reveal-mask/compositor animation timing are not verified by these headless scenarios; window-deactivation cleanup is wired through the normal WindowBase event with paired subscription lifetime. The separate upstream xUnit suite is not rerun.

## 112 — Investigate copied-brush permissions, nine-grid overrides and border bounds

| File Name | Line | Summary |
| --- | --- | --- |
| [tools/CopiedBrushReview/CopiedBrushReview.csproj](tools/CopiedBrushReview/CopiedBrushReview.csproj) | 1 | Add standalone synthetic MainWindow/MapTab investigation tooling outside normal regression discovery. |
| [tools/CopiedBrushReview/Program.cs](tools/CopiedBrushReview/Program.cs) | 1 | Compare normal/explicit/Core-negative brush modes, actual copied-pointer writes, nine-grid inner corners, border overrun canaries and exact persistence/history behavior. |
| [tools/CopiedBrushReview/README.md](tools/CopiedBrushReview/README.md) | 1 | Document scenario commands, actual versus forced UI reachability, fixture-only slide normalization and separate failed-observation semantics. |
| [tests/COPIED_BRUSH_REVIEW.md](tests/COPIED_BRUSH_REVIEW.md) | 1 | Record exact results, historical attribution, prioritized fixes and remaining semantic/native limits. |
| [FutureDevelopment.md](FutureDevelopment.md) | 266 | Track latent copied negative-state handling, explicit nine-grid corner overrides and border bounds as unimplemented HMA-016/017/018 findings. |
| [tests/README.md](tests/README.md) | 37 | Link the investigation separately from implemented map lifecycle protection. |

Findings: Normal copied stamping/repetition and explicit overrides work, but deliberately assigning Core CollisionIndex=-1 with an active copied pattern writes FC-prefixed permission bits. The actual 64-entry Movement dropdown has no selectable Preserve option and ignores empty selection, so no ordinary UI trigger for that retained copied state was demonstrated. Separately, actual nine-grid painting with explicit permission 01 writes an inferred inner corner with permission 0C (3001) from its cached discovery. Actual border drags write before the border with single/multi brushes and after it with a single brush; in-bounds copied border permission words are recorded without inventing engine-specific semantics.

Historical comparison: A separate read-only git archive of 03025d32, before ca25cecd, was built with the same probe. Its left border drag already changes canary 08FE from 5AA5 to 0001, confirming that the overrun predates the recent gesture fix. The snapshot completes nine positive controls and one failed boundary observation; application assembly hashes differ from the current build. No branch switch, checkout, reset or production source edit is performed.

Validation: Twenty-five isolated current-build scenarios complete 343 positive setup/preservation controls plus 23 behavioral observations: 13 passing and 10 failing. The failing observations are findings, not passing assertions. All scenarios check unchanged metadata, in-memory Save/reload and exact Undo/Redo; non-overrun cases also check every byte outside their intended region. Save faithfully persists bad data and Redo reapplies it, while Undo restores all original bytes. Freshly built MapSelection and BlockBagFill pass 394 and 2,081 assertions respectively, zero failures/skips. The full Mac suite is not repeated for investigation-only work; its preceding verified baseline remains 52 groups / 15,854 assertions at e64cca67. Upstream suites are not rerun; fresh dependency builds retain eight existing Core warnings.

Scope: Tooling and documentation only, with fabricated data and no user ROM, personal setting or emulator. Border fixture transitions are disabled only to settle the existing panel before real hit/capture checks. Native animations/trackpad delivery, game-specific border dimensions, gameplay and every corner topology remain outside this investigation. Recommended order: bound border reads/writes first, honor explicit nine-grid override at derived corners second, then define copied-source/override/destination-preserve policy before exposing preservation controls. No fixes are implemented in this entry.

## 113 — Reject out-of-range border access before calculating ROM addresses (HMA-018)

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Map/BorderEditor.cs](src/HexManiac.Core/ViewModels/Map/BorderEditor.cs) | 111 | Share a pixel-coordinate guard across both write overloads and reads, preventing outside/negative-fraction/non-finite positions from reaching ROM addresses. |
| [src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs](src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs) | 1966 | Leave the brush unchanged when border sampling returns an invalid position. |
| [tests/BorderBounds/BorderBounds.csproj](tests/BorderBounds/BorderBounds.csproj) | 1 | Add automatically discovered offscreen Core border-boundary coverage. |
| [tests/BorderBounds/Program.cs](tests/BorderBounds/Program.cs) | 1 | Protect default/custom dimensions, each edge, negative fractions, non-finite coordinates, prior Redo, valid clipping, canaries and exact persistence/history. |
| [tests/BorderBounds/README.md](tests/BorderBounds/README.md) | 1 | Document rejection and valid clipping semantics with synthetic-only scope. |
| [tests/MapGestureLifecycle/Program.cs](tests/MapGestureLifecycle/Program.cs) | 20 | Add actual captured single/copied border drags across all sides, invalid sampling, capture loss and exact Undo/Redo. |
| [tests/MapGestureLifecycle/README.md](tests/MapGestureLifecycle/README.md) | 60 | Describe the new pointer-level border protection. |
| [FutureDevelopment.md](FutureDevelopment.md) | 288 | Mark HMA-018 fixed and retain historical attribution. |
| [tests/COPIED_BRUSH_REVIEW.md](tests/COPIED_BRUSH_REVIEW.md) | 136 | Record the implementation separately from the original failed audit observations. |
| [tests/README.md](tests/README.md) | 37 | Link implemented border coverage alongside the remaining review findings. |

Behavior: Border writes reject a pointer origin outside the border before conversion or address calculation; even a small negative fraction cannot alias the first cell. Sampling returns -1 outside the region and does not replace the active brush. NaN/infinite coordinates are rejected too. Valid single writes and copied patterns retain their packed data and existing right/bottom intersection clipping. Rejected operations request no change token and retain prior Redo.

Evidence and validation: The new negative-fraction regression fails before the change by writing ROM bytes at (-0.001,8). BorderBounds now passes 190 assertions over default 2x2 and explicit 3x4 layouts, exact/fractional edges, invalid values, retained history, unrelated bytes/metadata, canaries, in-memory Save, independent reload and saved-state Undo/Redo. Two enforced real-pointer border scenarios pass another 38 assertions, checking actual image capture, all four edges, invalid brush sampling and interruption cleanup. The full Mac suite follows the independent nine-grid fix; upstream suites are not rerun. Fresh dependency builds retain eight existing Core warnings.

Scope: Border access validation only; no numerical parser, pointer relocation, painting encoding, history algorithm, compilation, game-engine or user-ROM changes. Valid clipping is preserved; the fixture-only slide animation is disabled for reliable headless hit testing. Native hardware/animation delivery remains outside this verification.

## 114 — Apply explicit nine-grid movement choices to inferred and fallback corners (HMA-017)

| File Name | Line | Summary |
| --- | --- | --- |
| [src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs](src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs) | 171 | Track an explicit permission choice separately from copied packed pattern values and clear it when selecting a fresh brush. |
| [src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs](src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs) | 996 | Retain original Draw9Grid overloads and optionally apply a permission override after corner-art selection at every written cell. |
| [tests/MapGestureLifecycle/Program.cs](tests/MapGestureLifecycle/Program.cs) | 20 | Add a real-control nine-grid matrix over all four inner corners, choice timing, copied defaults, subsequent strokes, fallback APIs and exact history/save behavior. |
| [tests/MapGestureLifecycle/README.md](tests/MapGestureLifecycle/README.md) | 73 | Document copied versus explicit policy, fallback compatibility and the unchanged latent negative-state finding. |
| [FutureDevelopment.md](FutureDevelopment.md) | 277 | Mark HMA-017 implemented without declaring HMA-016 fixed. |
| [tests/COPIED_BRUSH_REVIEW.md](tests/COPIED_BRUSH_REVIEW.md) | 146 | Retain original failed evidence and describe the independently implemented nine-grid follow-up. |
| [tests/README.md](tests/README.md) | 37 | Identify both implemented fixes and the remaining latent Core issue. |

Behavior: Explicit Movement selection now applies to every nine-grid cell, including automatically discovered inner corners and no-discovery fallbacks. An override belongs to the current brush and persists between its strokes; selecting a fresh copied/picker pattern clears it. Ordinary mixed copied/derived permissions and corner-art discovery remain intact. Existing int/double Draw9Grid signatures remain available and keep their prior no-override semantics.

Evidence: The enforced pre-fix case retains inner-corner 3001 after explicit permission 00. It now passes 602 assertions across four corner orientations and 00/01/3F selected before/after activation, matching the unmodified copied geometry/art while comparing every unrelated ROM byte and metadata. Re-copy and Cancel do not leak prior overrides; subsequent strokes with the same brush retain the choice. Save independently reloads exact bytes/metadata; Undo/Redo preserve separate edits and saved checkpoints. Direct Core checks preserve the legacy API and verify fallback corners with no discovery.

Validation: `scripts/verify-mac.sh --no-restore` freshly builds and passes all 53 regression groups / 16,684 assertions, zero failures and zero skipped groups. BorderBounds contributes 190 assertions; MapGestureLifecycle passes 1,285 assertions across 36 isolated scenarios, including 38 border-pointer checks and 602 nine-grid checks. Existing copied patterns, bag fill, rectangle permissions, interruption/Escape, same-cell editing, reveal ownership, clipboard routing, compilation/resource atomicity, pending drafts/persistence and HMA-006/007/history protections remain passing. The separate upstream suites were not rerun; fresh dependency builds retain eight existing Core warnings.

Scope: Current-brush explicit override tracking and packed write composition only. No new permission selector, preservation mode, numeric parser, compiler, allocation/relocation or history algorithm is added. HMA-016's forced negative copied-brush state remains outstanding. All tests use fabricated maps/in-memory Save; user ROM/settings and emulator behavior are unchanged. Native hardware/gameplay and game-specific art validation remain manual limits.

## 115 — Refresh the Mac Edition welcome screen and add contributor credits

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/StartScreen.axaml | 42 | Present HexManiacAdvance with a clean Mac Edition subtitle, smaller credits with bold labels and linked names, an Open ROM action and a compact help footer. |
| src/HexManiac.Avalonia/Controls/StartScreen.axaml.cs | 16 | Open contributor profiles and support links through macOS with the destination passed as a separate process argument. |
| src/HexManiac.Avalonia/AppIdentity.cs | 9 | Define the Mac Help destination in the fork README alongside the existing repository and issue URLs. |
| tests/WelcomeScreen/Program.cs | 54 | Extend existing window-size checks to include the contributor block and support links. |
| tests/WelcomeScreen/README.md | 23 | Record the credit and support destinations and the remaining native browser verification. |

Validation: WelcomeScreen builds with zero warnings/errors and passes all 33 assertions, including bounds at 1200x800, 800x600 and 500x400, rounded button states, existing Open command, zero-tab lifecycle, visible errors, shell mounting and version identity. Rendered and visually inspected the actual compiled control at 1200x800. No ROM or settings changed. Browser launches and native interaction remain manual checks; the full regression suite was not rerun for this presentation-only change.

## 116 — Align welcome-screen credit names with their labels

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/StartScreen.axaml | 31 | Center link-button content vertically so contributor names align with the adjacent bold credit labels. |

Validation: WelcomeScreen builds and passes all 33 assertions, including three window sizes and existing button states and Open behavior. Visually inspected the newly rendered 1200x800 control and confirmed all three names align with their labels while retaining clickable buttons. Native interaction remains a manual check.

## 117 — Export map PNGs beside the ROM with explicit conflict choices

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/MacPlatform/MapImageExporter.cs | 9 | Save map PNGs in MapImages beside the ROM with safe filenames, overwrite/copy/cancel choices, numbered copies, temporary-file publication and save-picker fallback. |
| src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs | 444 | Connect map export to the Mac PNG encoder, existing option/save dialogs and a successful-save confirmation with Reveal in Finder. |
| src/HexManiac.Core/Models/IFileSystem.cs | 139 | Add optional map image export support without changing existing filesystem implementations. |
| src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs | 2316 | Route native-resolution map renders to platform export, distinguish connected images and restore selection and event visibility in a finally block. |
| tests/MapImageExport/Program.cs | 11 | Verify destination policy, conflict actions, races, encoding failures, fallback, actual PNG pixels/dimensions, renderer routing and unchanged ROM bytes, metadata and history. |
| tests/MapImageExport/README.md | 1 | Document export behavior, automated verification and manual Mac dialog, event-appearance and Finder checks. |
| tests/MapImageExport/MapImageExport.csproj | 1 | Include the synthetic map fixture and production Mac assembly in the portable export regression group. |
| tests/README.md | 43 | Include the PNG export regression group in the coverage guide. |

Behavior: Edit Map Header > Export Image saves a PNG under MapImages in the ROM directory, using the existing map name and bank/map ID; All Connected Maps adds -connected. First export needs no destination prompt. An existing image offers Overwrite, Save Updated Copy (the first available numbered name from 2), or the dialog's Cancel action; closing the dialog also cancels. Unsaved/relative ROM paths use a save picker; an unavailable destination or failed write offers one fallback picker. A temporary file is encoded and flushed before publication, so failed encoding does not truncate an existing PNG, and a competing new-file writer is not silently overwritten. Successful saves offer Reveal in Finder. Existing clipboard export remains available to other filesystem implementations.

Validation: MapImageExport passes 33 assertions, including a decodable PNG with exact RGB555 pixels, all three event display modes, native 16-pixel tile dimensions, connected naming, state restoration after export failure and unchanged bytes/metadata/history. The complete `scripts/verify-mac.sh --no-restore` run freshly passes 54 regression groups / 16,723 assertions, zero failures and zero skipped groups. NuGet reports NU1900 warnings because its vulnerability-data service is unavailable; builds and checks succeed. The separate upstream suites were not rerun. Tests use temporary files and fabricated maps only; no user ROM or settings are changed. Native dialog appearance, Finder reveal, multi-map visual composition and live event-marker appearance remain manual checks.

## 118 — Make welcome-screen links recognizable at rest

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/StartScreen.axaml | 33 | Render contributor and footer links with permanent underlines, a consistent purple foreground, brighter hover styling and a visible keyboard-focus outline. |

Validation: WelcomeScreen passes 33 assertions, including three window sizes, existing Open behavior and button states. Visually inspected a fresh rendered preview of the compiled control with all six links underlined and the credits still aligned. Native hover/focus interaction remains a manual check; no new test group was added for this appearance change.

## 119 — Follow the approved GitHub repository rename to MacEdition

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/AppIdentity.cs | 8 | Point the shared repository identity, crash reporting, issue menu, Mac Help and release page at arkitekt13/HexManiacAdvance-MacEdition. |
| src/HexManiac.Avalonia/Updates/MacUpdateChecker.cs | 17 | Request compatible releases from the renamed repository's API endpoint. |
| tests/CrashReporting/Program.cs | 38 | Verify the literal crash-report destination uses the new MacEdition repository name. |

Validation: GitHub visibly confirms the approved repository rename and `git ls-remote` verifies access through the updated origin URL. WelcomeScreen (33), CrashReporting (29) and StartupUpdates (42) pass all 104 assertions, covering layout, fork report identity, compatible releases, cache isolation and network failure behavior. Fresh builds retain existing Core and unavailable NuGet vulnerability-feed warnings with zero errors. The full suite was not repeated after the preceding 54-group / 16,723-assertion PNG-export run. Historical changelog URLs and the local folder name are retained. GitHub Issues was observed disabled and has not been enabled as part of this rename.

## 120 — Add map status-bar zoom buttons and native dimensions

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 1181 | Replace fixed-position status text with a wrapping map-size readout, native-pixel tooltip, cursor coordinates and accessible minus/plus buttons. |
| src/HexManiac.Avalonia/Controls/MapTab.axaml.cs | 549 | Route status-button clicks through existing view-centered zoom after completing any owned pointer interaction and dismissing stale map previews. |
| src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs | 171 | Expose cached layout dimensions and zoom availability with change notifications on layout refresh and scaling. |
| tests/MapZoom/Program.cs | 99 | Click the real buttons through zoom limits and verify native dimensions, tooltip, navigation, resize and Undo/Redo alongside existing wheel checks. |
| tests/MapZoom/README.md | 84 | Document status-bar verification, optional synthetic preview rendering and remaining native trackpad checks. |

Behavior: The bottom-right map status area shows the selected map's width and height in tiles, excluding border tiles; hovering shows native pixels at 16x16 per tile, independent of zoom. Minus/plus use the existing view-center zoom path, levels and border compensation, disabling at 1/16x and 10x. Map navigation and width/height resizing, including Undo/Redo, update the bound readout and tooltip. Status contents wrap within available map width rather than using fixed Canvas offsets. No zoom-step, border-rendering, ROM-format or game-engine algorithm is changed.

Validation: Fresh targeted builds and runs pass MapZoom (1,276), MapRendering (93), MapImageExport (33), MapHistoryRefresh (23) and MapSelection (394): five groups / 1,819 assertions, zero failures. MapZoom uses three fresh synthetic sessions, 360 routed wheel events plus real status-button clicks, exact byte/metadata/history checks, actual width/height resize sliders and Undo/Redo. Visually inspected a rendered synthetic map tab with the new status bar. Existing Core and unavailable NuGet vulnerability-feed warnings remain; builds have zero errors. No user ROM or settings changed. The full Mac suite was not repeated for this change; native trackpad usability and tooltip appearance remain manual checks.

## 121 — Polish map status labels and zoom symbols

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 1188 | Label the cursor readout X,Y and center larger bold minus/plus symbols within their existing buttons. |

Validation: MapZoom freshly builds and passes all 1,276 assertions, including real button clicks, endpoint disabling, wheel zoom, dimension tooltips and resize Undo/Redo. Visually inspected the rendered synthetic map tab to confirm centered bold symbols and the coordinate label. Existing Core and unavailable NuGet vulnerability-feed warnings remain; the build has zero errors. No new tests were added for this presentation-only adjustment; native appearance remains a user check.

## 122 — Keep blockset hover previews bounded and input-transparent

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/BlocksetHoverPreview.cs | 10 | Fit the complete preview into the map area and release its image on row exit, scroll, context recycling or detachment. |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 714 | Replace both native blockset image tooltips with row hover handlers and a non-interactive map-owned preview layer. |
| src/HexManiac.Avalonia/Controls/MapTab.axaml.cs | 49 | Dismiss blockset previews on selection, dropdown/header closure, map-context changes, window deactivation and detachment. |
| tests/BlocksetPreviews/Program.cs | 14 | Exercise both real selector templates, rapid row changes, dismissal, oversized image fitting, window resizing and byte/metadata/history preservation. |
| tests/BlocksetPreviews/BlocksetPreviews.csproj | 1 | Add the portable synthetic preview regression group to automatic discovery. |
| tests/BlocksetPreviews/README.md | 1 | Document preview verification and remaining native compositor checks. |
| FutureDevelopment.md | 313 | Defer the broader hex-editor, Go To and image-selector preview audit at the user's request. |

Validation: BlocksetPreviews builds with zero warnings/errors and passes 38 assertions using fabricated blocksets and a separate 128x4096 image. The prior native path was independently observed with Pointer placement, hit testing enabled, no image size bounds and a 648-pixel image clipped within a 600-pixel tooltip. Neither selector now attaches that native image tooltip. No user ROM/settings changed. Native Mac flicker and visual placement still require user verification; the render-count defect is handled separately.

## 123 — Render only valid blocks in standalone blockset previews

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/Models/Runs/Sprites/BlockmapRun.cs | 498 | Derive standalone preview length from known table bytes or existing block boundaries and pass the correct highest-used index for blocks and attributes. |
| src/HexManiac.Avalonia/Controls/BlocksetHoverPreview.cs | 56 | Place fitted images beyond the dropdown popup's right edge when space permits so the list cannot obscure them. |
| tests/BlocksetPreviews/RenderBounds.cs | 5 | Verify exact pixels and metadata across primary/secondary, FireRed/Ruby-Emerald, short/partial/full-capacity and inferred/formatted blockset tables. |
| tests/BlocksetPreviews/Program.cs | 19 | Include the renderer matrix and verify both selector previews appear beside their rows. |
| tests/BlocksetPreviews/README.md | 18 | Document the pre-fix oversized-render reproduction and the retained isolated-secondary preview limitation. |
| tests/MapGestureLifecycle/Program.cs | 122 | Find a hit-tested empty background point instead of clicking the bottom-right status controls during capture-loss verification. |
| tests/README.md | 43 | Add blockset preview coverage and identify the broader tooltip audit as deferred. |

Evidence: Before the render correction, the new two-block regression fails with a 128x1040 preview instead of 128x16. The original FireRed probe forced 641 preview blocks even when only two were estimated, exceeding the normal 384-block secondary capacity. The renderer now uses an actual known table byte length (including word-formatted data) or the existing boundary estimator, bounded by the applicable primary/secondary capacity. Only standalone RenderBlockset changes; normal map rendering retains its existing usage-based reads and tile/palette behavior.

Validation: BlocksetPreviews passes 160 assertions: 120 renderer checks across 24 fabricated cases and 40 hover/lifecycle/placement checks. The fresh complete `scripts/verify-mac.sh --no-restore` run passes 55 regression groups / 17,001 assertions, zero failures and zero skipped groups. An earlier full run exposed a gesture probe whose hard-coded background coordinate now fell on the recently added status bar; the corrected probe confirms actual MapBackground capture and retains all eight scenario checks. Existing data/resource compilation, pointer/metadata/history protections, map gestures, Undo/Redo and Mac input regressions pass. The separate upstream suites were not rerun. No user ROM/settings changed; native hover appearance and flicker still require a live user check.

Scope: Secondary previews still show an isolated blockset rather than combining it with a selected primary set, so primary-dependent tiles may remain incomplete. The broader hex-editor/Go To preview audit is deferred in FutureDevelopment.md.

## 124 — Prevent map-header tileset selection from rebuilding its own live dropdown

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Map/MapHeaderViewModel.cs | 85 | Guard reentrant option synchronization during selection, reuse choices by ROM address and refresh the cache when a committed address is missing. |
| src/HexManiac.Core/ViewModels/Map/MapHeaderViewModel.cs | 150 | Keep explicit graphics invalidation and synchronous pointer writes while ignoring transient invalid indices and programmatic synchronization callbacks. |
| src/HexManiac.Core/ViewModels/Map/MapHeaderViewModel.cs | 267 | Refresh labels and lazy preview content without replacing equivalent bound option objects. |
| src/HexManiac.Avalonia/Controls/MapTab.axaml.cs | 570 | Queue presentation-only synchronization after the native selection event with generation, map and header identity checks. |
| src/HexManiac.Avalonia/Controls/BlocksetHoverPreview.cs | 27 | Dismiss a visible preview when its stable option's content is refreshed and release the content subscription on dismissal. |
| tests/BlocksetSelection/Program.cs | 23 | Reproduce the real binding crash and verify both selectors across stale, reordered, shrinking, missing-choice and keyboard cases with exact persistence and history checks. |
| tests/BlocksetSelection/BlocksetSelection.csproj | 1 | Add the portable full-window selection regression to automatic discovery with synthetic map resources. |
| tests/BlocksetSelection/README.md | 1 | Document the crash mechanism, input and cache matrix, queued callback safety, persistence and remaining native checks. |
| tests/README.md | 43 | Include selection stability and presentation-only refresh in the regression coverage guide. |

Evidence: The two November 4 crash reports have identical ArgumentOutOfRangeException traces in Avalonia SelectedItems enumeration during ComboBox.OnPointerReleased. A real second map header recreates the shared blockset cache with the same addresses but different option instances. Before this change, the synthetic regression reproduces that exact crash: PrimaryIndex notification calls ClearCaches, which clears the live option collections inside the native selection commit.

Behavior: Pointer assignments still finish synchronously in the original history token; only option-list presentation is deferred until after the selection event. Equivalent address lists keep their option identities while labels and preview content refresh. Genuine membership/order changes are synchronized under a model-write guard, and a committed pointer missing from the shared cache causes a cache refresh from the current ROM model. Queued work is coalesced and checked against the current map/header/context; it cannot reapply an undone selection or affect a newly navigated map. Explicit header refresh still invalidates graphics at unchanged addresses without requiring a collection rebuild.

Validation: BlocksetSelection passes 199 assertions, including twelve primary/secondary input/cache cases, exact four-byte pointer changes, Save before queued work, independent reload, saved-state Undo/Redo, Undo-before-refresh, navigation, invalid indices, stable preview content and changed-graphics refresh/Undo. The new matrix also caught a missing-choice blank-selection edge and an explicit-graphics-refresh preservation gap before completion. The fresh serial `scripts/verify-mac.sh --no-restore` run passes 56 regression groups / 17,200 assertions, zero failures and zero skipped groups. An overlapping run hit the unchanged FocusAnimation movement assertion; it subsequently passed three standalone reruns and the complete serial run without relaxing any assertion. Existing Core/NuGet warning behavior remains; builds succeed. The separate upstream suites were not rerun. No user ROM, settings, crash log or metadata sidecar changed; saves are in memory. Native macOS testing on the user's map remains pending. Numeric parsing, script compilation, relocation and game-engine algorithms are unchanged, and the broader preview audit remains deferred.

## 125 — Restore NPC template sprite selection with an independent remembered choice

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Map/EventTemplate.cs | 112 | Publish refreshed NPC sprite choices together and retain a valid session selection while ignoring transient binding indices. |
| src/HexManiac.Core/ViewModels/Map/EventTemplate.cs | 201 | Keep NPC graphics independent of trainer preferences and use that choice for NPC creation and its toolbar icon. |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 1378 | Fit the selected template sprite inside the existing toolbar icon box, including oversized sprites. |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 1450 | Restore the NPC-only pre-placement picker with the correct enum spelling, bounded sprite previews and the independent selection binding. |
| tests/NpcTemplate/Program.cs | 25 | Exercise the real settings popup, sprite and template transitions, navigation, refresh, exact generated NPC bytes, in-memory persistence and Undo/Redo. |
| tests/NpcTemplate/NpcTemplate.csproj | 1 | Include the synthetic headless NPC regression in automatic Mac suite discovery. |
| tests/NpcTemplate/README.md | 1 | Document the original hidden picker, coverage and session-only retention with remaining native checks. |
| tests/README.md | 61 | Link NPC template selection and preservation coverage. |

Evidence: The original panel checked the case-sensitive string `NPC` against enum value `Npc`, hiding its sprite picker. Its old binding also shared TrainerGraphics, which consults trainer preferences. The pre-fix headless regression fails because the NPC picker is absent. A follow-up refresh assertion caught the remembered model choice surviving while the displayed selection became blank; publishing the NPC option snapshot together preserves both.

Behavior: Choose the NPC sprite before placement; the toolbar icon and subsequent NPCs use it. Switching templates, closing/reopening settings and navigating maps in the same open ROM retain the choice. Trainer/Legendary keep their existing setting. Large sprites fit bounded previews and the toolbar box. Removing a selected sprite from refreshed options falls back to ID 0. The preference is not persisted across application relaunch. The generated NPC script, dialogue bytes, movement, elevation, range, trainer fields and flag defaults retain their existing behavior.

Validation: NpcTemplate passes 44 assertions. The fresh complete serial `scripts/verify-mac.sh --no-restore` run passes 57 regression groups / 17,244 assertions, zero failures and zero skipped groups. Existing sprite selection, map gesture/cancellation, script compilation/relocation, source editing and history protections pass. Builds succeed with existing Core/NuGet warnings; the separate upstream suites were not rerun. No user ROM, settings or sidecar changed; Save/reload uses memory. Native macOS appearance and dragging the chosen sprite onto the user's map remain manual checks.

## 126 — Publish the updated Mac Edition accomplishments sheet

| File Name | Line | Summary |
| --- | --- | --- |
| output/pdf/HexManiacAdvance_MacEdition_Accomplishments.pdf | N/A | Publish a four-page summary with 65 accomplishment bullets, updated map/NPC features and the latest verified Mac regression totals. |

Validation: Rendered and visually reviewed all four pages, checked page numbering and extracted text, and confirmed the 57-group / 17,244-assertion totals against the last completed Mac run at code checkpoint 03c69932. Benchmark figures retain workload-specific scope, and planned tutorial/guide work is distinguished from implemented features. This commit adds documentation only; no code, user ROM or settings changes, and no additional test-suite run.

## 127 — Investigate first launch, helper distribution and GitHub presentation

| File Name | Line | Summary |
| --- | --- | --- |
| tests/FIRST_LAUNCH_READINESS_REVIEW.md | 1 | Record confirmed startup/settings/packaging defects, GitHub attribution and setup gaps, implementation boundaries and deferred release checks before changing code. |
| tests/release-readiness/startup-before.json | 1 | Preserve original filename truncation, rejected-metadata startup exception and settings permission-failure observations. |
| tools/FirstLaunchReview/Program.cs | 1 | Reproduce startup behavior with temporary synthetic ROMs, unreadable metadata and stub app settings. |
| tools/FirstLaunchReview/FirstLaunchReview.csproj | 1 | Build the standalone investigation probe against the actual Mac application. |
| tools/FirstLaunchReview/README.md | 1 | Explain reproduction and isolation guarantees. |

Validation: Unchanged application builds; six existing groups pass 752 assertions with zero failures. The baseline probe reproduces filename truncation, an out-of-range startup lookup after metadata rejection and unhandled settings read permissions. A disposable working-directory probe confirms that invalid packaging inputs delete previous output before failing. No production source, personal settings or user ROM changed. Separate implementation commits follow this evidence checkpoint; the complete Mac suite was not rerun during investigation.

## 128 — Preserve startup paths and reject invalid ROM metadata without crashing

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/App.axaml.cs | 269 | Preserve literal filenames, consume only valid address suffixes and bind queued navigation to the actual opened live tab. |
| src/HexManiac.Core/ViewModels/EditorViewModel.cs | 765 | Return an awaitable explicit open result, retain metadata errors, catch malformed numeric metadata and guard import targets after initialization yields. |
| tests/FirstLaunch/Program.cs | 1 | Cover startup syntax, rejected metadata, retry, cancellation, delayed opening and navigation/close before address callbacks. |
| tests/FirstLaunch/FirstLaunch.csproj | 1 | Add the isolated headless first-launch group to automatic Mac regression discovery. |
| tests/FirstLaunch/README.md | 1 | Document preservation checks and the native Finder delivery limitation. |
| tools/FirstLaunchReview/Program.cs | 1 | Await the explicit startup operation when rerunning the investigation probe. |

Validation: FirstLaunch passes 33 assertions, including actual dispatched address navigation and cancellation after switching/closing tabs. FileDrop 24, SaveSafety 17 and ScriptWorkflowSafety 598 also pass (672 assertions across four groups). The original probe reproduced filename truncation and a startup out-of-range exception before this fix. No user ROM/settings changed; ordinary command/picker opening remains available, and numeric parsing changes are limited to startup address syntax and rejecting malformed sidecar fields, not script operands. The complete suite follows the remaining readiness changes.

## 129 — Keep settings access failures from aborting first launch or replacing unread preferences

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs | 45 | Allow an isolated app-support path for tests and expose nonfatal settings warnings without overriding the user's profile. |
| src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs | 80 | Load defaults after settings I/O failure, prohibit overwriting unread preferences and use staged writes that preserve earlier settings on failure. |
| src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs | 340 | Convert denied ROM-sidecar reads into an explicit open failure rather than treating unread metadata as absent. |
| src/HexManiac.Avalonia/App.axaml.cs | 309 | Show settings-read failure as an ordinary nonmodal welcome-screen error. |
| tests/FirstLaunch/Program.cs | 90 | Check isolated fresh profiles, restart, invalid directories, real permission failures, byte preservation and denied ROM-sidecar opening. |
| tests/FirstLaunch/README.md | 1 | Document the expanded settings and metadata coverage. |

Validation: FirstLaunch passes 47 assertions with zero failures. Fourteen additional checks exercise the real Mac filesystem with disposable profiles, including chmod-denied reads and read-only writes; unread settings and sidecars remain byte-for-byte unchanged. Existing settings paths and ROM metadata locations remain compatible; only tests inject another directory. No personal preferences or user ROMs changed. Settings-read protection lasts for the session so a later permission change cannot silently replace previously unread preferences with defaults. The complete Mac run follows the remaining readiness work.

## 130 — Verify external emulator setup and recovery for helper testing

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ExternalEmulatorLaunch/Program.cs | 95 | Cover failed preference publication/read, application removal during selection, retry after a launch exception and closing the ROM while the chooser is pending. |
| tests/ExternalEmulatorLaunch/README.md | 1 | Record 31 macOS assertions and correct the obsolete built-in F6 availability note. |

Validation: ExternalEmulatorLaunch passes 31 assertions using temporary fake applications/preferences and injected process starters. Existing first-use cancellation, remembered path, Browse output, Save and Launch, rejected source and failed-save checks still pass. The seven added checks confirm the existing launcher already handles the investigated recovery cases; no launcher implementation or personal preferences changed, and no real emulator was started.

## 131 — Build Mac bundles from any directory without destroying the previous successful build

| File Name | Line | Summary |
| --- | --- | --- |
| publish-mac.sh | 1 | Validate architectures, resolve repository paths, stage/self-contain/sign/verify bundles and preserve previous output until publication succeeds. |
| tests/MacPackaging/Program.cs | 1 | Run the real script with a disposable fake toolchain across invalid inputs, build/resource/runtime/signature failures and independent architecture outputs. |
| tests/MacPackaging/MacPackaging.csproj | 1 | Include packaging control-flow regressions in the complete Mac runner. |
| tests/MacPackaging/README.md | 1 | Distinguish simulated script checks from real native artifact verification. |
| tools/PublishedBundleReview/Program.cs | 1 | Load the actual published assemblies from an unrelated directory with isolated preferences and verify resources/settings/runtime presence. |
| tools/PublishedBundleReview/PublishedBundleReview.csproj | 1 | Keep the artifact probe independent of application project references. |
| tools/PublishedBundleReview/README.md | 1 | Document reproduction and limits of managed published-startup checks. |
| tests/FIRST_LAUNCH_READINESS_REVIEW.md | 79 | Record real arm64 publish, architecture, signature, version, resource and startup-loader evidence. |

Validation: MacPackaging passes 30 assertions; the original disposable working-directory probe deleted its previous output before failing. A real restored self-contained arm64 publish now succeeds, has a Mach-O arm64 executable, verified ad-hoc signature, application-matching 0.6.1 bundle version, 61 resource files and approximately 145 MiB disk usage. The independent published-loader probe passes seven checks including script references and isolated settings save/reload. Outputs are architecture-specific under artifacts/mac/osx-arm64 or artifacts/mac/osx-x64; failures retain the last successful bundle and clean their staging directory. Minimum runtime OS is recorded as 12.0, not a certification of all OS versions; native Intel/Gatekeeper/notarization/clean-machine launch remain unverified. No user settings or ROM changed.

## 132 — Repair the original Mac porter credit destination

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/StartScreen.axaml | 89 | Retain the xavierkeys credit while linking the verified original Mac port instead of the unavailable profile target. |
| tests/WelcomeScreen/Program.cs | 56 | Assert the original-porter attribution destination without launching a browser. |
| tests/WelcomeScreen/README.md | 1 | Explain the verified credit link and unchanged native browser check. |

Validation: WelcomeScreen builds and passes 34 assertions, including layout at three window sizes and the new credit URL check. Git history identifies xavierkeys as the original Mac port author, while GitHub identifies the fork parent as Xkeys-bot/HexManiacAdvance-mac; the former profile URL could not be verified. Credit text and visual layout remain unchanged. No ROM or settings changed.

## 133 — Explain Mac Edition setup, features, credits and helper reporting on GitHub

| File Name | Line | Summary |
| --- | --- | --- |
| README.md | 1 | Replace obsolete Windows/hidden-feature guidance with Mac setup, source builds, real controls, feature scope, external emulator setup and prominent thanks to Haven and xavierkeys. |
| docs/MAC_HELPER_TESTING.md | 1 | Provide a practical first-run checklist, report details, metadata locations and explicit remaining native/distribution/runtime work. |
| docs/REPOSITORY_PRESENTATION.md | 1 | Record reversible before/after About, website and issue-tracker settings, separate from version-controlled documentation. |
| .github/ISSUE_TEMPLATE/mac_bug_report.md | 1 | Offer a Mac-specific reproduction/environment template without requesting ROM uploads. |
| FutureDevelopment.md | 7 | Mark issue reporting enabled and retain native link, clean-machine, Intel, signing and runtime-upgrade reminders. |
| PORT_STATUS.md | 3 | Frame the original porter notes as historical and direct current users to the Mac Edition README. |
| RELEASE_NOTES.md | 1 | Distinguish historical release notes from current fork assets and installation instructions. |
| tests/README.md | 17 | Link first-launch, packaging and helper-readiness evidence. |

Validation: New relative documentation links and the existing README#on-macos in-app help anchor resolve. GitHub's actual About description/website are updated and Issues is enabled, confirmed in both the web UI and REST metadata; no issues or messages were submitted. The original Mac repository is verified as the fork parent, and Haven's project/wiki remain prominent. No downloadable release is fabricated or claimed; no native compatibility, notarization or runtime upgrade is implied. WelcomeScreen separately passes 34 checks for the repaired attribution link. The final complete Mac suite is running against unchanged application code; its full result will be recorded in a separate evidence checkpoint.

## 134 — Record final helper-readiness verification separately from implementations

| File Name | Line | Summary |
| --- | --- | --- |
| tests/release-readiness/startup-after.json | 1 | Preserve corrected filename, rejected-metadata and settings-read observations against the original evidence. |
| tests/release-readiness/mac-verification.json | 1 | Record all 59 regression group/assertion counts, tested checkpoints and the initial unchanged focus-animation failure/retry. |
| tests/release-readiness/published-bundle-verification.json | 1 | Record the rebuilt arm64 bundle's version, runtime, signature, resources, loader checks and managed-assembly SHA-256. |
| tests/FIRST_LAUNCH_READINESS_REVIEW.md | 89 | Summarize final validation, live GitHub changes and remaining native/runtime/distribution limits. |

Validation: The complete serial Mac runner passes 59 groups / 17,329 assertions, zero failures and zero skipped groups. The first full attempt stopped on the pre-existing unchanged FocusAnimation movement assertion; its unchanged standalone rerun and subsequent full run both pass without weakening any assertion. The final self-contained arm64 bundle builds from /private/tmp, passes strict signature verification and all seven independent published-loader checks with 61 resources and isolated preference roundtrip. Builds retain existing Core warnings; separate upstream suites were not rerun. No user ROM/settings or native app session changed. Runtime upgrade, native/Intel/OS checks, notarization and actual release publication remain explicit follow-up work; this evidence commit adds no application changes.

## 135 — Investigate runtime compatibility, animation timing, variable omissions and helper archives

| File Name | Line | Summary |
| --- | --- | --- |
| tests/MAINTENANCE_REVIEW.md | 1 | Separate confirmed analysis/test defects, promising isolated runtime results and useful archive preparation from speculative work. |
| tests/maintenance-review/focus-global-delayed.txt | 1 | Preserve the original clock assertion failure under a deliberate dispatcher delay. |
| tests/maintenance-review/focus-local-delayed.txt | 1 | Record the same real animation passing the original assertions with a local clock under the same delay. |
| tests/maintenance-review/variable-before-summary.json | 1 | Record freshly reproduced command/header reference omissions across five game identities. |
| tools/FocusClockReview/Program.cs | 1 | Instrument the production animation with original/global and isolated/local clock adapters for diagnosis only. |
| tools/FocusClockReview/FocusClockReview.csproj | 1 | Build the independent animation investigation against the Mac app. |
| tools/FocusClockReview/README.md | 1 | Explain delay stress, original assertions and isolation. |

Validation: A disposable .NET 10 copy builds successfully and passes ScriptWorkflowSafety's 598 assertions using a temporary SDK/runtime; the working app and installed SDK remain unchanged. The 35-ms clock probe reproduces the old assertion failure and passes all 13 original checks with an independent test clock. The read-only variable audit reconfirms 13 missed command references per game plus conditional-header fields. No production source, user ROM or personal settings changed. Implementations and broader compatibility evidence follow separately.

## 136 — Make the focus-animation regression deterministic without changing the animation

| File Name | Line | Summary |
| --- | --- | --- |
| tests/FocusAnimation/Program.cs | 30 | Give each real outline an independent test clock and verify observed position/size changes under normal and deliberately delayed dispatcher timing. |
| tests/FocusAnimation/README.md | 1 | Explain the global-clock interference, retained requirements and 25 assertions across six animations. |

Validation: The deliberate 35-ms delay reproduces the old test's failure with an already-completed task before its first sample. The independent-clock adapter passes 25 assertions, including the original cast-crash reproduction, actual movement/resizing, completion and cleanup; ten separate processes pass 250 assertions total. The movement check is strengthened to compare sampled values rather than accept a nonzero initial keyframe. Production FocusOutlineAnimation, duration, rendering and native behavior are unchanged. No ROM or settings used.

## 137 — Include confirmed variable operands and map-header comparisons in usage analysis

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Map/Flags.cs | 109 | Share engine-confirmed variable-reference classification between used-ID discovery and Find Uses, including raw commands inside macros and conditional-header fields. |
| src/HexManiac.Core/ViewModels/Map/Flags.cs | 190 | Bound conditional rows before reading them while retaining the existing traversal budget and sentinel semantics. |
| tests/FlagUsageSafety/Program.cs | 30 | Reproduce missing operands before the fix and protect detection/locations, literal exclusions, macro internals, headers, reservations and data/metadata/history immutability. |
| tests/FlagUsageSafety/README.md | 1 | Record 1,098 assertions and the bounded scope without claiming complete unused-ID safety. |
| tests/maintenance-review/variable-after-summary.json | 1 | Record zero omissions in the reviewed matrix and unchanged native reserve inventories for five game identities. |
| tests/FLAG_USAGE_INVESTIGATION.md | 1 | Preserve the original investigation with an explicit implemented follow-up link. |
| tools/FlagVariableReview/UsageDetectionAudit.cs | 126 | Describe the review as read-only analysis of current helpers rather than assuming no implementation has occurred. |
| FutureDevelopment.md | 105 | Mark the bounded repair complete while retaining freshness, legal ranges and native/custom-engine work as separate follow-ups. |

Validation: The expanded regression first fails on BPRE/subvar's second operand against unchanged production code, then passes 1,098 assertions across five synthetic game identities. The original copyvar-source case now excludes that used ID from suggestions. Every reviewed operand/header field is detected in the independent role audit, and existing native reserve inventories are identical before/after. ScriptAssistance 258, DialogueScaffolding 384, ScriptWorkflowSafety 598, MapHistoryRefresh 23 and NpcTemplate 44 also pass. Flags, allocation ranges, numeric parsing, script encoding/relocation, native game code and history algorithms are unchanged; analysis/suggestions preserve bytes, metadata and edit history. Snapshot freshness, native-code completeness and custom-engine safety are not claimed. No user ROM/settings changed.

## 138 — Prepare identifiable helper ZIPs with credits, license, instructions and checksums

| File Name | Line | Summary |
| --- | --- | --- |
| package-mac.sh | 1 | Rebuild the verified architecture-specific app and stage a local helper ZIP with source/build identity, dirty-state labeling, original license, instructions and portable SHA-256. |
| tests/MacPackaging/Program.cs | 72 | Verify real ZIP contents/checksum, source identity, credits, user-data exclusion, dirty labeling, archive failure cleanup and existing-output preservation using fake build/sign tools. |
| tests/MacPackaging/README.md | 1 | Record 43 packaging/archive assertions and distinguish the fake toolchain from native artifact verification. |
| README.md | 95 | Document local helper ZIP preparation and checksum verification without claiming release upload or notarization. |
| docs/MAC_HELPER_TESTING.md | 44 | Explain archive contents, build identification and keeping checksum files with distributed helpers. |

Validation: MacPackaging passes 43 assertions, including thirteen new helper-archive checks. Tests encode and inspect real ZIPs containing synthetic bundles; existing ZIP/checksum bytes remain intact on repetition and archive-creation failure publishes nothing. Invalid architectures still fail before building; separate arm64/x64 output and prior build protection remain covered. The original project license and thanks to Haven/xavierkeys accompany the app, and no ROM or emulator preference is copied. The script prepares generated local artifacts only, with no GitHub release or native emulator/app launch. A final real arm64 archive and full regression verification follow separately.

## 139 — Keep benchmark-backed test fixtures compatible with the newer C# compiler

| File Name | Line | Summary |
| --- | --- | --- |
| tools/ItemTableBenchmark/ItemFixture.cs | 23 | Escape the existing field lambda identifier inside a property accessor so C# 14 does not interpret it as the new contextual keyword. |
| tools/TrainerPortraitBenchmark/PortraitFixture.cs | 45 | Apply the same identifier-only compatibility fix to the portrait fixture property. |

Validation: The isolated .NET 10 trial reproduces CS9273 on each property before these changes. The only edits escape the two existing identifier declarations/references; fixture logic, data and public APIs are unchanged. ItemTablePresentation passes 48 assertions and PortraitFreshness passes 68 on the working .NET 8 setup. The complete isolated .NET 10 rerun includes both fixes; results are recorded separately when complete. No application target framework, dependency version, installed SDK, ROM or settings changed.

## 140 — Record final maintenance, compatibility and helper-archive verification

| File Name | Line | Summary |
| --- | --- | --- |
| tests/MAINTENANCE_REVIEW.md | 82 | Record completed bounded repairs, real archive checks, both complete runtime trials and the decision to retain the current application target. |
| tests/maintenance-review/net8-verification.json | 1 | Preserve all 59 fresh working-runtime group counts and explicit validation limits. |
| tests/maintenance-review/net10-compatibility.json | 1 | Preserve matching isolated runtime counts, temporary trial changes and earlier compiler/runner stops without claiming migration is complete. |
| tests/maintenance-review/helper-archive-verification.json | 1 | Record the clean source identity, SHA-256, ZIP integrity, contents and strict extracted-signature checks for the real arm64 helper archive. |
| tests/README.md | 21 | Link the maintenance investigation and its reproducible evidence from the regression index. |

Validation: Both the working SDK 8.0.300/runtime 8.0.5 and isolated SDK 10.0.403/runtime 10.0.14 pass 59 groups / 17,917 assertions with zero failures and zero skipped groups; exact per-group counts match. The real arm64 helper ZIP from clean implementation commit 52f917a14385 passes SHA-256, complete CRC/content inspection and strict extracted-app signature verification. The installed SDK and working net8.0 app target remain unchanged; no user ROM/preferences changed and no release was published. Separate upstream suites, native launch/emulator/gameplay, Intel, older macOS, Gatekeeper and notarization are not claimed. This entry records evidence only, with no additional application changes.

## 141 — Remove the eight launch-build warnings while preserving execution behavior

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/Core/Autos/StubFileSystem.cs | 57 | Remove ineffective optional defaults from explicit implementations while retaining the interface defaults used by callers. |
| src/HexManiac.Core/Models/CrossTableDataViewPort.cs | 125 | Remove the unused private selectAll field without changing command behavior. |
| src/HexManiac.Core/Models/Runs/TrainerPokemonTeamRun.cs | 38 | Remove the unused minimumLevels field while retaining the live local minimum-level lookup. |
| src/HexManiac.Core/Models/Runs/Sprites/BlockmapRun.cs | 340 | Remove the unread palette-count field and constant assignments without changing palette processing. |
| src/HexManiac.Core/ViewModels/ViewModelCore.cs | 151 | Match XML documentation parameter names to the existing TryUpdate signature. |
| src/HexManiac.Core/ViewModels/QuickEditItems/ApplyCFRUPatch.cs | 34 | Replace async-without-await with synchronous execution wrapped in completed, faulted or canceled tasks while retaining the patch/save/reopen body. |

Validation: A fresh complete Mac application rebuild passes with zero warnings and zero errors, compared with the eight warnings reproduced before cleanup; no warning suppressions were added. Fresh targeted builds and runs pass BlocksetSelection 199, ScriptWorkflowSafety 598 and ItemTablePresentation 48 assertions, totaling 845 across three groups. A temporary synthetic probe passes five additional checks for completed invalid-tab diagnostics and a faulted missing-resource task with unchanged model bytes; its first setup attempt lacked test resources, then passed after the probe setup was corrected. Two sandbox test builds stalled before output and were terminated, then the serial builds/runs completed outside the sandbox; neither stalled run is counted as a pass. The complete 59-group suite was not rerun for this cleanup. No real CFRU patch, user ROM, personal settings, runtime target, game engine or warning policy changed.

## 142 — Keep the border editor steady while painting and refreshing map data

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs | 636 | Refresh the existing border editor instead of replacing the object that owns live panel visibility. |
| src/HexManiac.Core/ViewModels/Map/BorderEditor.cs | 111 | Invalidate cached dimensions and rendered border data while retaining visibility and editor subscriptions. |
| tests/MapGestureLifecycle/Program.cs | 407 | Reproduce the animated hide/show failure and protect stable geometry, capture, complete single/copied strokes, no-ops, cache refresh, history and explicit open/close. |
| tests/MapGestureLifecycle/README.md | 65 | Explain the new real-animation lifecycle checks and their failing-before reproduction. |
| tests/BorderBounds/Program.cs | 65 | Verify changed border dimensions and exact Undo/Redo refresh without losing open state. |
| tests/BorderBounds/README.md | 18 | Record custom-dimension refresh coverage and the updated assertion count. |

Validation: The new animated single-stroke scenario fails against unchanged production code on the first transient hide. After the fix, all 38 MapGestureLifecycle scenarios pass 1,343 assertions, including the two new animated single/copied-border cases; BorderBounds passes 195 assertions. No drawing, pointer relocation, cache-invalidation scope, numeric rules or history algorithm changed, and no user ROM/settings were used. The normal open/close animation remains enabled. Full Mac-suite verification follows the separate image-preview fix; native trackpad confirmation remains useful.

## 143 — Fit hex and Go To image hints without intercepting their source input

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/ImagePreviewLayer.cs | 15 | Present complete image-bearing hints in a bounded input-transparent window layer with explicit source and window lifetimes. |
| src/HexManiac.Avalonia/Controls/ImageHoverPreview.cs | 10 | Attach image-only hover ownership to Go To tokens with collection, context and detachment cleanup. |
| src/HexManiac.Avalonia/Controls/HexContent.cs | 496 | Reset stale cell hover state and route image content through the fitted layer while preserving text tips and unformatted-cell caching. |
| src/HexManiac.Avalonia/Resources/MenuConverters.cs | 218 | Keep native Go To tooltips for nonempty text-only hints and exclude images handled by the overlay. |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml | 564 | Bind token image-hover content and host one input-transparent image layer in the window. |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs | 262 | Dismiss current image hints when Go To visibility changes. |
| tests/ImageHoverPreviews/Program.cs | 24 | Reproduce hex/compiled-token failures and protect 56 fitting, re-entry, input, text-tip, collection/context, tab and lifetime invariants. |
| tests/ImageHoverPreviews/ImageHoverPreviews.csproj | 1 | Run real compiled window templates with synthetic sprite data and headless routed input. |
| tests/ImageHoverPreviews/README.md | 1 | Document reproduction, assertions, intentional popup exclusions and native verification limits. |
| tests/IMAGE_PREVIEW_LIFECYCLE_REVIEW.md | 1 | Preserve investigation evidence, the bounded repair decisions and explicit coverage limits. |
| tests/preview-lifecycle/mac-verification.json | 1 | Record every fresh regression-group count, source checkpoints and native/security-audit limits. |
| tests/README.md | 17 | Link the preview lifecycle findings and focused regression from the suite index. |
| FutureDevelopment.md | 324 | Close the deferred code audit with confirmed repairs and retain native verification as release work. |

Validation: The real oversized hex hover and compiled Go To image-token cases fail separately against the prior implementation. ImageHoverPreviews now passes 56 assertions with zero build warnings/errors, including full transformed small/tall/wide image bounds, click-through, same-cell re-entry, text tips, keyboard/mouse selection, collection/context recycling, resizing, Go To closure/reopening, detach/reattach, document-tab changes and exact byte/metadata/history preservation. No script compilation, pointer relocation, game-engine data, numeric rules, interactive palette/map-list popups or global tooltip theme changed. The complete serial Mac suite passes 60 groups / 18,036 assertions with zero failures and zero skipped groups; exact counts are recorded in tests/preview-lifecycle/mac-verification.json. Source builds emit no compiler warnings; MapImageExport retains the existing NU1900 package-vulnerability-feed diagnostic, so successful security-data retrieval is not claimed. Native trackpad/compositor confirmation remains useful, and separate upstream suites were not rerun. No user ROM/settings changed.

## 144 — Investigate stale flag/variable usage and deleted-event reservations

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ID_SUGGESTION_LIFECYCLE_REVIEW.md | 1 | Record real-tool reproductions, reservation/source-lifetime risks, bounded repair design and preservation requirements. |
| tests/id-suggestion-lifecycle/before.json | 1 | Preserve the confirmed flag and script-event variable add/delete/reallocate results against the merged baseline. |

Validation: Fabricated maps exercise actual event creation, ID generation and deletion; fresh scans show 0021/4034 are unused after deletion while replacement suggestions incorrectly advance to 0022/4035. Focused regressions fail against the baseline for both cases. No user ROM/preferences were used; implementation and final validation are separate changes.

## 145 — Refresh unused-ID suggestions and release owned reservations safely

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Map/IdSuggestionState.cs | 11 | Separate live usage, scoped tool reservations, conservative unowned requests and retained pending source with byte/metadata invalidation. |
| src/HexManiac.Core/ViewModels/Map/IdSuggestionState.cs | 125 | Read pending operands through compiler conversion and researched roles without writing scripts/resources or interpreting dialogue as code. |
| src/HexManiac.Core/ViewModels/Map/EventTemplate.cs | 153 | Delegate suggestions to the lifecycle state and scope immediate multi-ID template actions. |
| src/HexManiac.Core/ViewModels/Map/IEventViewModel.cs | 838 | Scope immediate object-flag and script-trigger allocation so deleted final references can become reusable. |
| src/HexManiac.Core/ViewModels/Map/MapScriptCollection.cs | 392 | Scope conditional map-script variable generation while retaining its existing ROM write. |
| src/HexManiac.Core/ViewModels/Map/Flags.cs | 132 | Share the existing operand-role classifier with read-only pending-source and known-script analysis. |
| src/HexManiac.Core/ViewModels/Tools/CodeBody.cs | 98 | Scope assisted flag/variable insertion until CodeTool registers the pending source. |
| src/HexManiac.Core/ViewModels/Tools/CodeTool.cs | 66 | Synchronize pending/rejected overworld source reservations with existing compile, discard and state notifications. |
| tests/IdSuggestionLifecycle/Program.cs | 23 | Reproduce event reuse failures and protect 81 reference, draft, numeric, metadata, scope and history invariants. |
| tests/IdSuggestionLifecycle/IdSuggestionLifecycle.csproj | 1 | Link the real synthetic map/scanner/workflow fixtures for isolated lifecycle checks. |
| tests/IdSuggestionLifecycle/README.md | 1 | Describe focused coverage, original failures and conservative analysis boundaries. |
| tests/ID_SUGGESTION_LIFECYCLE_REVIEW.md | 53 | Explain the implemented ownership model and focused validation without claiming exhaustive unused-ID safety. |
| tests/id-suggestion-lifecycle/mac-verification.json | 1 | Preserve all 61 fresh regression-group counts and explicit native/security-audit limits. |
| tests/README.md | 24 | Link the lifetime review and focused group from the regression index. |
| FutureDevelopment.md | 106 | Mark freshness/owned reservation repair complete while retaining range, native/custom-engine and incomplete-scan work. |

Validation: The actual flag and variable deletion/replacement regressions fail before the repair; the focused group now passes 81 assertions across nine synthetic scenarios. No compiler, numeric grammar, candidate range, trainer policy, game-engine data, pointer relocation or history algorithm changed. Known formatted scripts and retained drafts remain protected; bare unowned investigator calls retain conservative reservations. The final serial Mac suite passes 61 groups / 18,117 assertions with zero failures and zero skipped groups. Source builds emit no compiler warnings; the existing MapImageExport NU1900 vulnerability-feed diagnostic is separate. Exact group counts are recorded in tests/id-suggestion-lifecycle/mac-verification.json; native gameplay/UI and separate upstream xUnit suites were not rerun. No user ROM/preferences used.

## 146 — Review ID reservation design and avoid repeated retained-script tail scans

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Map/Flags.cs | 497 | Add a retained-script traversal that scans each known formatted entry separately while preserving the normal public map-root walk. |
| src/HexManiac.Core/ViewModels/Map/IdSuggestionState.cs | 50 | Use the known-entry traversal to avoid following the same formatted tail from every caller. |
| tests/IdSuggestionLifecycle/KnownScriptGraph.cs | 11 | Compare old/new references through shared/cyclic/unformatted paths and protect public suggestions plus model immutability. |
| tests/IdSuggestionLifecycle/Program.cs | 26 | Include the independent graph scenario in the existing regression group. |
| tests/IdSuggestionLifecycle/README.md | 4 | Record 87 assertions across ten scenarios and the differential graph check. |
| tools/IdSuggestionBenchmark/Program.cs | 9 | Measure actual helper initialization, refreshed/cached queries and allocations using fabricated shared script graphs. |
| tools/IdSuggestionBenchmark/IdSuggestionBenchmark.csproj | 1 | Reuse the synthetic map/scanner fixtures without ROM or native-window dependencies. |
| tools/IdSuggestionBenchmark/README.md | 1 | Explain reproducibility and distinguish synthetic single-run observations from real-ROM performance. |
| tests/ID_SUGGESTION_DESIGN_REVIEW.md | 1 | Record ownership/cache/call-site evaluation, the measured traversal problem and the decision to retain the architecture. |
| tests/id-suggestion-review/benchmark-before.jsonl | 1 | Preserve the pre-refinement synthetic timing and allocation measurements. |
| tests/id-suggestion-review/benchmark-after.jsonl | 1 | Preserve matching post-refinement measurements without claiming statistical or real-ROM results. |
| tests/id-suggestion-review/mac-verification.json | 1 | Record all 61 fresh group counts and explicit native/real-ROM/security-audit limits. |

Validation: All 87 focused assertions pass. On the identical synthetic 1,000-entry/25-tail workload, initialization changes from about 1,342 ms to 4.6 ms and a post-edit refresh from 607 ms to 2.2 ms; cached requests remain below 0.05 ms in these single-run observations with unchanged managed allocation. The same candidate is returned, and differential graph checks preserve references, bytes and metadata while reducing repeated visits. The existing public walker, ownership/reservation policy, compiler, numeric grammar, relocation and history are unchanged. The complete serial Mac suite passes 61 groups / 18,123 assertions with zero failures and zero skipped groups; exact counts are in tests/id-suggestion-review/mac-verification.json. Source builds emit no compiler warnings; the existing MapImageExport NU1900 feed diagnostic is separate. The checked-in benchmark reproduces a 2.3-ms refreshed query. No user ROM/preferences, separate upstream-suite certification or real-ROM/native performance claims.

## 147 — Make the GitHub introduction and installation steps easier to follow

| File Name | Line | Summary |
| --- | --- | --- |
| README.md | 6 | Place Haven/xavierkeys credits directly below the description while retaining clear thanks and original-project links. |
| README.md | 13 | Put Installation first with release/source options, architecture selection, source ZIP steps and one Terminal command. |
| README.md | 54 | Replace the editing inventory and technical onboarding with a concise Mac Edition feature list and three practical help links. |
| docs/MAC_SETUP.md | 1 | Move emulator connection, Mac controls and targeted launch troubleshooting out of installation. |
| docs/DEVELOPMENT.md | 1 | Preserve bundle building, tester ZIP preparation and regression commands in developer documentation. |

Validation: The GitHub Releases API currently returns no releases, so Option 1 explicitly directs users to source installation until an app download exists. The actual public default-branch ZIP was downloaded without executing it, and its extracted-folder name and Avalonia project path match the documented command. The official .NET 8 page confirms the macOS SDK installer choices. Relative Markdown destinations, command shell syntax, requested section order and whitespace checks pass. No application code, SDK installation, user ROM/preferences or generated release changed; no full regression rerun was needed for documentation-only edits.
## 148 — Rehearse downloaded-source installation and helper packaging

| File Name | Line | Summary |
| --- | --- | --- |
| tests/INSTALLATION_REHEARSAL.md | 1 | Record actual source ZIP build, ARM64 packaging, extracted-signature checks, isolated startup results and explicit native/clean-machine limits. |

Validation: Downloaded baseline 6c64f826 builds with zero errors; network-enabled ARM64 packaging succeeds, ZIP integrity and extracted ad-hoc signature pass, and the startup probe passes seven checks with 61 resource files and isolated preferences. Initial restricted restore reports NU1900 feed warnings. No application code, personal ROM/settings or published release changed; no native-window, clean-machine or Intel certification is claimed. Future welcome-image/tutorial-patch feasibility is recorded without implementation.

## 149 — Add an optional map background to the welcome screen

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/StartScreen.axaml | 61 | Place an input-transparent map image and dark fade behind the existing welcome content. |
| src/HexManiac.Avalonia/Controls/StartScreen.axaml.cs | 14 | Load the optional PNG on attachment with bounded decoding, safe fallback and disposal on detach. |
| src/HexManiac.Avalonia/HexManiac.Avalonia.csproj | 41 | Copy optional release assets into both build and publish resources while preserving their folders. |
| OptionalAssets/Welcome/README.md | 1 | Document the fixed background filename, source folder, target map dimensions and release workflow. |
| tests/WelcomeScreen/Program.cs | 124 | Cover missing/corrupt PNG fallback, native proportions and input-transparent background layout. |

Validation: Fresh app/test build passes with zero warnings/errors; WelcomeScreen passes 38 assertions, including three window sizes, existing actions/credits/error visibility and optional-image loading. No final background image is bundled yet; no ROM, compiler, history or user preferences changed.

## 150 — Prepare safe optional Tutorial Village patch distribution

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Project/TutorialPatch.cs | 13 | Verify optional manifest/UPS hashes, exact English FireRed 1.1 base and output before creating a separate ROM without overwriting existing ROMs or metadata. |
| src/HexManiac.Avalonia/Windows/MainWindow.Tutorial.cs | 13 | Add a conditionally enabled Utilities action that preserves pending/rejected source and opens the verified new ROM after the save picker. |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml | 188 | Keep Create Tutorial ROM visible but disabled until the assets and matching base are available. |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs | 78 | Initialize and refresh tutorial availability with the active editor and tab lifecycle. |
| OptionalAssets/Tutorial/README.md | 1 | Document the UPS/manifest asset locations, required hashes, base revision and safe new-copy workflow. |
| tests/TutorialPatch/Program.cs | 1 | Protect successful forward application, base/output rejection, metadata/history, new-copy output and retained pending/rejected drafts with 36 synthetic assertions. |
| tests/TutorialPatch/TutorialPatch.csproj | 1 | Reuse synthetic map and script workflow fixtures without personal ROMs or native windows. |
| tests/TutorialPatch/README.md | 1 | Explain focused coverage and native picker/gameplay checks awaiting the finished assets. |
| tests/TabMenus/Program.cs | 42 | Verify the tutorial action is visible, disabled and explanatory for an unavailable or nonmatching base. |
| tests/README.md | 25 | Link the tutorial checks and both optional release-asset setup guides. |
| tests/optional-release-assets/mac-verification.json | 1 | Record every fresh regression count, optional asset publish checks and explicit validation limits. |

Validation: The focused tutorial group passes 36 assertions; the complete fresh Mac suite passes 62 groups / 18,164 assertions with zero failures and zero skipped groups. PNG, JSON and UPS probes copy byte-for-byte into publish resources and are removed afterward. An initial suite run was interrupted by concurrent probe cleanup; the final run uses stable folders and completes. App/focused builds emit zero compiler warnings; the existing MapImageExport NU1900 feed diagnostic is separate. No personal ROM/settings, core compiler, numeric parsing, relocation or history code changed. Native save-picker cancellation, the final background appearance and tutorial gameplay await the actual assets; neither final PNG nor patch is bundled yet.

## 151 — Add a staged Mac Edition release checklist

| File Name | Line | Summary |
| --- | --- | --- |
| docs/MAC_RELEASE_CHECKLIST.md | 1 | Separate recorded verification, private helper preparation, native platform checks, public-release gates and optional asset polish without treating coverage gaps as confirmed bugs. |
| docs/DEVELOPMENT.md | 52 | Link the release checklist from developer release preparation. |
| docs/MAC_HELPER_TESTING.md | 71 | Link the staged checklist and distinguish implemented asset frameworks from unfinished final content. |
| FutureDevelopment.md | 7 | Point release reminders to the candidate gates and platform-results checklist. |

Validation: Local Markdown destinations and whitespace checks pass. Verification totals reference the existing 62-group / 18,164-assertion record; native/clean-machine, Intel, signing, runtime and final-asset work stays explicitly unchecked. This is documentation only; no app code, ROM/settings, dependencies or GitHub release changed, and no regression rerun was required.

## 152 — Focus the Go To text box immediately after Command-G

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs | 641 | Wait for lazy Go To text-box attachment and schedule focus before queued input while cancelling closed/replaced-editor requests. |
| tests/GotoFocus/Program.cs | 1 | Exercise delayed presentation, immediate typing, query replacement, Escape, cancelled requests and unchanged ROM/history with 12 headless checks. |
| tests/GotoFocus/GotoFocus.csproj | 1 | Add an isolated real-window input regression group using fabricated ROM bytes. |
| tests/GotoFocus/README.md | 1 | Record the simulated pre-fix focus failure and the native event-ordering verification limit. |
| tests/GotoFocus/mac-verification.json | 1 | Preserve exact counts for all seven relevant focus/input regression groups. |

Validation: The old code passes ordinary headless opening but fails to focus when text-box attachment is explicitly delayed beyond the queued request; the repair passes all 12 Go To checks. Fresh GotoFocus, ClipboardRouting, AutocompleteDismissal, ImageHoverPreviews, TabMenus, FirstLaunch and MacSecondaryActions builds/checks pass 355 assertions across seven groups, with zero failures/skips and zero build warnings/errors. The full Mac suite was not rerun for this bounded focus change. Native Cocoa timing awaits user confirmation; compiler, Go To matching/navigation, shared field controls, ROM bytes/history and personal settings are unchanged.

## 153 — Investigate a read-only map reference beside script source

| File Name | Line | Summary |
| --- | --- | --- |
| tests/SCRIPT_MAP_REFERENCE_REVIEW.md | 1 | Record source-map ambiguity, live renderer/history side effects, input-routing risks and bounded implementation acceptance checks before production changes. |
| tools/ScriptMapReferenceReview/Program.cs | 1 | Reproduce live-render history effects on fabricated 80 × 50 maps without personal ROMs or preferences. |
| tools/ScriptMapReferenceReview/ScriptMapReferenceReview.csproj | 1 | Link the existing synthetic map fixture and actual renderer for reproducible investigation. |
| tests/script-map-reference/probe-before.jsonl | 1 | Preserve observed cold/cached rendering and unchanged bytes/metadata alongside the NPC dirty/history transition. |

Validation: The completed synthetic probe draws both maps; the NPC case enables Undo and marks the model unsaved while bytes and serialized metadata remain unchanged. Initial draw is about 12–15 ms in this limited single run, and cached access reuses pixels. This is not a real-ROM performance claim or proof of the user's native event timing. GitHub checkpoint checkpoint/pre-script-map-preview-2026-11-05 preserves 68ed0f91 before implementation.

## 154 — Add a map reference beside overworld script source

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/ViewPort.cs | 2601 | Retain explicit source-map context through script-tab reuse/duplication and expose refresh notifications without changing compilation. |
| src/HexManiac.Core/ViewModels/Map/MapEditorViewModel.cs | 305 | Capture the current map for event double-click and new-script-tab routes. |
| src/HexManiac.Core/ViewModels/Map/BlockMapViewModel.cs | 2675 | Carry map identity when following an event's script-address link. |
| src/HexManiac.Core/ViewModels/Map/MapScriptCollection.cs | 312 | Preserve originating map context for map-header script navigation. |
| src/HexManiac.Core/ViewModels/ChangeHistory.cs | 43 | Notify read-only observers after completed Undo/Redo without requesting history tokens. |
| src/HexManiac.Core/ViewModels/EditorViewModel.cs | 29 | Persist the Map/Hex preference in app settings while keeping Hex as the default. |
| src/HexManiac.Avalonia/Project/ScriptMapSnapshot.cs | 14 | Capture live data once and render a bounded detached model/history into immutable reference pixels off-thread. |
| src/HexManiac.Avalonia/Controls/ScriptMapReferenceView.cs | 20 | Display the named map with drag/scroll panning and no tile, event, tooltip or context-menu editing. |
| src/HexManiac.Avalonia/Controls/TabView.ScriptMap.cs | 20 | Manage lazy snapshots, source focus, per-tab/map pan offsets, invalidation, resource release and stale-result rejection. |
| src/HexManiac.Avalonia/Controls/TabView.axaml | 816 | Add compact Hex View/Map View controls and replace the complete right-hand hex section when selected. |
| src/HexManiac.Avalonia/Controls/TabView.axaml.cs | 75 | Connect map-reference lifecycle handling when the actual tab control is created. |
| src/HexManiac.Avalonia/Windows/MainWindow.Editing.cs | 14 | Restore script focus and prevent clipboard/delete or palette shortcuts from reaching hidden hex controls. |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs | 92 | End reference panning on Escape and restore source focus instead of hidden hex focus. |
| tests/ScriptMapReference/Program.cs | 1 | Exercise read-only rendering, real map routes/buttons, pending/rejected source, panning, settings, stale frames, keyboard isolation and history with 49 focused assertions. |
| tests/ScriptMapReference/ScriptMapReference.csproj | 1 | Reuse fabricated map/workflow data and offscreen controls without personal ROMs or preferences. |
| tests/ScriptMapReference/README.md | 1 | Explain coverage, synthetic inputs and remaining native/real-ROM verification. |
| tests/script-map-reference/mac-verification.json | 1 | Preserve the complete 64-group run and distinguish its 48 map assertions from the final focused follow-up. |
| tests/script-map-reference/final-input-checks.json | 1 | Record all eight final input/focus groups and 404 assertions after the last Command-G guard. |
| tests/SCRIPT_MAP_REFERENCE_REVIEW.md | 88 | Add the implemented boundaries and verification links to the separately committed investigation. |
| README.md | 65 | List the map-reference scripting feature in Mac Edition's feature summary. |
| docs/MAC_SETUP.md | 24 | Explain how to open a source map, switch views and pan without implying movement simulation. |
| docs/MAC_RELEASE_CHECKLIST.md | 59 | Add a native candidate check for the new reference pane and editing/history workflow. |
| tests/README.md | 38 | Link the new regression group from the Mac test index. |

Validation: The complete serial Mac suite passes 64 groups / 18,224 assertions with zero failures and zero skipped groups, including 48 map-reference assertions. A final Command-G/palette-routing guard adds one more map assertion; that 49-check group plus seven freshly rebuilt related groups pass 404 assertions across eight groups, with zero failures/skips. These overlapping totals are recorded separately, not added together. Offscreen layout was rendered and inspected; app/focused builds emit zero compiler warnings/errors, with the existing MapImageExport NU1900 feed diagnostic separate. Synthetic snapshot preparation takes about 32–36 ms in these observations, not a real-ROM/native performance claim. Compiler, numeric grammar, pointer relocation and history algorithms are unchanged; only read-only history/refresh notifications are added. No personal ROM, metadata, settings or runtime migration was used. Native tutorial-map appearance and trackpad feel await user testing; the pre-feature build remains available on GitHub at checkpoint/pre-script-map-preview-2026-11-05 (68ed0f91).

## 155 — Add a light tile grid to the script-side map reference

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/ScriptMapReferenceView.cs | 25 | Add a Grid checkbox and input-transparent translucent 16-pixel overlay in the image's scrolling coordinate space without modifying map pixels. |
| src/HexManiac.Avalonia/Controls/TabView.ScriptMap.cs | 41 | Synchronize the grid preference without regenerating a snapshot or changing source focus. |
| src/HexManiac.Core/ViewModels/EditorViewModel.cs | 31 | Persist the optional grid in app settings with an off default. |
| tests/ScriptMapReference/Program.cs | 140 | Cover the real checkbox, geometry/input isolation, unchanged bitmap/model state, rendered line spacing/alpha and preference reload. |
| tests/ScriptMapReference/README.md | 15 | Describe grid rendering and settings preservation checks. |
| tests/script-map-reference/grid-verification.json | 1 | Record 57 map-reference and 47 isolated startup checks with native/full-suite limits. |
| docs/MAC_SETUP.md | 30 | Explain the Grid checkbox and remembered display-only behavior. |

Validation: Fresh app/test builds emit zero warnings/errors; ScriptMapReference passes 57 assertions and FirstLaunch passes 47, totaling 104 assertions across two groups with zero failures/skips. Offscreen layout was rendered and inspected; actual pixel checks distinguish 16-pixel map boundaries from 8-pixel graphics boundaries and verify translucent lines. Toggling reuses the same bitmap, preserves script focus and never invokes map snapshot preparation or changes live ROM/metadata. The full Mac suite was not rerun for this bounded visual/settings addition; native appearance awaits user confirmation. No personal ROM/settings, compiler, history or numeric behavior changed.

## 156 — Keep syntax-coloured source readable while selected

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/TextEditor.axaml | 28 | Replace the opaque native selection with a theme-accent brush at 28% opacity and transparent native selection foreground so syntax layers remain visible. |
| tests/DialogueDiagnostics/Program.cs | 96 | Check selected dialogue pixels, translucent highlighting and unchanged source/selection/model state alongside existing diagnostic rendering checks. |

Validation: The baseline fails the translucency regression; the updated DialogueDiagnostics group passes 30 assertions with zero build warnings/errors. Rendered selected dialogue retains coloured glyphs, and actual selected text, source, ROM bytes and metadata remain unchanged. This affects the shared layered editor only; ordinary short-field selection, compilation, numeric parsing and history algorithms are unchanged. Native appearance still benefits from user confirmation.

## 157 — Keep accepted movement suggestions dismissed

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs | 31 | Suppress reentrant and queued text notifications for the accepted target/context/result while allowing genuinely changed queries and clearing markers when context detaches or changes. |
| tests/AutocompleteMapping/Program.cs | 169 | Reproduce mouse-accepted walk_right/walk_down popups reopening and protect dismissal, fresh queries and LF/CRLF movement source. |
| tests/AutocompleteMapping/README.md | 6 | Record 310 checks and movement acceptance coverage. |
| tests/script-editor-polish/mac-verification.json | 1 | Preserve exact results for nine rebuilt shared-editor, drafting, clipboard, encounter and map-reference groups. |

Validation: Before the repair, a real mouse click accepting a movement instruction reopens its popup during layout; after the repair all 310 mapping assertions pass, including explicit queued notification and fresh-query controls. The focused final run passes nine groups / 1,928 assertions with zero failures or skips: DialogueDiagnostics 30, AutocompleteMapping 310, AutocompleteDismissal 85, DialogueScaffolding 384, ScriptAssistance 258, ClipboardRouting 78, WildEncounterEditing 128, ScriptMapReference 57 and ScriptWorkflowSafety 598. Fresh builds report zero warnings/errors. Full Mac/upstream suites were not rerun for these bounded overlay/visual changes. No personal ROM/settings or compiler, matching, numeric, relocation or history rules changed; native confirmation remains useful.

## 158 — Investigate combined tour-guide scripting workflows

| File Name | Line | Summary |
| --- | --- | --- |
| tests/TOUR_GUIDE_SCRIPT_REVIEW.md | 1 | Record mixed-resource coverage gaps, a reproduced inline-resource formatting failure and a separately scoped permissive movement-input finding before production changes. |
| tests/TourGuideWorkflow/Program.cs | 1 | Add a synthetic six-resource tour regression and optional minimal formatting/invalid-movement observation probes. |
| tests/TourGuideWorkflow/TourGuideWorkflow.csproj | 1 | Reuse the controlled script workflow fixture without user ROMs or preferences. |

Validation: Fresh build succeeds without compiler warnings/errors. The pre-fix mixed-tour run fails because correctly encoded dialogue has NoInfoRun formatting after successful compilation; the smaller single-message probe reproduces it and a second formatting pass restores PCS. The invalid movement probe separately observes silent omission for auto streams and zero substitution for existing streams. This evidence commit intentionally precedes the formatting repair; neither finding is attributed to recent map-reference work or counted as a passing regression. Numeric/resource grammar is unchanged.

## 159 — Preserve inline resource formatting in compiled script spans

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/Models/Code/ScriptParser.cs | 294 | Skip exact known resource spans while formatting a minimum compiled span, preserving inline dialogue/movement formats without treating data bytes as opcodes. |
| tests/TourGuideWorkflow/Program.cs | 1 | Protect six-resource allocation/growth, named relocation, failure/correction, navigation/callbacks, history/save and reachable following sections with 321 synthetic assertions. |
| tests/TourGuideWorkflow/README.md | 1 | Explain canonical complete-anchor comparison, reproducible commands and native/gameplay limits. |
| tests/ScriptAssistance/Program.cs | 21 | Extend real typing/Enter/click coverage to move.npc/applymovement actor operands and resource Undo/Redo, totaling 352 assertions. |
| tests/ScriptMapReference/Program.cs | 250 | Check movement-suggestion acceptance and pending-edit panning, pixel reuse, compilation and Undo in the actual map pane, totaling 63 assertions. |
| tests/TourGuideWorkflow/mac-verification.json | 1 | Record all 65 freshly built Mac groups and exact assertion totals. |
| tests/TourGuideWorkflow/upstream-baseline-summary.json | 1 | Preserve the pre-repair matched-host upstream baseline and its one outdated rejected-source assertion. |
| tests/TourGuideWorkflow/upstream-repair-summary.json | 1 | Preserve identical upstream totals/failure after the formatting repair. |
| tests/TOUR_GUIDE_SCRIPT_REVIEW.md | 59 | Record the repair, verified scope, upstream comparison and fresh-runner dependency limitation. |
| FutureDevelopment.md | 3 | Track the independently reproduced permissive invalid-movement behavior as HMA-019, without changing enum/numeric grammar here. |
| tests/README.md | 98 | Link the combined workflow group and its review; update related group READMEs. |

Validation: Full Mac suite passes 65 groups / 18,673 assertions with zero failures/skipped groups. Builds succeed; the pre-existing MapImageExport NU1900 feed warning remains separate. The upstream first-project run and a matched-host pre-repair baseline each report 2,715 cases: 1,779 passed, one identical failure, 935 skipped, zero engine errors. The sole failing legacy assertion expects navigation to discard rejected source, contrary to HMA-006; its contract is aligned separately next. The baseline comparison reused a working identical-version runner dependency manifest after fresh-archive discovery failed on the omitted xUnit runtime dependency. No ROM/settings files, allocation rules, numeric parsing, compiler opcodes or history algorithms were changed. The initial upstream comparison picked up an existing cached optional FireRed fixture; the isolated fixture-free follow-up is recorded below. Native/emulator checks and HMA-019 remain explicit limitations.

## 160 — Align the legacy navigation test with retained script drafts

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Tests/Before_Baseclass/ScriptTests.cs | 446 | Require navigation to retain rejected source without changing compiled bytes, then require explicit Undo to discard the draft without undoing the compiled model. |
| tests/TourGuideWorkflow/upstream-romfree-baseline-summary.json | 1 | Record the same outdated assertion failing on pre-repair code with all optional ROM fixtures excluded. |
| tests/TourGuideWorkflow/upstream-romfree-final-summary.json | 1 | Record the complete isolated first-project upstream pass with explicit absent-fixture skips. |
| tests/TOUR_GUIDE_SCRIPT_REVIEW.md | 88 | Distinguish fixture-bearing initial results, fixture-free final runs and runner-dependency limits. |
| tests/UPSTREAM_BASELINE.md | 9 | Link the new test-contract alignment and exact isolated upstream results. |

Validation: With sampleFiles excluded, the pre-repair matched host reports 2,715 cases: 1,666 passed, one legacy failure, 1,048 skipped and zero engine errors. After this test-only alignment, the isolated final run reports 2,715 cases: 1,667 passed, zero failed, 1,048 skipped and zero engine errors. The skips require absent optional ROMs. Initial upstream runs had picked up an existing cached FireRed fixture and therefore had different pass/skip totals; those summaries remain preserved and are not called ROM-free. The cached fixture still hashes identically to the repository's existing sample; no ROM/settings files were edited. The known fresh-runner xUnit dependency omission is documented, and the existing xUnit1013 analyzer warning remains separate. The production tree is unchanged from entry 159's full 65-group / 18,673-assertion Mac pass; a redundant full Mac rerun was not required for this upstream-test-only edit. Evidence, production repair and legacy-test alignment remain separate commits on the unmerged branch.

## 161 — Keep suggestion truncation markers out of source

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/AutoCompleteSelectionItem.cs | 10 | Expose whether a completion is a real value rather than the list's ellipsis marker. |
| src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml | 45 | Display truncation indicators as disabled rows instead of clickable completions. |
| src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs | 279 | Skip disabled markers during keyboard wrapping and reject marker acceptance even when a Click is raised programmatically. |
| tests/AutocompleteMapping/Program.cs | 193 | Cover the real FireRed movement list's disabled marker, guarded Click, Up/Down wrapping and Enter acceptance across LF/CRLF. |
| tests/AutocompleteMapping/README.md | 6 | Record the expanded 320-assertion group and indicator behavior. |

Validation: The new disabled-row assertion fails before the fix. The repaired group passes 320 assertions with zero build warnings/errors, including existing mouse acceptance, caret mapping, queued-notification dismissal and history checks. No ROM fixtures/preferences or movement compiler/matching/numeric rules changed. The full shared-control regression run follows the separately scoped movement-validation change.

## 162 — Reject unrecognized movement rows without partial compilation

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/Models/Code/ScriptParser.cs | 611 | Validate movement rows using the existing deferred-enum or existing-table syntax and report the exact unrecognized source span inside the existing atomic compilation transaction. |
| tests/MovementValidation/Program.cs | 1 | Exercise 1,981 assertions for real FireRed names, four commands, two resource paths, three newline styles, precise diagnostics, rollback, persistence/navigation, relocation, correction, Undo/Redo and numeric compatibility. |
| tests/MovementValidation/MovementValidation.csproj | 1 | Reuse the controlled fabricated-ROM fixture and actual metadata resources in an automatically discovered Mac regression group. |
| tests/MovementValidation/README.md | 1 | Explain reproduction, scope, legacy parsing differences and native/gameplay limits. |
| tests/MovementValidation/mac-verification.json | 1 | Preserve all 66 full-suite group counts and the tested parser hash. |
| tests/MovementValidation/upstream-verification.json | 1 | Preserve the complete isolated first-project upstream results with absent optional-ROM skips. |
| tests/ScriptAssistance/Program.cs | 23 | Load actual FireRed movement metadata and verify the compiled walk_up byte/terminator after typing, keyboard and mouse assistance, totaling 364 assertions. |
| tests/ScriptAssistance/README.md | 22 | Record the strengthened real-catalog assistance coverage. |
| FutureDevelopment.md | 5 | Mark HMA-019 fixed for script source while retaining the original investigation evidence and standalone-table scope limit. |
| tests/TOUR_GUIDE_SCRIPT_REVIEW.md | 31 | Link the implemented movement-validation and disabled-marker follow-ups without rewriting the original findings. |
| tests/README.md | 101 | Link the new validation coverage and distinguish the earlier unimplemented checkpoint. |

Validation: The first unknown-row rejection assertion fails before implementation; 219 valid-input compatibility assertions pass on that baseline. The final expanded MovementValidation group passes 1,981 assertions. Full Mac verification passes 66 groups / 20,676 assertions with zero failures/skipped groups, including AutocompleteMapping 320, ScriptAssistance 364, TourGuideWorkflow 321, ScriptWorkflowSafety 598 and ScriptMapReference 63. The isolated upstream first-project run discovers 2,715 cases: 1,667 passed, zero failed, 1,048 absent optional-ROM skips and zero engine errors; its copied working dependency host explicitly excludes sampleFiles/ROM files. Existing MapImageExport NU1900 and NamedByteTests xUnit1013 warnings are separate from successful builds. No personal ROM/settings, numeric grammar, movement bytes, allocator, table writer or history algorithms changed. Known names, partial/quoted aliases, comments, numeric wrapping and existing terminator handling retain their previous behavior; raw out-of-range compatibility checks are not a gameplay-validity guarantee. Standalone table editing and non-movement resources are not tightened. Native/editor/emulator checks remain user validation. Marker and compiler fixes are separate commits on the unmerged branch.

## 163 — Measure script map snapshots and open-document tab retention

| File Name | Line | Summary |
| --- | --- | --- |
| tools/ScriptMapMemoryBenchmark/Program.cs | 1 | Measure synthetic snapshot/display/clear, concurrent work, actual Map/Hex tab switches, detached-control ownership, window closure and individual document closure using post-GC samples and weak references. |
| tools/ScriptMapMemoryBenchmark/ScriptMapMemoryBenchmark.csproj | 1 | Reuse the actual renderer/UI and fabricated map fixture in a separate Release benchmark. |
| tools/ScriptMapMemoryBenchmark/README.md | 1 | Document repeatable commands, private-field diagnostics, cleanup-experiment scope and managed-versus-native memory limits. |
| tests/SCRIPT_MAP_MEMORY_REVIEW.md | 1 | Distinguish healthy snapshot ownership from reproduced general tab retention and define a bounded lifecycle follow-up. |
| tests/script-map-memory/baseline.json | 1 | Preserve the initial complete Map/Hex comparison and per-size snapshot measurements. |
| tests/script-map-memory/cleanup-probe.json | 1 | Preserve repeated measurements, source-byte checks and the insufficient controlled cleanup experiment. |
| tests/script-map-memory/close-documents.json | 1 | Record zero retained watched views/bitmaps/documents/models after closing four documents while another remains open. |
| FutureDevelopment.md | 3 | Track HMA-020 as reproduced offscreen open-document retention, with production lifecycle changes explicitly pending. |

Validation: Release benchmark builds without warnings/errors. Across eight display/clear cycles at 80×52, 128×128 and 256×256 tiles, no watched snapshots, pixel arrays or bitmap wrappers survive collection; final settled heap is within 0.02 MiB of the warmed baseline. Four concurrent renders at the largest supported preview size transiently add about 400 MiB of sampled managed heap, then release all watched payloads. Repeated actual switches retain 12 watched distinct detached TabViews and grow settled managed heap similarly in Hex and Map modes (about 96 to 255 MiB versus 94 to 249 MiB). A document event exposes stale TabView handlers, but clearing that path does not release every control: remaining owners are not yet isolated and no partial production fix is claimed. Individual document close and complete-window teardown release all watched closed objects. Source bytes remain unchanged; detached render phases also preserve serialized metadata/saved/history state. No production map code or personal ROM/preferences changed; offscreen synthetic managed-memory observations do not certify native RAM, real-ROM timing or new-Mac behavior.

## 164 — Warn slightly earlier near the in-game dialogue edge

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Tools/CodeBody.cs | 15 | Lower the shared advisory dialogue-width budget from 214 to 208 pixels, leaving a six-pixel cushion without changing encoding or wrapping. |
| tests/DialogueDiagnostics/Program.cs | 81 | Protect earlier rendered warnings, clearing below the margin, exact proportional-font boundaries, advisory status and unchanged source/ROM/metadata across LF/CRLF; the group now passes 40 assertions. |
| tests/DialogueDiagnostics/README.md | 23 | Describe the conservative pixel budget, estimator limits and focused verification scope. |
| tests/dialogue-warning-margin/mac-verification.json | 1 | Record exact counts for nine freshly built diagnostic/layout/script/map/NPC groups. |

Validation: The new 210-pixel dialogue-row assertion fails at the original 214-pixel budget. The updated warning is visible at 210 pixels, clears at 204 pixels and respects an exact 208-pixel proportional-font boundary; it does not become a compile error or rewrite source. Fresh DialogueDiagnostics 40, ScriptWidthUpdates 448, ScriptEditorLayout 102, ScriptWorkflowSafety 598, DialogueScaffolding 384, ScriptMapReference 63, MapEventEditing 63, MapHistoryRefresh 23 and NpcTemplate 44 pass 1,765 assertions across nine groups with zero failures/skips and no build warnings/errors. The complete Mac/upstream suites were not rerun for this bounded advisory change. Existing Enter/scroll/paragraph bytes, compilation, numeric rules, resources/pointers, history, focus and rejection/persistence protections remain covered. No personal ROM/settings or production map-lifecycle fix is included; native in-game font/control behavior and dynamic substitutions still need the user's exact example if the warning remains inaccurate.

## 165 — Isolate open-document tab retention before repairing it

| File Name | Line | Summary |
| --- | --- | --- |
| tests/TabViewLifetime/Program.cs | 1 | Require detached actual Map/Hex tab controls to collect while documents stay open, with source/history and reattachment protections. |
| tests/TabViewLifetime/README.md | 1 | Record the first-switch baseline failure and synthetic/native verification limits. |
| tools/ScriptMapMemoryBenchmark/RetentionTrace.cs | 1 | Trace bounded strong event owners without changing app state or traversing ROM bytes. |
| tools/ScriptMapMemoryBenchmark/Program.cs | 56 | Add a lifecycle-only comparison mode and optional ownership diagnostics. |
| tests/tab-lifecycle-memory/before.json | 1 | Preserve pre-repair lifecycle measurements separately from the earlier warmed full corpus. |
| tests/SCRIPT_MAP_MEMORY_REVIEW.md | 81 | Record the failing collection check and the additional header/filter/image/palette event roots. |

Validation: Before production edits, the new collection assertion fails on the first Hex-only switch with all four watched controls alive. The paired synthetic baseline retains 12 detached watched tabs and 24 stale TabView handlers in both modes. Removing just the general tab/header subscriptions exposes further filter and hidden image/palette observers; detach-only cleanup cannot cover controls that never attached. Evidence and implementation are separate commits. No app rendering/compiler/history algorithm or personal ROM/settings is changed by this evidence commit. Reattachment assertions and after measurements are recorded for the following repair's verification.

## 166 — Release old tab controls without changing Map View rendering

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/TabView.axaml.cs | 78 | Balance document observers and cursor timer on attachment/removal while retaining source and stored scroll state. |
| src/HexManiac.Avalonia/Controls/HexContent.cs | 61 | Track and release exact document/header/request-menu subscriptions and restore them on reuse. |
| src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs | 138 | Balance model, diagnostic, caret and focus observers without settling or clearing script drafts. |
| src/HexManiac.Avalonia/Controls/HorizontalSlantedTextControl.cs | 39 | Replace anonymous header-item callbacks with removable listeners covering deleted/reset/replaced rows. |
| src/HexManiac.Avalonia/Controls/AngleComboBox.cs | 178 | Observe filter models only while attached and invalidate detached selection-restoration callbacks. |
| src/HexManiac.Avalonia/Controls/TileImage.cs | 255 | Restore pixel observers from current data when attached and release them when detached. |
| src/HexManiac.Avalonia/Controls/SelectionRender.cs | 26 | Balance image-selection model subscriptions across attachment and context changes. |
| src/HexManiac.Avalonia/Controls/PaletteControl.axaml.cs | 65 | Avoid collection observers in never-attached hidden palettes and restore them exactly once on reuse. |
| tests/MapRendering/Program.cs | 15 | Attach live map/sprite controls to headless windows while preserving all 93 rendering assertions. |
| tests/ScriptEditorLayout/Program.cs | 15 | Exercise live diagnostic coalescing on attached editors while preserving all 102 layout assertions. |
| tests/tab-lifecycle-memory/after.json | 1 | Preserve the repaired lifecycle-only benchmark with zero old watched tabs and unchanged source bytes. |
| tests/tab-lifecycle-memory/full-after.json | 1 | Preserve the complete snapshot/burst/switch/close corpus with zero watched retained payloads/documents. |
| FutureDevelopment.md | 3 | Mark HMA-020 repaired in offscreen verification and retain native/real-ROM validation limits. |

Validation: The new lifetime group passes 41 assertions, including actual Map/Hex detached-control collection, never-attached hidden panels, repeated listener restoration, selection/caret/focus, pending callbacks, retained rejected source/diagnostics and exact Undo/Redo states. The first full run stopped at MapRendering's never-attached live-zoom fixture; MapRendering and ScriptEditorLayout now attach their subjects to headless windows without removing assertions (93 and 102 respectively). A fresh complete `scripts/verify-mac.sh --no-restore` passes **67 groups / 20,727 assertions / zero failures / zero skipped groups**; per-group counts are in `tests/tab-lifecycle-memory/mac-verification.json`. The existing MapImageExport NU1900 vulnerability-index warning remains; the Release benchmark build has zero warnings/errors. Both paired lifecycle modes go from 12 retained old watched tabs and 24 stale tab handlers to zero. Settled managed heap is about 226→61 MiB in Hex and 212→62 MiB in Map for the same lifecycle-only workload. The full snapshot/burst/close corpus also releases every watched payload, old view and closed document/model. This is a retained managed-memory repair, not a rendering-speed/native-RAM claim. No forced app GC, DataContext clearing, source rewriting, map rendering/read-only handler changes, compiler/numeric/history algorithms or personal ROM/settings changes. Full Mac coverage includes the earlier six-pixel advisory dialogue margin. Upstream xUnit was not rerun for this Avalonia-only repair; native Cocoa/Retina/real-ROM and new-Mac behavior remain user verification. Branch remains unmerged.

## 167 — Reproduce editor content hidden by overlay scrollbars

| File Name | Line | Summary |
| --- | --- | --- |
| tests/EditorScrollbars/Program.cs | 1 | Check actual text/panel scroll templates for non-overlapping gutters, stable hover/resize geometry, right-edge clicks and retained source/selection. |
| tests/EditorScrollbars/EditorScrollbars.csproj | 1 | Run offscreen Avalonia/Skia layout and input checks independently of personal ROMs/settings. |
| tests/EditorScrollbars/baseline.json | 1 | Record the first non-overlap assertion failing on merged checkpoint ccb765fa before production edits. |
| tests/EditorScrollbars/README.md | 1 | Explain the controlled production-viewer hosts and distinguish layout checks from core map/script workflows and native trackpad checks. |

Validation: On ccb765fa, the group builds without warnings/errors and fails at the first script-editor viewport assertion: content extends beneath a scrollbar. The installed Fluent template spans content underneath its scrollbar columns when auto-hide is enabled. The left editors inherit that behavior; the port's custom TextBox template additionally omits forwarding AllowAutoHide to its inner ScrollViewer. Evidence and production repair are separate commits. No personal ROM/settings, compiler/numeric/history behavior or production code changes are included in this evidence commit.

## 168 — Reserve scrollbar space throughout the left editor panels

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 161 | Give block, event, map-header and tile viewers stable scrollbar gutters instead of hover overlays. |
| src/HexManiac.Avalonia/Controls/TabView.axaml | 231 | Reserve scrollbar space in table, text, image, multi-script and log tools and the associated map-preview list. |
| src/HexManiac.Avalonia/Resources/GeneralResources.axaml | 172 | Forward the text box's AllowAutoHide setting into its custom inner ScrollViewer. |
| src/HexManiac.Avalonia/Controls/TextEditor.axaml | 19 | Name the syntax-paint canvas and disable overlay scrollbars in the transparent editing text box. |
| src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs | 257 | Clip syntax paint to the actual text viewport after scroll, viewport and queued width updates without changing source or glyph layout. |

Validation: The new EditorScrollbars group passes 78 assertions, including both scrollbar directions in the real script editor, exact syntax viewport clipping, stable hover/resize geometry, unchanged source/selection and clickable right-edge buttons in seven production panel viewer instances at 220/420 widths. The baseline fails its first non-overlap assertion before the repair. Fresh `scripts/verify-mac.sh --no-restore` on the transferred Apple Silicon Mac passes **68 groups / 20,805 assertions / zero failures / zero skipped groups**; exact counts and any build warnings are preserved in `tests/EditorScrollbars/verification.json`. Existing map selection/tiling/zoom, event editing/navigation, clipboard, autocomplete, diagnostics, script layout/width, lifecycle retention, pending/rejected source, Save and Undo/Redo protections all pass. The patch uses existing template properties and clipping only: no scrollbar input handlers, compilation/numeric/resource/history algorithms, Map View interaction code or personal ROM/settings changed. Native trackpad feel and real-ROM panel appearance await the user's relaunch. Evidence and implementation remain separate commits on the unmerged feature branch.

## 169 — Reproduce the stale disabled object script arrow

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ObjectScriptNavigation/Program.cs | 1 | Exercise the actual object arrow and command through pointer/text changes, correction, script creation, NPC templates and model Undo/Redo. |
| tests/ObjectScriptNavigation/baseline.json | 1 | Record the existing button remaining disabled after a valid address is assigned on checkpoint 260e078d. |
| tests/ObjectScriptNavigation/README.md | 1 | Distinguish captured navigation-target checks from complete tab/focus behavior and preserve existing history selection semantics. |

Validation: The regression builds without warnings/errors and fails on the first null-to-valid assignment's actual enabled-state check before production edits. A separate diagnostic probe finds CanExecute=true while the visible arrow remains disabled, pointer clicks deliver no navigation, and reselecting the object restores both enabled state and click delivery. Missing CanGotoScript notifications are in older object setters and are independent of the scrollbar layout repair. No production code or personal ROM/settings changes are included in this evidence commit.

## 170 — Refresh object script navigation after address changes

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Map/IEventViewModel.cs | 752 | Notify CanGotoScript after programmatic object ScriptAddress assignment so the existing arrow refreshes without reselection. |
| src/HexManiac.Core/ViewModels/Map/IEventViewModel.cs | 804 | Publish the same dependent-state notification after ScriptAddressText edits, including clearing and correction. |
| tests/ObjectScriptNavigation/verification.json | 1 | Record exact fresh results for eleven focused object/map/script/scrollbar/lifetime groups. |

Validation: ObjectScriptNavigation passes 51 assertions; the unchanged same button enables after null-to-valid assignment, routes real pointer clicks to the current navigation target, disables after clearing/out-of-ROM assignment and recovers after correction. Existing Create Script/NPC-template paths refresh immediately; map Undo/Redo restores exact model bytes and assignment metadata, with the object panel reopened after its existing selection-clearing refresh. Fresh ObjectScriptNavigation 51, NpcTemplate 44, MapEventEditing 63, MapHistoryRefresh 23, MapEventMenus 68, ScriptWorkflowSafety 598, EditorScrollbars 78, TabViewLifetime 41, ScriptMapReference 63, ScriptWidthUpdates 448 and DialogueScaffolding 384 pass **11 groups / 1,861 assertions / zero failures / zero skips**, with zero build warnings/errors. The full Mac and upstream xUnit suites were not rerun for these two dependent-property notifications; the preceding scrollbar repair's complete run remains 68 groups / 20,805 assertions. No command implementation, parsing/numeric rules, pointer allocation/relocation, compilation, history algorithm, ROM fixture or personal settings changed. Captured target callbacks do not certify complete native tab/focus behavior; real-ROM relaunch remains user verification. Evidence and repair are separate commits on the unmerged scrollbar feature branch.

## 171 — Reproduce assistance fighting pointer edits and disappearing on later commands

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ScriptAssistanceEntry/Program.cs | 1 | Exercise real multi-section editor input through typing/Enter/mouse completion, whitespace and prior pointer edits, then require exact manual edits and stale-callback cancellation. |
| tests/ScriptAssistanceEntry/baseline.json | 1 | Preserve the pre-repair failure where deleting an operand immediately reinserts auto. |
| tests/ScriptAssistanceEntry/README.md | 1 | Document the 72-case source/input matrix, later coverage additions and deferred-source validation boundaries. |

Validation: On checkpoint 71f4abc7 the initial regression builds without warnings/errors and fails at the first pointer deletion: unconditional assistance puts auto back. Prior isolated input probes also reproduce trailing-space rejection through all three entry routes and InsertAutoActive remaining false after deleting > from an earlier auto token. The reported second section and preceding movement blocks work in otherwise normal conditions. Evidence and production repair remain separate commits. Tests use synthetic files only; no app code, user ROM/settings or compiler/numeric/history algorithms change in this evidence commit.

## 172 — Offer initial resource assistance while preserving later pointer edits

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Tools/CodeBody.cs | 286 | Separate assistance planning from application, accept trailing whitespace, and distinguish initial command entry from operand edits/deletion. |
| src/HexManiac.Core/ViewModels/Tools/CodeTool.cs | 362 | Remove automatic global suppression toggles and plan help using the actual source edit. |
| src/HexManiac.Core/ViewModels/Tools/CodeTool.cs | 422 | Apply planned source/caret assistance after native input settles, guarded by compile generation, body/address/source/draft identity and explicit assistance preference. |
| src/HexManiac.Avalonia/Controls/AutocompleteOverlay.axaml.cs | 351 | Let physical Space supply the generated separator for script completion while preserving whitespace already in source. |
| tests/ScriptAssistance/Program.cs | 71 | Include the exact command/input route in failures without changing the existing assistance assertions. |

Validation: ScriptAssistanceEntry passes 789 assertions across 72 real-input cases and explicit stale-callback/caret-bound checks. LF/CRLF, typed/Enter/mouse completion, msgbox.npc/preparemsg/move.player, trailing spaces/tabs and prior auto-pointer editing all receive initial help. The command name alone waits for its separator; explicit addresses, deletion and later operand whitespace remain exactly as entered, while future commands still receive help. Planned insertion is source-only until native input settles and cannot overwrite a newer source edit, survive Undo/navigation, target another body/address or write ROM directly. The first full run exposed the typed msgbox alias's preinserted separator; the overlay compatibility repair omits only that generated separator and preserves existing whitespace. Legacy ScriptAssistance still passes all 364 assertions. A fresh complete `scripts/verify-mac.sh --no-restore` passes **70 groups / 21,645 assertions / zero failures / zero skipped groups**, including compilation/resource/pointer/relocation, rejected drafts, Save/Close, Undo/Redo, Map View, clipboard, completion dismissal and control lifetime regressions. Exact counts and build warnings are in `tests/ScriptAssistanceEntry/verification.json`; the existing MapImageExport NU1900 vulnerability-index warning remains. No compiler, allocator, numeric grammar, game-engine or history algorithm changed, no personal ROM/settings were opened, and no new timer or preference UI was introduced. Existing explicit opt-outs and Spartan mode remain respected; editing a pointer never flips them. Goto/call gain no auto syntax. Native real-ROM testing remains user validation. Evidence and implementation are separate commits on the unmerged feature branch.

## 173 — Investigate safe event navigation from the script-side map

| File Name | Line | Summary |
| --- | --- | --- |
| tests/SCRIPT_MAP_EVENT_NAVIGATION_REVIEW.md | 1 | Preserve the rollback checkpoint, read-only live-pointer probe, navigation/draft/relocation design and the inline-help click interception found during actual-window testing. |

Validation: The synthetic checkpoint probe preserves live bytes, metadata, free-space/history and saved state for object/script-event reads, then resolves an updated pointer rather than a cached address. Checkpoint 7a6c80ec and its pushed annotated pre-feature tag preserve the previously merged build. This evidence commit documents the investigation separately from production implementation; no personal ROM or settings are used. Final implementation and complete regression results follow in the next entry.

## 174 — Open event scripts directly from the script-side map

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Project/ScriptMapSnapshot.cs | 53 | Copy immutable object/script-event hit bounds and IDs from the detached render without retaining its model or event editors. |
| src/HexManiac.Avalonia/Project/ScriptMapEvent.cs | 12 | Validate current map/row identity and resolve live pointers and sprite IDs through read-only accessors. |
| src/HexManiac.Avalonia/Controls/ScriptMapReferenceView.cs | 35 | Add click-versus-pan handling, read-only event details and an overlapping-event chooser with stale-action cancellation. |
| src/HexManiac.Avalonia/Controls/TabView.ScriptMap.cs | 131 | Settle pending source, block rejected drafts with an inline red notice, reread relocated pointers and navigate in the same script tab while protecting focus and hidden hex input. |
| src/HexManiac.Avalonia/Controls/TabView.ScriptMap.cs | 35 | Let map gestures pass through inline command help so it cannot consume the first navigation click. |
| tests/MapRendering/SyntheticMapFixture.cs | 51 | Support combined NPC-text and script-event synthetic fixtures without erasing the latter's table format. |
| tests/ScriptMapNavigation/Program.cs | 25 | Cover actual pointer/chooser/focus input, readonly state, rejected source, relocation, correction/retry, Undo/Redo, stale records and control cleanup. |
| tests/ScriptMapNavigation/verification.json | 1 | Preserve every fresh regression group's assertion count, build warning and validation boundary. |
| tests/ScriptMapNavigation/lifetime-memory.json | 1 | Preserve actual Hex/Map tab-switch and window-close lifetime measurements. |
| README.md | 65 | Describe event-script navigation beside the existing Map View feature. |

Validation: A fresh complete `bash scripts/verify-mac.sh --no-restore` passes **71 groups / 21,686 assertions / zero failures / zero skipped groups**. New ScriptMapNavigation passes 41 assertions, including clicking while inline help is visible, exact rejected dialogue source/diagnostics/selection retention, stale callback cancellation, a click that settles and rereads its own relocated destination, one-step Undo/Redo restoration, large-sprite hit regions, null/shared pointers, a real overlapping-event menu selection and Command-V routed to source. Existing ScriptMapReference passes 63 assertions; the full run also covers HMA-006/007/008 workflow protections, save/close, source assistance, completion dismissal and control lifetime. Build output has only the existing MapImageExport NU1900 vulnerability-index lookup warning; app and focused builds have zero warnings/errors. The upstream xUnit suite was not rerun.

Release lifecycle measurements retain zero detached TabViews/old bitmap wrappers after actual tab switches in both Hex and Map modes, zero source-tab handlers, and zero of 27 watched controls after each window closes; source bytes remain unchanged. These are headless synthetic measurements, not native responsiveness or in-game claims. The map remains a detached image with navigation-only targets, no live map editing commands or tile selection. Pure navigation preserves bytes/metadata/history; pending valid source deliberately compiles through the existing compiler before navigation. No personal ROM/settings or core compilation, numeric, allocation, relocation, history or game-engine algorithms changed. Investigation and implementation are separate commits on the unmerged feature branch; the pushed pre-feature checkpoint tag remains available. Native real-ROM/trackpad/popup testing remains user verification after restart.

## 175 — Reproduce long script boxes jumping when clicked

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ScriptScrollStability/Program.cs | 1 | Exercise real pointer/caret/scroll input in the production script viewer, with a synthetic full-tab case protecting pending source and ROM state. |
| tests/ScriptScrollStability/SyntheticTourScript.txt | 1 | Preserve the reported two-section script's layout while replacing personal dialogue with neutral text. |
| tests/ScriptScrollStability/baseline.json | 1 | Record the checkpoint failure where clicking visible source moves the outer scrollbar from 55 to 5. |

Validation: On checkpoint 554b16db the regression builds with zero warnings/errors and fails at its first stability assertion, reproducing the jump without opening the user's ROM. An isolated focus experiment identifies the outer CodeToolMultiTextBox ScrollViewer's whole-control focus reveal as the cause; disabling that automatic reveal stabilizes initial clicks. The evidence commit precedes production repair; remaining interaction, caret-reveal and source/history assertions must still pass after the fix. Personal dialogue is excluded from tracked test fixtures, and no personal ROM/settings are opened or modified.

## 176 — Keep long script boxes steady when clicked

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/TabView.axaml | 847 | Disable whole-control focus reveal on the outer multi-script viewer so clicking visible source does not scroll an oversized editor to its edge. |
| src/HexManiac.Avalonia/Controls/TextEditor.axaml.cs | 210 | Correct the port's inaccurate focus-scrolling comment while preserving native caret reveal and the existing focus handler. |
| tests/ScriptScrollStability/Program.cs | 18 | Verify stable clicks, exact caret hits, selection drag, typing, keyboard/model caret reveal and pending-source/ROM safety across real production controls. |
| tests/ScriptScrollStability/SyntheticTourScript.txt | 33 | Keep neutral dialogue line widths without trailing-whitespace artifacts in the tracked fixture. |
| tests/ScriptScrollStability/verification.json | 1 | Preserve exact fresh focused-group counts and separate validation of the user's pasted source. |

Validation: ScriptScrollStability passes **195 assertions** with its neutral two-section fixture and another **195** when given the exact pasted script from an ignored local text file. LF/CRLF, 14/20-point text, two viewport sizes, unfocused/already-focused clicks, manual scroll, drag selection and exact clicked character positions pass. Arrow keys still reveal an offscreen caret; model-driven assistance can reveal both the beginning and end of a long source. A complete MainWindow/script-tab case preserves the exact pending source/debounce count, synthetic ROM bytes/metadata and saved/Undo state. The one behavioral change is the outer ScrollViewer focus setting; native caret bring-into-view remains enabled, and there is no scroll-restoration timer or blanket event suppression.

Fresh **13 groups / 3,266 assertions / zero failures / zero skipped groups**, with zero build warnings/errors, cover ScriptScrollStability 195, EditorScrollbars 78, ScriptMapReference 63, ScriptMapNavigation 41, ScriptWidthUpdates 448, ScriptEditorLayout 102, ScriptAssistance 364, ScriptAssistanceEntry 789, ClipboardRouting 78, ScriptWorkflowSafety 598, TabViewLifetime 41, AutocompleteDismissal 85 and DialogueScaffolding 384. ScriptScrollStability was rerun after adding explicit offscreen model-caret coverage; the totals use its final 195 checks. The complete Mac and upstream xUnit suites were not repeated for this one-setting repair; the preceding full Mac run was 71 groups / 21,686 assertions. No personal ROM/settings were opened or modified, and personal dialogue is excluded from committed fixtures. Compiler, numeric parsing, allocation, relocation, history, script semantics and game-engine behavior are unchanged. Evidence and repair are separate commits on the unmerged feature branch. Native trackpad/real-ROM UI confirmation remains user testing after restart.

## 177 — Navigate script signposts from the script-side map

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Project/ScriptMapEvent.cs | 10 | Resolve signpost kinds 0–4 through arg as a script pointer and expose other kinds only as informational non-script records. |
| src/HexManiac.Avalonia/Project/ScriptMapSnapshot.cs | 69 | Capture detached signpost hit bounds and kind alongside existing object/script targets. |
| src/HexManiac.Avalonia/Controls/TabView.ScriptMap.cs | 139 | Show hidden-item/secret-base status without settling source or attempting script navigation. |
| tests/ScriptMapNavigation/Program.cs | 140 | Exercise all script signpost kinds, pointer changes, rejected drafts, null/deleted/stale-kind targets and pointer-shaped non-script data. |

Validation: ScriptMapNavigation passes 71 assertions, up from 41; all five script-bearing signpost kinds open in the same left editor without live formatting writes. Hidden-item kinds 5–7, secret-base kind 8 and unsupported kind 255 retain the exact pending compile callbacks and source while displaying their purpose; their payload is never read as a pointer. Fresh live row counts and kind identity reject deleted/stale targets, and signpost clicks share the existing rejected-source protection. Build has zero warnings/errors. Only synthetic inputs are used; compiler, pointer tracking/relocation, numeric and history algorithms are unchanged. Full cross-feature verification follows after the requested UI changes.

## 178 — Focus and select source when entering through a signpost arrow

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 638 | Give the signpost arrow an explicit enabled navigation handler so its host is captured before the map detaches. |
| src/HexManiac.Avalonia/Controls/MapTab.axaml.cs | 89 | Focus and select the requested source box after navigation while rejecting stale destinations and open overlays. |
| tests/SignpostScriptNavigation/Program.cs | 55 | Verify real arrow input, first/reused script tabs, select-all, click-to-deselect and unchanged ROM state. |
| tests/ScriptMapNavigation/Program.cs | 153 | Verify Map View signpost navigation does not automatically select all source. |

Validation: SignpostScriptNavigation passes nine assertions with zero build warnings/errors; actual pointer input focuses the script and selects its contents on first and repeated arrow entry, while a normal source click clears the selection for incremental editing. ScriptMapNavigation passes 72 assertions including the separate Map View behavior. The source lookup targets the requested CodeBody rather than the last-focused address field; delayed focus checks the current model, selected address and overlay state. No personal ROM/settings, compiler, pointer relocation or history rules changed. Cross-feature verification follows after the requested visual fixes.

## 179 — Separate block identity and movement-permission controls

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 106 | Replace the shared fixed-height/margin arrangement with measured rows for Edit Block, permission controls, Block ID and optional nine-grid selection. |
| tests/BlockPanelLayout/Program.cs | 14 | Verify non-overlap, panel-width containment and working checkbox bindings across six width/font combinations. |

Validation: The production controls reproduce the overlap at 256px/14pt: automatic permission occupies (5,139,212,15), crossing Block ID at (191,137,65,18). The repaired layout passes 48 assertions at 256/320/512px and 14/20pt, with zero build warnings/errors. The permission label can wrap and the movement selector uses the available width; existing data bindings and drawing/permission behavior remain unchanged. This regression uses a layout-only model, no ROM. Full cross-feature verification follows after the remaining delete-icon adjustment.

## 180 — Center map-script delete icons

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 991 | Give both map-script and table-sub-script delete buttons a 24×18 hit area and a centered 8×8 square X. |

Validation: An ignored headless preview instantiates the actual parent/sub-script templates and renders both before and after. Previously each X used a 10×20 control at (5,-1) inside a 20×18 button; the taller-than-button control let its uniformly stretched ink sit too high. Both now use an 8×8 square at (8,5) in a 24×18 button, with zero center offset and no bounds overflow. Before/after images were visually inspected. Build has zero warnings/errors, existing Delete command bindings are unchanged, and no ROM is used. Complete regression results follow in the verification record.

## 181 — Verify signpost navigation and map-panel UI changes together

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ScriptMapNavigation/verification.json | 1 | Record all 74 fresh regression groups, their assertion counts, the four implementation commits, delete-icon geometry and validation boundaries. |
| tests/ScriptMapNavigation/README.md | 5 | Describe the current 72-check navigation coverage, including signpost unions and arrow-independent selection behavior. |
| README.md | 65 | Include script signposts in the script-side Map View feature description. |

Validation: Fresh complete `bash scripts/verify-mac.sh --no-restore` passes **74 groups / 21,969 assertions / zero failures / zero skipped groups** on implementation head 48180bb4. This includes ScriptMapNavigation 72, SignpostScriptNavigation 9, BlockPanelLayout 48, ScriptScrollStability 195, ScriptMapReference 63, ScriptWorkflowSafety 598, ScriptAssistanceEntry 789, ClipboardRouting 78 and TabViewLifetime 41, plus every existing map/rendering/history, input, save and packaging regression group. The only build warning is the existing MapImageExport NU1900 vulnerability-index lookup warning; no build errors occur. Actual parent/sub-script delete-icon templates were rendered before and after and visually inspected. The earlier navigation memory measurement remains historical; no fresh memory benchmark or native real-ROM gameplay claim is made. The full upstream xUnit suite was not rerun. No personal ROM/settings were used, and the four requested behavioral/visual changes remain separate commits. Native UI confirmation remains user testing after restart.

## 182 — Show Object ID and assigned flag in Map View

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Project/ScriptMapEvent.cs | 12 | Replace Sprite ID metadata with live object flags and label Object ID explicitly, showing unassigned flags as none. |
| src/HexManiac.Avalonia/Project/ScriptMapSnapshot.cs | 62 | Capture only the requested object identity/flag metadata alongside detached hit bounds. |
| tests/ScriptMapNavigation/Program.cs | 131 | Check assigned/removed flags from the current row without rebuilding the map or exposing Sprite ID. |

Validation: ScriptMapNavigation passes 74 assertions with zero build warnings/errors. Object details use the same four-digit hexadecimal flag notation as the editor, with an explicit 0x prefix; zero is shown as none. Flags are reread on click, while event identity guards and pure-navigation ROM/metadata/history protections remain unchanged. No personal ROM/settings are used.

## 183 — Show the event trigger variable in Map View

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Project/ScriptMapEvent.cs | 13 | Include the live trigger field as Variable Number in script-event details and chooser labels. |
| src/HexManiac.Avalonia/Project/ScriptMapSnapshot.cs | 68 | Capture the green event's trigger variable without changing its script or trigger semantics. |
| tests/ScriptMapNavigation/Program.cs | 76 | Verify changed variable numbers are shown on subsequent clicks without rerendering the map. |

Validation: ScriptMapNavigation passes 76 assertions with zero build warnings/errors. Variable Number uses the existing trigger editor's four-digit hexadecimal convention with a 0x prefix, including 0x0000 when that is the stored value. Changing 0x403F to 0x4040 is reflected on click from the live record. No compilation, numeric parsing, trigger comparisons, history or personal ROM/settings changed.

## 184 — Reproduce signpost entries losing relocated scripts

| File Name | Line | Summary |
| --- | --- | --- |
| tests/SignpostScriptGrowth/Program.cs | 9 | Force script growth behind a named neighbor and require every script-bearing signpost to follow while preserving pointer-shaped item/base payloads. |
| tests/SignpostScriptGrowth/README.md | 1 | Document the synthetic relocation reproduction and intended history/resource safety coverage. |

Validation: On 7ef1738c the new regression builds with zero warnings/errors and fails at the first signpost pointer assertion. A separate isolated probe shows the formatted root moving from 018800 to 01C000 with zero ordinary pointer sources, while arg remains 018800 and compilation reports success; old script bytes are cleared. The normal template deliberately keeps arg as a raw union, so generic pointer-source relocation does not know that reference exists. No personal ROM/settings are opened. This evidence commit precedes the targeted repair.

## 185 — Keep signposts linked when an XSE script entry relocates

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Tools/CodeTool.cs | 506 | Retarget matching script-kind signpost args inside the existing compile transaction only when an XSE entry actually moves. |
| tests/SignpostScriptGrowth/Program.cs | 30 | Force a second-pass compiler failure after pointer retargeting and require exact rollback, retained diagnostics and successful correction/retry. |

Validation: SignpostScriptGrowth passes 48 assertions with zero build warnings/errors. All five script signpost kinds follow a moved entry; pointer-shaped hidden-item/base/unsupported payloads remain unchanged, and ordinary shared object pointers retain their existing relocation behavior. Exact one-step Undo/Redo restores bytes, metadata and saved state. An injected second-pass error confirms signpost args have already been updated before failure, then verifies full byte/metadata/allocation rollback, no model-history entry, retained rejected source/diagnostics and successful retry. Separate dialogue growth moves its text resource while keeping the script entry stable and remains exactly undoable. The scan uses read-only map accessors, runs only on an actual XSE entry move, does not convert arg to tracked-pointer metadata, and writes through the existing attempted change. Parsing, numeric rules, resource/relocation allocation and history algorithms are unchanged; this repairs the additional raw-union references those algorithms cannot otherwise see. No personal ROM/settings are used. Complete cross-feature verification follows the remaining audit work.

## 186 — Reproduce missing map-header condition rows

| File Name | Line | Summary |
| --- | --- | --- |
| tests/MapHeaderScripts/Program.cs | 10 | Exercise real header table additions/deletions, shifted row edits, type transitions and exact model-history round trips. |
| tests/MapHeaderScripts/README.md | 1 | Document the raw-table/UI-count mismatch and the audit regression's scope. |

Validation: On 3fc61c58 the regression builds with zero warnings/errors and fails at the first Add Table Script assertion. The isolated probe confirms one displayed child versus two ROM table rows at the same table address. The non-relocation branch calls LINQ Append without consuming its returned sequence and does not publish a new view model; its attempted row address also points four bytes past the row start. This evidence commit precedes repair. No personal ROM/settings are used.

## 187 — Keep map-header condition rows current and independently undoable

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Map/MapScriptCollection.cs | 185 | Release old child callbacks before rebuilding condition view models from current row addresses. |
| src/HexManiac.Core/ViewModels/Map/MapScriptCollection.cs | 287 | Initialize an empty table's first condition like existing type conversion, then publish every added row and close its history action. |
| src/HexManiac.Core/ViewModels/Map/MapScriptCollection.cs | 337 | Reload shifted child rows after deletion and separate that operation from later field edits in Undo history. |
| tests/MapHeaderScripts/Program.cs | 10 | Cover added/deleted/shifted/empty/relocated rows, stale callbacks, regular/table type transitions and parent history. |

Validation: MapHeaderScripts passes 69 assertions with zero build warnings/errors. Added rows are immediately visible; deleting the first row rebinds the survivor to its actual ROM start, so editing cannot overwrite a former row or terminator. Deleting every condition and adding one back creates temp0/value-zero rather than an invisible zero-variable terminator, matching existing regular-to-table defaults. A named neighbor forces actual table relocation and checks owner/child pointers. All regular/table type transitions preserve the surviving entry and round-trip through Undo/Redo; parent deletion and shifted edits do likewise. Byte and structural-metadata comparisons exclude only the existing FreeSpaceSearch allocation hint, which ordinary history has never rewound; no allocator/history algorithm was changed to make that hint compare equal. Failed evidence and repair remain separate commits. No personal ROM/settings or numeric/game-engine rules changed.

## 188 — Reproduce inconsistent source focus across map arrows

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ScriptArrowFocus/Program.cs | 57 | Navigate through real object/green/header arrow controls and require correct targets, source focus, normal selection and valid-address enablement. |
| tests/MapRendering/SyntheticMapFixture.cs | 73 | Add optional synthetic regular and conditional header scripts for the navigation audit. |

Validation: On 8c95a0e5 the real-pointer regression builds with zero warnings/errors and fails on the first object-arrow source-focus assertion after opening the intended address. This is the same missing source-focus behavior previously fixed only for signposts; it does not indicate a wrong script address or modified ROM. Further assertions preserve table-parent navigation and check green-event address validity. The synthetic fixture option is off by default. Evidence precedes repair; no personal ROM/settings are used.

## 189 — Focus source consistently after map script arrows

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 285 | Queue source focus for object, green-event and map-header arrows while preserving their existing bound commands and CanExecute rules. |
| src/HexManiac.Avalonia/Controls/MapTab.axaml.cs | 89 | Share guarded post-navigation source lookup, selecting all only for the explicitly requested signpost arrow. |
| src/HexManiac.Core/ViewModels/Map/IEventViewModel.cs | 1773 | Expose and notify valid-address navigation state for green events and reject invalid direct navigation requests. |

Validation: ScriptArrowFocus passes 26 assertions and SignpostScriptNavigation passes nine, with zero build warnings/errors. Actual pointer input opens the intended object, green, regular-header and conditional-child source and focuses its text box without selecting all; conditional parent arrows continue to open their tables. Signpost arrows retain select-all and ordinary click-to-deselect. Green-event arrows disable/re-enable after null, out-of-ROM and corrected assignments, including programmatic updates. The shared callback checks current model, selected tool/address, realized visible controls and open overlays before focusing. Existing MethodCommand bindings remain in place, including object-arrow enablement behavior. Map View uses its separate navigation path. No personal ROM/settings, parsing, compiler, relocation or history algorithm changed.

## 190 — Reproduce recent-file paths disappearing after restart

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ProfilePersistence/Program.cs | 11 | Save and reload a real injected Mac profile with disposable filenames containing spaces, brackets, commas, quotes, apostrophes and Unicode. |
| tests/ProfilePersistence/README.md | 1 | Document isolated startup/emulator/profile checks and the existing default-window-size boundary. |

Validation: On 0a141bdf the new group builds with zero warnings/errors and fails on exact recent-path restoration after a successful profile reload. Map View/grid/assistance/zoom settings restore, but ParseRecentFiles splits at the first bracket/comma even inside a quoted filename. No personal profile or ROM is opened. Window geometry is not explicitly persisted in existing code; the audit does not manufacture a new geometry-restoration feature. Evidence precedes the targeted preference-format repair.

## 191 — Preserve exact recent-file paths across profile reloads

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/EditorViewModel.cs | 616 | Read a marked escaped recent-file array while retaining a quote-aware literal reader for older profiles. |
| src/HexManiac.Core/ViewModels/EditorViewModel.cs | 655 | Mark new recent-file values as Json and escape complete paths rather than constructing quoted strings by concatenation. |
| tests/ProfilePersistence/Program.cs | 34 | Verify exact legacy migration, punctuation/Unicode reopening and safe handling of malformed/missing recent entries. |

Validation: ProfilePersistence passes 28 assertions with zero build warnings/errors. Five disposable files with spaces, brackets, commas, embedded quotes, apostrophes and Unicode survive a real injected Mac profile save/reload and reopen through their exact Recent menu commands without changing bytes. Unmarked old profiles keep literal Windows backslashes and single/double-quoted paths; saving them migrates to the marked escaped format without ambiguous escape guessing. Invalid/null/missing recent entries do not prevent startup or discard unrelated settings. Fresh instances retain Map View/grid/assistance/zoom and the injected emulator preference; every launch is intercepted, so no native app starts. No ROM auto-opens on restart. Existing 1200×800 default geometry is verified and unchanged; explicit geometry restoration is not a pre-existing persisted feature. No personal profile/ROM, game metadata format, numeric parsing or engine behavior changed. System.Text.Json is provided by the existing target runtime; no package or runtime upgrade was added.

## 192 — Reproduce crossed diff/search preference values

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ProfilePersistence/Program.cs | 14 | Save distinct diff/search limits and require a new editor to restore each from its own key. |

Validation: On 9b9de924 the isolated-profile regression builds with zero warnings/errors and fails when MaximumDiffSegments=17 and MaximumSearchResults=321 are reloaded. Inspection confirms the search-limit branch checks and parses maxDiffLine instead of maxSearchResultsLine. This is an existing preference-reader mix-up, not a search algorithm or runtime issue. Evidence precedes repair and uses no personal settings.

## 193 — Restore the search-result limit from its own setting

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/EditorViewModel.cs | 613 | Check and parse maxSearchResultsLine rather than maxDiffLine when restoring MaximumSearchResults. |
| tests/ProfilePersistence/Program.cs | 46 | Verify independent limits with both keys, the search key alone and missing/invalid search values. |

Validation: ProfilePersistence passes 32 assertions with zero build warnings/errors. Distinct saved limits restore as 17/321; the search key works without a diff key, while missing or malformed search values retain the existing 400 default independently of diff preferences. Existing numeric parsing, 1–10000 bounds, search/diff algorithms and all other settings remain unchanged. Evidence and one-line repair are separate commits; no personal settings or ROM are used.

## 194 — Record completed script/map follow-up audit and full regression results

| File Name | Line | Summary |
| --- | --- | --- |
| tests/SCRIPT_MAP_FOLLOWUP_AUDIT.md | 1 | Record reproducible defects, separate evidence/repair commits, rollback checkpoint, safety coverage and unchanged-working audit areas. |
| tests/SCRIPT_MAP_FOLLOWUP_VERIFICATION.json | 1 | Preserve every group and assertion count from the final complete Mac regression run against a25c0742. |
| tests/ScriptMapNavigation/README.md | 7 | Update navigation coverage to 76 assertions and describe live variable/flag details with a link to the latest suite proof. |
| README.md | 65 | Describe the object flag and event variable context available beside script source. |

Validation: The documented full Mac procedure passes 78 groups / 22,148 assertions, with zero failures and zero skipped groups. The only build-warning category is the existing MapImageExport NU1900 failure to fetch NuGet vulnerability metadata. All production changes were committed before this final run; this entry adds documentation only. No personal ROM/profile was opened or modified, and emulator starts were intercepted. Native interaction/real-ROM gameplay and the full upstream xUnit suite are outside this result. The pre-audit state is retained by checkpoint/pre-script-map-audit-2026-11-07; individual evidence and repairs remain separate commits.

## 195 — Align the block-panel Movement label with its selector

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 126 | Remove conflicting fixed label/selector heights and center the Movement label within their measured row. |
| tests/BlockPanelLayout/Program.cs | 24 | Check shared vertical centering and label containment at all six existing panel-width/font combinations. |
| tests/BlockPanelLayout/README.md | 12 | Record the reproduced protruding-label regression and updated layout coverage. |

Validation: The new containment check fails against b548924d, reproducing the reported label protrusion. After the layout-only repair, BlockPanelLayout passes 60 assertions across 256/320/512px widths and 14/20pt fonts; the project builds with zero warnings/errors and its rendered preview was inspected. Existing non-overlap, available-width and real checkbox-binding checks also pass. No ROM, settings, map data or movement-permission behavior changed. The full suite was not rerun for this localized layout adjustment.

## 196 — Keep Block ID stationary beside Edit Block

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 131 | Place Block ID at the left of the Edit Block row with separate label, reserved numeric and button columns to prevent digit-width shifts. |
| tests/BlockPanelLayout/Program.cs | 31 | Verify identical column positions and unclipped IDs from 0 through 1023 at six panel-width/font combinations. |
| tests/BlockPanelLayout/README.md | 18 | Document the stationary Block ID row and expanded regression coverage. |

Validation: BlockPanelLayout passes 156 assertions across 256/320/512px widths and 14/20pt fonts, including seven representative one- through four-digit IDs, exact stable control bounds, row alignment, non-overlap and the existing real checkbox-binding checks. The project builds with zero warnings/errors and the rendered layout was inspected. The full suite was not rerun for this localized presentation change; no ROM, settings, tile-selection or movement-permission behavior changed.

## 197 — Center Block ID in the space before Edit Block

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 131 | Center the fixed-width Block ID group in the available left column while reserving an independent right column for Edit Block. |
| tests/BlockPanelLayout/Program.cs | 32 | Check horizontal centering alongside stable digit changes, label alignment and button non-overlap at all existing sizes. |
| tests/BlockPanelLayout/README.md | 24 | Describe the centered group and 162-assertion layout coverage. |

Validation: BlockPanelLayout passes 162 assertions across 256/320/512px panel widths and 14/20pt text, with zero build warnings/errors. Seven representative IDs through 1023 retain exact control bounds and fit their reserved numeric field; Edit Block remains separate, and the rendered preview was inspected. The full suite was not rerun for this presentation-only adjustment. No ROM, settings or map-editing behavior changed.

## 198 — Group Block ID beside Edit Block with clear spacing

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml | 131 | Shift Block ID right beside Edit Block with an explicit 8px gap and retain fixed digit space without crowding narrow panels. |
| tests/BlockPanelLayout/Program.cs | 33 | Verify the explicit button gap alongside stable bounds, four-digit fit and existing non-overlap checks. |
| tests/BlockPanelLayout/README.md | 29 | Record the final toolbar placement and tested narrow-panel fit. |

Validation: BlockPanelLayout passes 162 assertions across 256/320/512px widths and 14/20pt fonts, with zero build warnings/errors. A rendered preview was inspected using the widest tested ID, 1023, to verify the gap before Edit Block. The full suite was not rerun for this local presentation-only adjustment. No ROM, settings or editing behavior changed.

## 199 — Retain Go To focus until its input is visible

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs | 665 | Keep a cancellable visibility/layout/attachment focus request until the Go To input can accept typing. |
| tests/GotoFocus/Program.cs | 78 | Reproduce delayed visibility after attachment through the native menu's bound command and verify focus, replacement typing and cancellation. |
| tests/GotoFocus/README.md | 20 | Record the reproduced presentation-timing gap and native-confirmation boundary. |

Validation: The attached-but-hidden reproduction fails against the previous handler and passes after repair; GotoFocus now passes 17 assertions with zero build warnings/errors. Existing Command-G input, immediate typing, query replacement, Escape, delayed attachment and stale-request cancellation checks pass alongside the new delayed-visibility case. Pending event subscriptions end on successful focus, close or editor replacement, without timers. ROM/history remain unchanged. Headless timing simulation does not establish the exact Cocoa event sequence; native confirmation of the reported shortcut remains useful.

## 200 — Select signpost dialogue instead of the entire script

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/MapTab.axaml.cs | 124 | Use decompiled stream classifications to select only the first text block's contents after a signpost-arrow navigation. |
| tests/SignpostScriptNavigation/Program.cs | 70 | Verify message-only selection, multiline text after movement, immediate replacement typing, normal click-to-deselect and no selection for scripts without dialogue. |
| tests/SignpostScriptNavigation/README.md | 1 | Document dialogue-only arrow behavior and its separate navigation safeguards. |

Validation: The message-only assertion fails against the previous explicit SelectAll handler. SignpostScriptNavigation now passes 14 assertions with zero build warnings/errors; actual pointer navigation selects text without commands/braces, typing replaces only the message and leaves the surrounding movement/script compilable, and normal clicking deselects. No-dialogue scripts receive focus without command selection. Pure navigation/selection preserves ROM bytes and metadata. Fresh ScriptArrowFocus (26) and ScriptMapNavigation (76) runs also pass, preserving other-arrow behavior and Map View pending-source/read-only protections; GotoFocus separately passes 17. Only the signpost arrow requests dialogue selection. The complete suite was not rerun for these focused UI corrections; personal ROM/settings were untouched.

## 201 — Reconnect Go To focus when opening ROMs or switching tabs

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs | 262 | Track and replace Go To model subscriptions so focus and overlay handling follow the active tab rather than the initial welcome-screen instance. |
| tests/GotoFocus/Program.cs | 25 | Construct the welcome window before opening the first synthetic ROM and exercise additional tab replacements, stale events, native-command entry and pending-focus cancellation. |
| tests/GotoFocus/README.md | 28 | Explain the missed startup sequence, confirmed subscription defect and expanded 27-assertion coverage. |

Validation: The normal window-before-ROM startup fixture fails against 30e2e552 at the first focus assertion, reproducing a gap the earlier ROM-before-window fixture missed. After subscription repair, GotoFocus passes 27 assertions, including immediate typing after first open, a second ROM tab, returning through the native menu's bound command, old-model isolation, delayed-attachment replacement and window-close cleanup. Fresh ScriptArrowFocus (26), ScriptMapNavigation (76), TabViewLifetime (41) and SignpostScriptNavigation (14) also pass: 184 assertions across five groups, zero build warnings/errors. Both synthetic ROMs and histories remain unchanged. Native computer inspection reached a welcome-screen app rather than the user's pictured session; no personal ROM/profile was opened for testing, and this result does not claim the user's native shortcut was directly observed working. The full suite was not rerun for this subscription correction.

## 202 — Keep script sections separate across unknown padding

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/Models/Code/ScriptParser.cs | 110 | Combine a script destination across a one-byte gap only when that byte is a complete recognized command. |
| src/HexManiac.Core/Models/Code/ScriptParser.cs | 197 | Align script-length consolidation with supported section boundaries so unknown padding remains outside generated source. |
| tests/ScriptGapRoundTrip/Program.cs | 11 | Reproduce FF-gap source failures and verify preserved layout, compilation, shared targets, relocation/resources, rejected drafts and Undo/Redo. |
| tests/ScriptGapRoundTrip/README.md | 1 | Document the synthetic reproduction and preserved existing separator behavior. |

Validation: The new regression fails against 4efc18ba because the decompiler combines a one-byte FF gap into source containing an unsupported .raw FF directive. After repair, ScriptGapRoundTrip passes 117 assertions with zero build warnings/errors across one-/two-/three-byte gaps and single/shared callers. Generated bodies compile, comment-only edits preserve bytes/metadata, and growth/forced relocation preserves caller pointers, dialogue resources, named neighbors and gaps through exact Undo/Redo. Rejected drafts retain diagnostics and correct successfully. Adjacent sections and recognized one-byte end/nop separators retain their existing combined behavior. The repair preserves unknown bytes and numeric/opcode/allocation rules; it does not add a raw-byte compiler feature or erase padding. Personal ROM/settings were untouched; full-suite validation follows separately.

## 203 — Clear resolved script-draft warnings without hiding other errors

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/ViewPort.cs | 655 | Retain the emitted persistence warning and notify listeners only when all rejected/pending drafts in that viewport are resolved. |
| src/HexManiac.Core/ViewModels/EditorViewModel.cs | 322 | Track displayed error ownership and clear a resolved warning only when its source and text still match. |
| src/HexManiac.Core/ViewModels/EditorViewModel.cs | 1252 | Attach and detach warning-resolution listeners with existing tab listener lifetimes. |
| tests/ScriptDraftWarning/Program.cs | 6 | Reproduce the stale banner and verify correction/discard, unrelated-error preservation, multiple drafts and identical warnings from separate ROMs. |
| tests/ScriptDraftWarning/README.md | 1 | Document resolution rules and synthetic warning ownership coverage. |

Validation: The regression fails before repair because successful correction retains the red persistence banner. ScriptDraftWarning now passes 16 assertions with zero build warnings/errors. Correction or explicit draft discard clears only its own still-displayed warning; another rejected edit, unresolved draft, newer unrelated/direct error or another ROM's identical-address warning stays visible. Exact model bytes/structural metadata are preserved in rejection/correction/discard cases. Existing log messages remain available, file/game behavior is unchanged and personal ROM/settings were untouched. Full-suite validation follows separately.

## 204 — Verify warning recovery preserves script typing focus

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ScriptDraftWarning/Program.cs | 46 | Exercise rejected-source warning recovery in the actual headless MainWindow and verify source focus survives resolution in Hex View. |
| tests/ScriptDraftWarning/ScriptDraftWarning.csproj | 4 | Use the existing Avalonia.Headless test dependency for the real-control recovery check. |
| tests/ScriptDraftWarning/README.md | 16 | Record the added focus check and updated assertion count. |

Validation: ScriptDraftWarning passes 18 assertions with zero build warnings/errors. Actual source controls accept focus before the invalid draft, and successful correction clears the warning while keeping typing focus in the script source. Existing ownership, unrelated-error, multi-ROM and multi-draft tests continue to pass. No further production adjustment was needed for this edge case; full-suite validation remains in progress.

## 205 — Record full verification of the script-gap and warning repairs

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ScriptGapRoundTrip/verification.json | 1 | Record every assertion count from the complete 80-group Mac regression run after both repairs and recovery-focus coverage. |
| tests/ScriptGapRoundTrip/README.md | 24 | Link the exact full-suite result alongside the targeted synthetic reproduction. |

Validation: The documented Mac procedure passes 80 groups / 22,417 assertions, with zero failures and zero skipped groups. ScriptGapRoundTrip passes 117 and ScriptDraftWarning passes 18; existing pending-source, compilation/resource failure atomicity, navigation, relocation, history and UI groups also pass. Production implementation is 19216eec with focus-coverage tests at 530de08e; no production code changed during this run. The only build-warning category is the existing MapImageExport NU1900 inability to fetch NuGet vulnerability metadata. The full upstream xUnit suite was not rerun. Existing rejected drafts remain protected rather than silently rewritten, and personal ROM/settings were untouched.

## 206 — Keep tile flip arrows reachable across their hover gaps

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/SelectedBlockEditor.axaml | 45 | Reserve the existing outer spacing as a padded transparent interaction area containing tile images, arrows and the gaps between them. |
| src/HexManiac.Avalonia/Controls/SelectedBlockEditor.axaml.cs | 24 | Hide flip controls only when the pointer leaves the complete interaction region and retain wheel handling within that region. |
| tests/BlockFlipControls/Program.cs | 28 | Trace continuous pointer approaches and positional clicks for both flip axes across all eight pieces, panel widths and text sizes. |
| tests/BlockFlipControls/README.md | 1 | Document the reproduced hover-gap failure, native confirmation and targeted regression results. |

Validation: Against 3ddb055b, the new real-control regression fails while approaching the upper-left vertical flip arrow, matching native reproduction for both upper-/lower-left foreground pieces. After repair, BlockFlipControls passes 1,825 assertions, verifying every approach step, correct hit targets, exact intended bit changes and normal exit dismissal across 280/300/350px widths and 14/20pt text. Fresh MacSecondaryActions (43) and BlockPanelLayout (162) also pass: 2,030 assertions across three groups, zero build warnings/errors. Native coordinate clicks in a disposable synthetic-data app crossed both formerly dead gaps and successfully flipped the intended pieces. Tile positions/overall reserved space and model flip/painting commands are unchanged; the user's Tutorial Village ROM and personal settings were never opened. The full suite was not rerun for this local hover-region repair.

## 207 — Add Tutorial Village as the first welcome-screen background

| File Name | Line | Summary |
| --- | --- | --- |
| OptionalAssets/Welcome/background.png | 1 | Bundle an unchanged copy of arkitekt13's 1312×960 Tutorial Village export through the existing optional-background asset pipeline. |
| OptionalAssets/Welcome/README.md | 1 | Record image provenance, dimensions, replacement procedure and existing responsive crop/fade behavior. |
| docs/MAC_RELEASE_CHECKLIST.md | 105 | Mark the initial image present while retaining final packaged-native visual approval as an open release check. |
| docs/MAC_HELPER_TESTING.md | 72 | Distinguish the included initial image from pending final approval and tutorial-patch gameplay work. |

Validation: WelcomeScreen passes 38 assertions and MacPackaging passes 43, with zero build warnings/errors. The supplied image and bundled asset are byte-identical; the image is copied to build output at resources/Optional/Welcome/background.png. Rendered the actual attached StartScreen with the supplied image and default theme at 1200×800, 800×600 and 500×400; inspected previews and retained the full export using existing proportional fill and dark fade. No image pixels, app layout/rendering code, ROM or original export were changed. Final native appearance approval remains open, particularly at very small window sizes; this is the first background version rather than a finalized tutorial patch or release asset approval. The full regression suite was not rerun for this asset/documentation-only addition.

## 208 — Tidy the map-image export confirmation

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Windows/MapImageSavedDialog.axaml | 1 | Present a compact themed confirmation with a clear heading, selectable filename/location and aligned Reveal in Finder/OK buttons. |
| src/HexManiac.Avalonia/Windows/MapImageSavedDialog.axaml.cs | 9 | Populate file details and retain reveal-without-dismissal plus standard dialog dismissal behavior. |
| src/HexManiac.Avalonia/MacPlatform/MacFileSystem.cs | 474 | Use the dedicated success dialog while preserving the existing export policy, Finder action and no-owner console fallback. |
| tests/MapImageExport/README.md | 28 | Describe the new presentation and actual-control smoke verification. |

Validation: MapImageExport passes its existing 33 assertions. Rendered and visually inspected the actual compiled dialog using a representative Tutorial Village output path; positional Reveal/OK clicks, Enter/Escape dismissal and bounded very-long-path layout passed an isolated smoke check with Finder launches intercepted. The app/preview build has zero warnings/errors; the existing export test project still reports NU1900 when NuGet vulnerability metadata cannot be fetched. No additional persistent tests were added for this reversible presentation adjustment, and the full suite was not rerun. Generic save/error prompts, export bytes, overwrite/copy handling and Finder behavior are unchanged. No ROM/export file or personal preferences were touched; the user's separately updated welcome-background PNG remains unstaged and unmodified by this work.

## 209 — Preserve long-script scrolling through delayed compilation refresh

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Core/ViewModels/Tools/CodeTool.cs | 320 | Update refreshed source directly and retain the collection item when its body identity is unchanged so another section's editor/caret is not rebuilt. |
| tests/ScriptScrollStability/Program.cs | 99 | Reproduce delayed jumps after an earlier-section caret move and verify repeated Enter, compiled dialogue edits, stable controls/focus/source and Undo/Redo. |
| tests/ScriptScrollStability/SyntheticCameraScript.txt | 1 | Preserve the reported camera-event structure with neutral dialogue and synthetic addresses for permanent regression coverage. |
| tests/ScriptScrollStability/ScriptScrollStability.csproj | 8 | Copy the camera-event regression source into the test output. |
| tests/ScriptScrollStability/README.md | 30 | Record the distinct delayed-refresh cause, native before/after observations and expanded coverage. |

Validation: The added regression fails on 0a8da532 with scroll offset 1198 → 100.8125 after delayed Enter compilation. The repaired ScriptScrollStability passes 245 assertions, including actual script fonts 14/20, unchanged earlier editor identity/caret, repeated blank lines near the bottom, preserved source/focus/caret, unchanged model bytes/structural metadata for blank lines, legitimate dialogue compilation and exact Undo/Redo. The exact supplied script also passes through the local-file route without committing its story. Native synthetic-ROM confirmation reproduces 1209 → 114.78125 before repair and a stable 1195 → 1195 after repair. This does not disable keyboard/model caret reveal or alter parsing, transactions, allocation, relocation or history algorithms. The user's ROM/preferences and separately updated welcome-background PNG remain untouched. Focused suite results are recorded separately; the full suite was not rerun for this editor-refresh correction.

## 210 — Record focused verification of delayed script-refresh stability

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ScriptScrollStability/compile-refresh-verification.json | 1 | Preserve the exact 13-group focused result and native delayed-compilation scroll observations for 0f633b3f. |

Validation: Fresh related regressions pass 13 groups / 2,234 assertions, with zero failures, zero skipped groups and zero build warnings/errors. ScriptScrollStability was rebuilt and rerun after strengthening font-size coverage and passes 245 with both the sanitized default and exact supplied local-file routes. The result also includes existing workflow failure-atomicity, assistance, clipboard, navigation, diagnostics, width updates, editor lifetime and signpost-relocation groups. Native before/after evidence and scope limitations are in the verification file. Personal ROM/preferences were untouched, and the user's modified welcome PNG remains unstaged. This is a focused result, not a claim that the complete Mac or upstream xUnit suites were rerun.

## 211 — Include the refreshed Tutorial Village welcome image

| File Name | Line | Summary |
| --- | --- | --- |
| OptionalAssets/Welcome/background.png | 1 | Include the user's updated 1312×960 Tutorial Village background exactly as supplied in the asset folder. |
| OptionalAssets/Welcome/README.md | 3 | Record the refreshed image and its current approximately 267 KiB size. |

Validation: The supplied PNG header reports 1312×960 pixels and its SHA-256 is 22e561867f25cd875386383241f5ddf7de6b4c6759a64bbe8358bbbd16ac8611. No image conversion, crop or source/export/ROM edit was performed. The existing copy-to-build/publish and responsive fade pipeline remains unchanged. This previously unstaged user asset is included under the explicit request to push and merge everything; complete pre-merge Mac-suite validation follows separately.

## 212 — Record complete pre-merge verification and commit review

| File Name | Line | Summary |
| --- | --- | --- |
| tests/MAP_REFERENCE_MERGE_VERIFICATION.json | 1 | Preserve exact results for every group in the complete pre-merge Mac suite against b6b642f9, with commit-review and rollback-checkpoint details. |
| tests/README.md | 9 | Link the dated complete-suite proof and distinguish it from upstream and native verification. |

Validation: The documented Mac procedure passes 81 groups / 24,292 assertions, with zero failures and zero skipped groups. The only build-warning category is the existing MapImageExport NU1900 inability to fetch NuGet vulnerability metadata. Reviewed all 38 existing feature commits since the default-branch checkpoint: each has a descriptive subject and its own CodexChanges update; the supplied refreshed background is separately committed as b6b642f9 with entry 211. Changelog numbering is continuous. The annotated tag checkpoint/pre-map-reference-merge-2026-11-08 preserves default-branch commit 7a6c80ec, and the planned merge preserves individual feature commits. No production code changed during this complete run; this entry adds verification documentation only. Personal ROM/preferences were untouched; the upstream xUnit suite and a complete native/gameplay pass were not rerun.

## 213 — Replace accidental code-font scroll zoom with explicit controls

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Controls/TabView.axaml | 816 | Fit centered minus/plus buttons and a current-size indicator beside Code Type while retaining Hex View and Map View. |
| src/HexManiac.Avalonia/Controls/TabView.axaml.cs | 492 | Resize through explicit button clicks and remove Command-wheel font resizing while keeping the existing 8–20 bounds. |
| tests/ScriptFontControls/Program.cs | 25 | Exercise real pointer input, modified wheel deltas, ordinary scrolling, size limits and unchanged source/model/history/selection. |
| tests/ScriptFontControls/README.md | 1 | Document explicit font controls and the preserved Command-minus navigation shortcut. |
| docs/MAC_SETUP.md | 53 | Explain how to resize code text and that Command-scrolling no longer resizes it. |

Validation: ScriptFontControls passes 12 assertions with zero build warnings/errors. Actual button clicks adjust one step and retain caret/selection/focus, normal scrolling still moves the source, Command-wheel/fractional/horizontal deltas never resize it, and source/ROM/metadata/saved/history/pending-compile state remains unchanged. Fresh ScriptScrollStability (245), ScriptMapReference (63), ScriptMapNavigation (76), ScriptWidthUpdates (448), ClipboardRouting (78) and ScriptEditorLayout (102) also pass: seven focused groups / 1,024 assertions, zero failures and zero build warnings/errors. The actual compiled header was rendered and visually inspected. Command-minus is already Go Back, so its keyboard/native-menu route remains untouched; explicit buttons fulfill the requested alternative. No personal ROM/preferences were opened. The full Mac suite was not rerun for this focused input/UI change.

## 214 — Refresh the accomplishments sheet and GitHub feature overview

| File Name | Line | Summary |
| --- | --- | --- |
| README.md | 63 | Explain Map View navigation, event context, stable source editing, explicit text sizing and recent control fixes while preserving simple installation and upstream credits. |
| output/pdf/HexManiacAdvance_MacEdition_Accomplishments.pdf | N/A | Replace the November 4 sheet with five pages and 92 grouped one-sentence accomplishments through e7bccda6. |
| docs/accomplishments.json | 1 | Retain editable summary records with individual changelog references for future release documentation. |
| scripts/build-accomplishments.py | 1 | Rebuild the PDF from the reviewed records with consistent typography, section layout and page numbering. |

Validation: Rendered and visually inspected all five final PDF pages, verified extracted text and bullet counts, checked local README destinations and git diff whitespace. The sheet distinguishes the November 8 complete Mac run (81 groups / 24,292 assertions / zero failures / zero skipped groups) from the subsequent seven-group / 1,024-assertion font-control checks; no new suite run is claimed. Historical performance figures retain their workload scope, the pending tutorial patch is identified as a framework, and .hma compatibility awaits correct-base user verification. These changes update documentation only; no app code, ROM, preferences or packaging output changed in this commit.

## 215 — Checkpoint and investigate supplied SCADE assets and metadata loading

| File Name | Line | Summary |
| --- | --- | --- |
| tests/SCADE_INVESTIGATION.md | 1 | Record patch identity, pinned input hashes, BPS requirements, existing metadata loading and safe new-copy publication design before implementation. |

Validation: Pushed annotated checkpoint/pre-scade-2026-11-09 at 9d7f347a before edits. Independently inspected the supplied BPS header/footer and verified its patch CRC with Python zlib; reviewed the supplied README and TOML plus HMA's real metadata selection/version paths. The patch requires 16 MiB FireRed 1.0 and produces 32 MiB; the supplied TOML has 541 anchors and version 99.0.0. No patch was applied or production code changed in this evidence commit. Existing TutorialPatch binary passes 36 assertions; this is not a fresh complete-suite result. Personal ROMs/preferences remain unchanged.

## 216 — Create SCADE ROM copies with their supplied metadata before opening

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Project/BpsPatch.cs | 9 | Decode bounded BPS1 into detached bytes with all four operations, sequential overlap, checked integers/offsets and three verified CRCs. |
| src/HexManiac.Avalonia/Project/ScadePackage.cs | 17 | Verify versioned patch/TOML assets and exact base identity; publish matching new TOML/ROM files in that order, preserving collisions and cleaning ordinary failures. |
| src/HexManiac.Avalonia/Windows/MainWindow.Scade.cs | 19 | Add a dedicated Utilities action with a Mac folder/name save picker, captured base bytes and opening only after both outputs exist. |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml | 188 | Expose Create SCADE ROM separately from Tutorial Village and existing HUBOL utilities. |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs | 77 | Initialize and refresh SCADE availability with the selected tab and native Utilities menu. |
| src/HexManiac.Avalonia/HexManiac.Avalonia.csproj | 42 | Include optional BPS/TOML assets and the unchanged creator README in build and publish resources. |
| OptionalAssets/SCADE/scade.json | 1 | Pin the creator-supplied October 21, 2026 patch and TOML, preserving both files and Creator-README.txt byte for byte. |
| OptionalAssets/SCADE/README.md | 1 | Credit the engine creators and explain setup, exact FireRed 1.0 requirements, metadata loading and paired-file failure limits. |
| tests/ScadeInstaller/Program.cs | 1 | Cover BPS errors and operations, independent vectors, paired file writes/races/retry, draft protection and normal custom-metadata open/save/reopen. |
| tests/ScadeInstaller/IndependentBpsVectors.json | 1 | Preserve twelve fabricated vectors generated by the independent Rom Patcher JS implementation. |
| docs/MAC_SETUP.md | 62 | Explain the folder/name utility and keeping the matching ROM/TOML together. |
| tests/README.md | 31 | Link the SCADE-specific regression and its verification boundaries. |

Validation: ScadeInstaller builds with zero warnings/errors and passes 135 assertions. Initial output TOML is byte-identical, normal HMA opening uses relocated custom definitions without regenerating the file, and save/reopen retains addresses and formats. Pending/rejected source, original model bytes/metadata/history, collisions, ordinary publication failures and retry are protected. Twelve independent synthetic patches exercise all four operations; a separate two-assertion comparison executes all 185,880 supplied SCADE commands on fabricated 16 MiB input and matches independent 32 MiB output byte for byte (SHA-256 cceef3504fa108e41ba8a643e53b5f351206b840a09dec635f29a7ab7623a502). Only that temporary comparison patch has adjusted test checksums; shipped assets remain unchanged. Framework-dependent publish copies BPS/TOML/manifest/creator README byte for byte. Actual SCADE-on-FireRed-1.0 loading/gameplay and the real native picker remain manual checks because discovered local bases are 1.1 or edited. Existing Tutorial Village, global IPS/UPS import, compiler, numeric rules and history algorithms are unchanged; no general BPS import route is added and no personal ROM/preferences changed. Complete cross-feature verification follows separately.

## 217 — Record complete SCADE cross-feature verification and remaining manual checks

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ScadeInstaller/verification.json | 1 | Preserve all 83 group counts, independent BPS comparison, metadata lifecycle proof, asset publish identity and explicit real-ROM/native limits for f55f4155. |

Validation: Fresh documented Mac runner passes 83 groups / 24,439 assertions, zero failures and zero skipped groups. The only warning category is the existing MapImageExport NU1900 vulnerability-feed lookup failure. ScadeInstaller contributes 135 assertions; twelve independent synthetic vectors are included, while the separate two-check full supplied-command-stream comparison is not added to that total. Normal initial loading preserves supplied metadata, and save/reopen retain relocated custom definitions. Build/publish copies the patch, TOML, manifest and creator README byte for byte. No exact clean FireRed 1.0 was available, so actual patched-game opening/gameplay and native picker behavior are explicitly pending; upstream xUnit was not rerun. The original checkpoint remains pushed, original ROMs/preferences are unmodified, and implementation remains on the separate SCADE branch.

## 218 — Verify SCADE against the supplied clean FireRed 1.0 base

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ScadeInstaller/RealRomVerification.cs | 1 | Add an opt-in real-ROM path using temporary pairs, independent expected bytes, normal HMA loading and actual save/reopen with Undo/Redo and map rendering. |
| tests/ScadeInstaller/Program.cs | 30 | Dispatch the opt-in mode and strengthen synthetic save/reopen coverage with a changed document plus verified ROM/metadata write counts. |
| tests/ScadeInstaller/real-base-verification.json | 1 | Record matching clean-base/output identities, 37 real-base assertions and all 541 supplied anchor addresses/formats retained after saving. |
| tests/ScadeInstaller/README.md | 36 | Explain repeatable opt-in verification and distinguish native/gameplay checks from confirmed patch and metadata behavior. |
| tests/README.md | 39 | Link the actual-base verification separately from historical complete-suite results. |

Validation: The uploaded 16 MiB BPRE revision-0 base has CRC32 DD88761C and remains SHA-256 3d0c79f1627022e18765766f6cb5ea067f6b5bf7dca115552189ad65a5c3a8ac after testing. Original-patch output matches independent Rom Patcher JS byte for byte: 32 MiB, CRC32 07C373C5, SHA-256 15e44f658a27070352e731b167715f21bebf6a67e6e55e51b81420765d4dcc6f. The normal HMA loader accepts the clean base for the utility and loads all 541 creator anchor addresses from the byte-identical TOML without regenerating it. A disposable stat edit forces one real ROM write and one metadata write: all 541 anchor addresses and formats survive, addresses survive reopening, and Undo/Redo restore the correct bytes. Pallet Town, Viridian City and an interior render, including after reopening. Opt-in checks pass 37 assertions; the strengthened default synthetic group passes 137, with zero build warnings/errors. The previous unchanged-document Save was a no-op, so this commit corrects that coverage gap; no production repair was required. The historical 83-group / 24,439-assertion complete run is retained separately and not represented as freshly rerun. All temporary project pairs are deleted; no ROM data or user preferences are committed/changed. Native save-picker behavior and in-game feature compatibility remain manual checks.

## 219 — Test the supplied DS graphics IPS against vanilla, HUBOL and SCADE

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ScadeInstaller/DsGraphicsCompatibility.cs | 1 | Add an opt-in, exact-patch review of detached IPS application, Undo/Redo, all maps, metadata, gameplay tables, compressed graphics and live event-script references. |
| tests/ScadeInstaller/Program.cs | 29 | Dispatch the optional review without changing the default synthetic suite or adding any production utility. |
| tests/DS_GRAPHICS_COMPATIBILITY_REVIEW.md | 1 | Explain confirmed vanilla/HUBOL collisions, bounded SCADE findings and why universal graphics-only compatibility is not established. |
| tests/ds-graphics-review/results.json | 1 | Preserve every map comparison plus exact input/output hashes, corrupted pointers, credits assets and overwritten live script entrypoints. |
| tests/ScadeInstaller/README.md | 65 | Document opt-in reproduction and keep ROM outputs outside the repository. |

Validation: Generated HUBOL and SCADE from the exact uploaded clean FireRed 1.0 with patch checksums enforced, then compared independent IPS record application with HMA output. Each base renders 425 maps before/after with zero new render failures; 223 images, 20 maps' cells and 17 maps' collision/elevation bitmaps change, while dimensions and used block attributes do not. Vanilla gets four invalid ability-description pointers from the 0x24FC33 text record. HUBOL loses five still-referenced credits graphics to invalid compressed data and has two live script entrypoints overwritten across 15 object events. SCADE preserves all 541 named definitions through actual save/reopen with no equivalent confirmed collisions in the checked structures. Shorter valid title-screen assets and an unreferenced overwritten tileset are explicitly excluded from live corruption findings. The map review completes 35 investigation invariants and the supplemental diagnostic pass 32; these are not compatibility certification. Default ScadeInstaller still passes 137 assertions, builds have zero warnings/errors, and the full Mac suite was not rerun for this opt-in test-only addition. No utility was added, no production code changed, no ROM data or game PNGs were committed, and original ROMs/preferences remain unchanged. In-game/custom-project/other-CFRU compatibility remains unverified; recommend a corrected or separately vetted patch.

## 220 — Review the alternate Ikarus tileset patch across all three bases

| File Name | Line | Summary |
| --- | --- | --- |
| tests/ScadeInstaller/DsGraphicsCompatibility.cs | 25 | Pin the alternate patch, follow recognized child scripts/header roots, filter true script overlaps and distinguish valid replacement graphics from damaged active assets. |
| tests/ScadeInstaller/DsGraphicsCompatibility.cs | 250 | Correct investigation PNG BGR555 conversion without changing the app or raw pixel comparison hashes. |
| tests/ScadeInstaller/Program.cs | 29 | Add the generic graphics-review alias for the opt-in exact-input probe. |
| tests/IKARUS_GRAPHICS_COMPATIBILITY_REVIEW.md | 1 | Compare Ikarus against the earlier patch and document broad map changes plus the Five Isle Meadow routine removal. |
| tests/ikarus-graphics-review/results.json | 1 | Preserve all 425-map comparisons per base, stable table/metadata results and the shared script change. |
| tests/ScadeInstaller/README.md | 79 | Link alternate-patch reproduction and refined script coverage. |

Validation: Exact clean FireRed 1.0, bundled HUBOL and supplied SCADE each render 425 maps before/after with zero new failures; independent IPS application and exact patch-delta Undo/Redo agree. All 549/549/541 named definitions retain addresses/formats through actual save/reopen. No equivalent ability-pointer, active compressed-graphics or overwritten live script-entry corruption from the earlier DS patch is found. Checked active Pokémon/ability/move/item/trainer data remains unchanged. On each base, 120 map images change, 99 maps' block cells change, 43 maps' collision/elevation bitmaps change and 212 maps use changed behavior/layer attributes; dimensions do not change. One shared child routine at 0x168983, reached by a Five Isle Meadow signpost and map header, replaces setmaptile 12 21 687 1 with return. The probe follows 1,959/1,945/1,958 valid roots with zero thrown parse errors, without certifying unsupported/native code. Final investigation completes 35 invariants; default ScadeInstaller passes 137 assertions with zero build warnings/errors. No full Mac run was repeated for these opt-in test/documentation changes. Uploaded originals and preferences remain unchanged; no patch/ROM/PNG was committed and no utility or production feature was added. Recommend a fresh-base trial and in-game progression/movement checks before integration or existing-project use.

## 221 — Checkpoint and scope the Ikarus Visual Upgrade workflow

| File Name | Line | Summary |
| --- | --- | --- |
| tests/IKARUS_UTILITY_DESIGN.md | 1 | Record the authorized edited-project recommendation, mandatory verified paired backup, source/draft guards and undoable metadata refresh design before implementation. |

Validation: Pushed checkpoint/pre-ikarus-visual-upgrade-2026-11-09 at 27175c4f and isolated work on codex/ikarus-visual-upgrade. Reviewed existing backup, generic IPS import, model loading and history behavior: the backup command does not guarantee both writes, and generic IPS import clears history. No production code or game data changed in this design commit. Implementation and verification follow separately.

## 222 — Add the backed-up, undoable Ikarus Visual Upgrade utility

| File Name | Line | Summary |
| --- | --- | --- |
| OptionalAssets/Ikarus/ | 1 | Bundle the exact reviewed FR V3.2 IPS and unchanged creator notes, full graphics credits and usage guidance. |
| src/HexManiac.Avalonia/Project/IkarusUpgrade.cs | 1 | Validate assets/game identity, prepare on a detached model, publish and verify a non-overwriting current ROM/TOML backup, recheck source and apply. |
| src/HexManiac.Avalonia/Windows/MainWindow.Ikarus.cs | 1 | Add the Utilities action, confirmation and native backup picker with captured-source/sibling-draft guards and derived map refreshes. |
| src/HexManiac.Avalonia/Windows/IkarusUpgradeDialog.axaml | 1 | Present the fresh-project recommendation, bounded compatibility claims, non-graphics changes, backup workflow and expandable original credits. |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml | 188 | Expose the new dedicated utility without changing generic IPS/UPS import. |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs | 77 | Initialize and refresh utility availability with editor/tab changes. |
| src/HexManiac.Avalonia/HexManiac.Avalonia.csproj | 42 | Copy optional IPS assets into both build and publish output. |
| src/HexManiac.Core/Models/ModelDelta.cs | 200 | Allow the dedicated reloadable patch token to override reversion; ordinary delta implementation stays unchanged. |
| src/HexManiac.Core/Models/ReloadingModelDelta.cs | 1 | Apply same-size reviewed bytes with rollback, preserve pre-existing history, and rebuild metadata-derived formats through Undo/Redo. |
| src/HexManiac.Core/ViewModels/ChangeHistory.cs | 113 | Provide an explicitly scoped formatting token so derived refreshes preserve Undo/Redo and saved state. |
| tests/IkarusUpgrade/ | 1 | Cover backup failures, source/draft guards, isolated formatting and actual dialog controls, plus opt-in real-ROM save/reopen and existing-map checks. |
| README.md | 82 | List the backed-up visual upgrade with its recommendation and testing limits. |
| docs/MAC_SETUP.md | 63 | Give concrete utility steps, backup/Undo behavior and the tested compatibility boundaries. |

Validation: The dedicated synthetic/headless group builds with zero warnings/errors and passes 70 assertions. Published IPS and creator notes match their source hashes byte for byte. Real temporary vanilla FireRed 1.0, bundled HUBOL and supplied SCADE projects retain all 549/549/541 named anchor addresses/formats through actual Save/reopen, produce exact expected patched bytes, refresh both map snapshots and initialized Map Editors, and retain earlier edits through UI Undo/Redo. An existing-map probe exposed a formatting-only history token after refresh; scoped NoDataChange formatting removes that extra Undo step without clearing Redo, refuses byte writes, and refreshes map caches after data-tab Undo/Redo. The ordinary history path is unchanged outside this explicit utility scope. Mandatory verified backup captures current unsaved bytes/metadata without overwriting originals; cancellation before choosing a destination writes/compiles nothing, and rejected sibling source stops patching. English BPRE revisions including 1.1 are accepted without a clean CRC requirement; other games/languages and unsupported sizes are blocked. Compatibility remains qualified for gameplay, 1.1, other CFRU builds and custom projects; native picker behavior remains manual. No personal ROM/preferences changed. Complete final-suite and opt-in counts follow in a separate evidence commit.

## 223 — Verify the complete Ikarus candidate and real project lifecycle

| File Name | Line | Summary |
| --- | --- | --- |
| tests/IkarusUpgrade/verification.json | 1 | Record all 84 fresh group counts and explicit warning/manual limits for implementation 78125ff3. |
| tests/IkarusUpgrade/real-verification.json | 1 | Preserve exact source/asset identities, 57 real-ROM checks, all named definitions and temporary test-output hashes. |
| tests/README.md | 35 | Link the final full run and distinguish the opt-in process total from additional real-ROM assertions. |
| README.md | 89 | Update the public test summary and point to current per-group evidence. |

Validation: Fresh documented Mac runner passes 84 groups / 24,511 assertions, zero failures and zero skipped groups. IkarusUpgrade contributes 70 assertions. The opt-in exact-base process separately passes 127 assertions (those same 70 plus 57 supplemental real-ROM checks); it is not added wholesale to the full-suite total. Temporary vanilla FireRed 1.0, bundled HUBOL and supplied SCADE retain every named anchor address/format (549/549/541) through actual Save/reopen, agree with expected patch bytes, and restore initialized map images and previous edits through data-tab Undo/Redo. Published IPS and creator notes are byte-identical. Uploaded source remains SHA-256 3d0c79f1627022e18765766f6cb5ea067f6b5bf7dca115552189ad65a5c3a8ac; temporary pairs are deleted and no personal ROM/preferences changed. The only warning category is existing MapImageExport NU1900 vulnerability-feed unavailability; there are no compiler warnings. Native macOS picker behavior, gameplay, FireRed 1.1, other CFRU builds and custom projects remain manual checks, and the upstream xUnit suite is not included/rerun. Feature remains isolated and unmerged on codex/ikarus-visual-upgrade, with the pushed pre-implementation checkpoint available.

## 224 — Enable the read-only usage diagnostic for the supplied FireRed 1.0

| File Name | Line | Summary |
| --- | --- | --- |
| tools/FlagVariableReview/UsageDetectionAudit.cs | 50 | Accept BPRE0 alongside BPRE1 in the optional diagnostic while retaining the explicit English-FireRed gate and real revision/hash reporting. |
| tools/FlagVariableReview/README.md | 24 | Document the two supported read-only fixture revisions and absolute input path. |

Validation: Diagnostic builds with zero warnings/errors. FireRed 1.0 completes all five synthetic game identities with 20 command cases each, then reviews 4,762 variable references from 3,979 decoded segments and 2,143 roots: zero missing references/IDs and zero incomplete traversals within the bounded collector. Model and uploaded-file bytes remain unchanged. The existing supplied 1.1 phase is also rerun successfully; actual engine behavior and complete arbitrary/native-code usage are not certified. This changes only the opt-in diagnostic, not the app, compiler or allocator. Exact results follow in the separate fixture-verification evidence commit.

## 225 — Run previously blocked FireRed 1.0 upstream and integration checks

| File Name | Line | Summary |
| --- | --- | --- |
| tests/FIRERED_10_VERIFICATION.json | 1 | Record exact four-run counts, 158 newly enabled case names, source identities, bounded usage diagnostics and remaining fixture/manual limits. |
| tests/UPSTREAM_BASELINE.md | 3 | Add the supplied-1.0 Debug/Release results without rewriting historical ROM-free checkpoints. |
| tests/INTEGRATION_BASELINE.md | 3 | Record the real FireRed workflows now passing and the nine remaining Emerald skips. |
| tests/FLAG_USAGE_INVESTIGATION.md | 191 | Add read-only corroboration on 1.0 and a fresh 1.1 diagnostic repeat with explicit bounded-scan limits. |
| tests/README.md | 20 | Link the newly available real-ROM coverage separately from Mac assertion totals. |

Validation: Unchanged upstream first-project assertions pass in Debug and Release: 2,716 cases, 1,907 passed, zero failed, 809 skipped, zero engine errors per configuration. Integration passes in both configurations: 67 cases, 58 passed, zero failed, nine skipped, zero engine errors. Direct comparison with prior fixture-bearing XML identifies 126 first-project and 32 integration cases changing Skip to Pass: 158 distinct newly enabled cases, not doubled across configurations. BasicLoad also discovers the newly staged 1.0 file. Remaining skips require other games/revisions/hacks, including Emerald; no fixture is mislabeled or assertion weakened. The read-only variable collector checks 4,762 references across 3,979 segments and 2,143 roots on each supplied revision, with zero missing/incomplete results within its checked roles; all model/disk hashes remain unchanged. The existing unmarked-method xUnit1013 warning is preserved and its method is not counted as executed. Only ignored test-output fixture copies are staged; no personal ROM, TOML or preferences are changed/committed. Existing SCADE and Ikarus exact-base evidence already covers those installers; native gameplay/pickers and unrelated fixtures remain manual/outside scope. Application code and prior 84-group / 24,511-assertion Mac evidence are unchanged.

## 226 — Prepare Tutorial Village distribution with verified paired metadata

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Project/TutorialPatch.cs | 17 | Pin the exact clean 16 MiB BPRE1 base and require hashed, parseable, current tutorial TOML alongside manifest and UPS. |
| src/HexManiac.Avalonia/Project/TutorialPatch.cs | 97 | Validate output bytes and supplied named definitions on a detached model before any publication. |
| src/HexManiac.Avalonia/Project/TutorialPatch.cs | 118 | Publish unchanged matching TOML before the new ROM, reject existing files/directories and clean owned partial output after ordinary failures. |
| src/HexManiac.Avalonia/Windows/MainWindow.Tutorial.cs | 51 | Capture the original editor/source, use a folder/name picker, prepare off the UI thread and open only after both output files exist. |
| tests/TutorialPatch/Program.cs | 1 | Expand synthetic coverage for base pinning, paired publication, metadata validation, normal opening and actual Save/reopen while retaining draft/history checks. |
| tests/TutorialPatch/RealBaseVerification.cs | 1 | Exercise the public loader with a temporary one-byte lesson on the real clean 1.1 base; preserve every real named definition and original source. |
| OptionalAssets/Tutorial/README.md | 1 | Specify the three release files, canonical base identity, TOML hash, paired naming, version refresh and final gameplay limits. |
| tests/TutorialPatch/README.md | 1 | Explain synthetic and opt-in real-base coverage without implying the unfinished village was tested or shipped. |
| docs/MAC_SETUP.md | 96 | Explain the end-user tutorial creation workflow and matching metadata behavior. |

Validation: Focused TutorialPatch builds with zero warnings/errors and passes 58 assertions. The real-base lifecycle probe passes 18 supplemental checks with all 547 named definitions retained through actual normal Save/reopen, Pallet rendering and Undo/Redo; the supplied clean FireRed 1.1 bytes remain unchanged. Publication tests prove the exact supplied TOML exists before ROM publication, failed collisions preserve existing files, and the initial normal loader does not regenerate definitions. Edited ROMs, wrong revisions/games, arbitrary base hashes, missing/stale/tampered definitions and unverified outputs are blocked. Only the internal test loader permits fabricated identities; the public utility pins SHA-256 729041b940afe031302d630fdbe57c0c145f3f7b6d9b8eca5e98678d0ca4d059 and length. The final village patch/TOML are not added and its menu stays disabled until supplied. No tutorial project, original ROM, preferences, compiler or core history code changed. Rollback checkpoint/pre-tutorial-toml-2026-11-09 was pushed before edits; complete cross-feature evidence follows separately.

## 227 — Verify paired Tutorial Village framework across the complete Mac suite

| File Name | Line | Summary |
| --- | --- | --- |
| tests/TutorialPatch/verification.json | 1 | Record all 84 fresh group counts, tested implementation, checkpoint, source/build limits and unchanged originals. |
| tests/TutorialPatch/real-base-verification.json | 1 | Preserve the exact clean 1.1 identity and 18 real-base packaging/lifecycle checks with all 547 definitions retained. |
| tests/README.md | 31 | Separate complete Mac totals from the supplemental real-base lesson and unfinished actual tutorial assets. |
| README.md | 89 | Point to the current 84-group / 24,533-assertion verification record. |

Validation: Fresh documented Mac runner passes 84 groups / 24,533 assertions, zero failures and zero skipped groups. TutorialPatch contributes 58 assertions. The separate opt-in process passes 76 assertions (the same 58 plus 18 supplemental real-base checks), not an extra 76 added to the full-suite total. Exact clean English FireRed 1.1, a temporary one-byte lesson UPS and its matching exported TOML exercise the public loader, edited/revision-zero rejection, byte-exact paired output, zero metadata regeneration on initial open, real Pallet rendering, actual Save/reopen and Undo/Redo. All 547 named definition addresses/formats survive and source SHA-256 remains 729041b940afe031302d630fdbe57c0c145f3f7b6d9b8eca5e98678d0ca4d059. Temporary pairs are removed; the unfinished village, personal ROMs and preferences are untouched. Only existing MapImageExport NU1900 vulnerability-feed unavailability remains; there are no compiler warnings. No final tutorial asset files are added: the utility stays disabled pending the finished patch/TOML/manifest. Native picker and final gameplay remain manual checks. Existing upstream Debug/Release results are separate and were not rerun for this UI/package-only change.

## 228 — Highlight ROM utilities, tutorial readiness and community help

| File Name | Line | Summary |
| --- | --- | --- |
| README.md | 53 | Add a clearly labeled utilities preview for SCADE, backed-up Ikarus and the unfinished Tutorial Village framework without claiming they are in the main download. |
| README.md | 110 | Add Haven's official HMA Discord invite alongside upstream tutorials and Mac Edition bug reports. |
| docs/accomplishments.json | 1 | Expand the grouped sheet to 100 one-sentence accomplishments with current metrics, utility safeguards and distinct implemented/pending status. |
| scripts/build-accomplishments.py | 54 | Read current test metrics, evidence and availability notes from editable records instead of retaining hard-coded old counts. |
| output/pdf/HexManiacAdvance_MacEdition_Accomplishments.pdf | N/A | Publish six reviewed pages covering the new utilities and verified real-ROM workflows while retaining original project credits and scoped performance metrics. |

Validation: Rebuilt with the bundled PDF runtime, rendered and visually inspected all six pages, checked every accomplishment title and all page numbers, and verified exactly 100 bullets. Metrics cite the completed 84-group / 24,533-assertion Mac run separately from upstream cases and installer real-ROM checks; no test rerun is claimed for documentation. The official Discord invite is taken from Haven's current README and resolves to Hex Maniac Advance Development. Compatibility remains qualified, the playable tutorial is explicitly unfinished, and code is identified as preview-only. Documentation-only publication updates the default GitHub page and PDF without merging the feature branch or changing app source, ROMs or preferences.

## 229 — Show a nonintrusive notice for compatible Mac updates

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml | 278 | Show a dismissible update banner above both the editor and welcome screen with fixed View Update and Dismiss actions. |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml.cs | 970 | Observe validated update availability, preserve session dismissal and use the existing file-system process launcher for the fixed fork releases page. |
| tests/UpdateNotice/ | 1 | Exercise actual banner clicks, source focus/selection, welcome/editor lifetime, fixed routing and unchanged model/history/rejected drafts. |

Validation: Fresh UpdateNotice builds with zero warnings/errors and passes 13 headless assertions. The rendered actual banner is visually inspected. Showing it and clicking either action preserve source focus, caret, selection, bytes, metadata and saved/history state; rejected drafts and diagnostics remain intact. It never downloads/installs anything or launches a browser without a user click. Dismissal lasts for the window session while Help > Update remains available; a new window can remind again, and detached editor events cannot affect a replacement editor. Existing version/architecture/release/cache/network policy is unchanged. No personal ROM or preferences changed. A pushed pre-change checkpoint is available; Mac-only 1.0 identity and cross-feature verification follow separately.

## 230 — Give Mac Edition an independent 1.0.0 release identity

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/MacAssemblyInfo.cs | 1 | Define Mac-only 1.0.0 assembly/file versions while retaining existing attribution and common assembly settings. |
| src/HexManiac.Avalonia/HexManiac.Avalonia.csproj | 12 | Stop linking shared Core version declarations into the Mac app without modifying Core or SharedAssemblyInfo.cs. |
| publish-mac.sh | 12 | Read bundle identity from the Mac version file rather than Core's migration version. |
| tests/MacVersionMetadata/ | 1 | Verify existing-format projects load without regeneration and retain custom definitions, lists, history and Core version through actual saving/reopening. |
| tests/CrashReporting/Program.cs | 14 | Check actual Mac 1.0.0 and unchanged Core 0.6.1.0 assembly identities. |
| tests/StartupUpdates/Program.cs | 19 | Cover old-client transition to 1.0.0, same/older-version rejection and later 1.0.1 availability. |
| tests/MacPackaging/Program.cs | 20 | Give fake Core and app files different versions to prove the app version drives bundle/archive identity. |
| tests/UpdateNotice/Program.cs | 67 | Add explicit pending-source preservation to the banner's input and rejected-draft checks. |
| docs/DEVELOPMENT.md | 50 | Document independent release/metadata versioning and stable release asset requirements. |
| docs/MAC_SETUP.md | 114 | Explain the dismissible notice, daily startup checks and manual download workflow. |

Validation: MacVersionMetadata passes 15 synthetic existing-project assertions; Core still exports metadata version 0.6.1 while the app reports 1.0.0. Initial opening produces zero metadata writes and no dirty/Undo state; custom anchors/formats/lists, actual Save/reopen, script decoding and Undo/Redo are retained. StartupUpdates passes 46 assertions, CrashReporting 31 and MacPackaging 43; focused builds have zero warnings/errors. The real Apple Silicon Release app builds successfully, reports 1.0.0 in both bundle version fields and passes deep strict signature verification (still ad-hoc, not notarized). Fresh actual-base SCADE verification passes 37 checks against independent output, retaining 541 definitions; the tutorial lifecycle process passes 76 assertions (58 default plus 18 real-base) and retains 547 definitions without initial TOML regeneration. Original supplied ROM hashes are unchanged; the user's working tutorial is never opened. No Core, shared metadata version, compiler, numeric, engine or history code is modified. Complete final regression evidence follows separately; no GitHub release is published by this version change.

## 231 — Verify Mac 1.0 identity, project preservation and complete regressions

| File Name | Line | Summary |
| --- | --- | --- |
| tests/StartupUpdates/mac-edition-1-0-verification.json | 1 | Preserve every group count, unchanged Core/shared-version identities, actual bundle versions, banner behavior and real project metadata checks. |
| tests/MacVersionMetadata/Program.cs | 56 | Optionally inspect the actual published Mac/Core assemblies and both bundle version fields without launching the app or a personal ROM. |
| tests/MacVersionMetadata/README.md | 15 | Distinguish three actual-bundle checks from the fifteen default project-compatibility assertions. |
| tests/README.md | 28 | Link the 86-group candidate and separate metadata/notice protection groups from historical runs. |
| README.md | 101 | Point preview verification to the current 86-group / 24,568-assertion evidence. |

Validation: Fresh documented Mac runner passes 86 groups / 24,568 assertions, zero failures and zero skipped groups. UpdateNotice contributes 14 actual-input assertions and MacVersionMetadata 15 project checks; StartupUpdates contributes 46, CrashReporting 31 and MacPackaging 43. Core tree 5035dfb85aa6545ce7a9a79c1955227743f07041 and SharedAssemblyInfo.cs are identical to the pushed pre-change checkpoint. The app and actual Apple Silicon bundle report 1.0.0 while both the runtime metadata writer and actual bundled Core retain 0.6.1; strict deep signature verification passes and the package remains ad-hoc signed. Fifteen synthetic project checks plus three actual-bundle checks pass as an 18-assertion supplemental process, not eighteen extra assertions added to the full total. Fresh SCADE checks pass 37 against an independent exact output, preserving all 541 supplied definitions; the tutorial process passes 76 (58 default plus 18 supplemental), preserving all 547 real-base definitions and byte-identical TOML on initial open. Actual Save/reopen and Undo/Redo retain custom definitions and scripts, and original input hashes remain unchanged. The user's working tutorial and preferences are never opened/modified. Existing MapImageExport NU1900 is the only warning category, with no compiler warnings. Native browser/update-release flow, Intel execution, notarization and final tutorial gameplay remain explicit manual/distribution checks. Upstream Core suites were not repeated because Core and its shared version are unchanged. No GitHub 1.0 release is published, and code remains on the isolated feature branch.

## 232 — Keep ROM-creation utilities disabled on search results

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Project/ScadePackage.cs | 70 | Treat a results viewport without a ROM model as an unavailable source instead of dereferencing it. |
| src/HexManiac.Avalonia/Project/TutorialPatch.cs | 87 | Apply the same source guard to the optional tutorial utility. |
| tests/ScadeInstaller/Program.cs | 141 | Reject a search-results source without throwing. |
| tests/TutorialPatch/Program.cs | 156 | Cover results-tab eligibility even when tutorial assets are present. |

Validation: Restoring Find submission exposed a NullReferenceException in SCADE menu refresh when a multi-match search selected SearchResultsViewPort. The default SCADE group passes 138 assertions and TutorialPatch passes 59, including new results-source guards and their existing patch, metadata, draft and lifecycle protections. The Find keyboard group also now reaches its multiple-results checks without this exception. No patch algorithms, ROM data, compiler or history behavior changed.

## 233 — Submit global Find searches with Return

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/Windows/MainWindow.axaml | 308 | Bind Enter only inside FindBox to the existing Find command using the current control text. |
| tests/FindKeyboard/ | 1 | Reproduce the missing binding with actual keyboard input and verify search, navigation and data preservation. |
| tests/README.md | 34 | Link the focused Find regression and distinguish its counts from complete-suite evidence. |

Validation: The new group fails before the binding at Return submission, then passes 25 assertions after the repair. Fresh focused groups pass FindKeyboard 25, GotoFocus 27, ClipboardRouting 78, UpdateNotice 14, ScadeInstaller 138 and TutorialPatch 59: six groups / 341 assertions, zero failures or skipped groups; builds report zero warnings/errors. Checks cover case-insensitive Pokémon names, hex searches, case/file-scope settings, no results, multiple results, F3/Shift-F3 and Escape. Enter in another field remains normal text input, and searches leave both synthetic ROMs, metadata, disk files and Undo histories unchanged. No Core search, table navigation, script compiler or engine behavior changed; no personal ROM or preferences were opened. Native Cocoa confirmation remains a manual check.

Packaging: The Apple Silicon self-contained app rebuilt successfully as Mac Edition 1.0.0 and passed deep strict signature verification with its local ad-hoc signature. The publish process emitted the existing macOS CSSM_ModuleLoad diagnostic, but completed successfully.

## 234 — Prevent the map encounter help link from crashing after a rebuild

| File Name | Line | Summary |
| --- | --- | --- |
| src/HexManiac.Avalonia/MacPlatform/MacHelpLink.cs | 1 | Open help with the absolute macOS launcher, a stable working directory and a single URL argument; return launch/exit diagnostics safely. |
| src/HexManiac.Avalonia/Controls/MapTab.axaml.cs | 959 | Await the More Info launcher and report errors on the originating viewport instead of letting process failures escape the click handler. |
| tests/MapHelpLink/ | 1 | Reproduce deleted-directory launch failure and verify safe process execution and error handling without opening a browser. |

Investigation: The reported stack fails in Process.ResolvePath → GetCwd, before the browser is opened. App startup sets the current directory inside its bundle; republishing while an old instance runs can remove that directory. This is a likely trigger, while the missing-directory failure itself is reproduced. The legendary tip is existing pattern-based guidance for cry/setwildbattle/capture-flag scripts, independent of the selected sprite; it is not a species classification or gameplay change. The existing Haven tutorial URL is preserved.

Validation: MapHelpLink passes 19 assertions, including actual reproduction of the old FileNotFoundException from a removed temporary cwd, successful absolute launch from that same state, URL argument preservation, invalid schemes, null child process, expected launch exceptions, nonzero exit codes and stderr. Tests substitute harmless system processes and never open a browser or personal ROM. Native More Info browser confirmation remains manual. No Core, compiler, encounter recognition, game behavior, ROM data or history code changes.

Related focused verification: BlocksetPreviews passes 160 assertions and FindKeyboard passes 25; together with MapHelpLink, three groups / 204 assertions pass with zero failures or skipped groups and zero build warnings/errors. This is a focused run, not a replacement complete-suite total.

Packaging: The self-contained Apple Silicon Mac Edition 1.0.0 app rebuilt successfully and passed deep strict ad-hoc signature verification. The publish process emitted the existing CSSM_ModuleLoad diagnostic and completed successfully. Quit the existing app before reopening the rebuilt bundle.
