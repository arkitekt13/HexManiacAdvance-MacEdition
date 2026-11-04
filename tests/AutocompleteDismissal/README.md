# Suggestion dismissal and editor lifetime

Run `dotnet run --project tests/AutocompleteDismissal` or the complete Mac regression suite.

The 85 checks use production TextEditor/AutocompleteOverlay controls and synthetic script data.
Before the fix, the regression fails when a bound source update reopens suggestions after focus
has moved to another control. Every scenario begins with a verified focused, open popup.

Coverage includes focus loss, unfocused/hidden/detached source updates, overlay and target editor
context replacement, unsupported contexts, target replacement/removal, hidden/disabled parents,
target-only visibility/detachment, reattachment, window deactivation/reactivation, and actual
outside clicks on a non-focusable action. Tests require the overlay hidden, Popup.IsOpen false
and old options discarded. Restoring visibility,
attachment or activation cannot restore old suggestions without a new eligible notification.

The native deactivation callback is exercised through Avalonia's WindowBase hook; native OS
event delivery and every application-switch sequence still need normal desktop verification.
Outside dismissal observes the original pointer event without handling, capturing or replaying
it; the action executes once even while the editor keeps focus.

Subsequent focused notifications can offer fresh suggestions. An Enter sent after dismissal
cannot accept stale completion text, while ordinary newline behavior is permitted. All synthetic
ROM bytes and metadata remain identical.

AutocompleteMapping separately preserves 294 exact offering/acceptance checks, including actual
mouse press/release, Enter, arrows, quotes/space, continuous typing and live script/history
round-trips. Its synthetic positive controls now explicitly focus their target, matching normal
usage. WildEncounterEditing protects the shared overlay's actual encounter-field interaction.
This fix applies to shared AutocompleteOverlay hosts; native ComboBox menus are separate.
