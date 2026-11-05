# Flag and variable suggestion lifetimes

Run through `scripts/verify-mac.sh --no-restore`, or build this project and execute its DLL.
Nine independent synthetic child scenarios pass 81 assertions; any failed child fails the group.

The baseline fails both actual add/generate/delete/replacement cases: flags advance from 0021
to 0022 and script-event trigger variables from 4034 to 4035 after their final reference is gone.

Coverage includes real object/script-event creation and deletion, manual edits after initialization,
shared references, exact deleted-event Undo/Redo and retained Redo after read-only suggestions,
nested/multiple allocation scopes, conservative unowned requests, pending numeric/enum conversion,
complete and malformed source/VarGet operands, literal exclusions, comments/resource blocks,
retained rejected drafts across navigation, explicit discard, assisted insertion, successful
compilation, correction/removal, exact compiled Undo/Redo, known orphaned scripts and metadata-only
script discovery. Pending analysis and suggestions preserve bytes, metadata and history.

The shared workflow/scanner fixtures are fabricated and require no user ROM, preferences, native
window or emulator. Existing ScriptAssistance and ScriptWorkflowSafety retain broader insertion,
relocation, pending save/close, stale callbacks and failure-atomicity controls. This is bounded
analysis, not exhaustive custom/native-engine detection, legal-range/exhaustion changes or trainer
record reclamation. Bare legacy investigator calls keep conservative session reservations;
application tools use explicit operation scopes and CodeTool-owned pending sources.
