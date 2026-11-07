# Initial script assistance without sticky suppression

Run through `scripts/verify-mac.sh` or restore/build/execute this project with the same
procedure. Uses the real MainWindow, TextEditor, autocomplete overlay and CodeTool with
synthetic ROM/map-independent script data. No personal ROM, preferences or emulator is loaded.

The UI fixture follows the reported multi-section structure: two closed movement blocks,
a jump to another section, then a newly entered resource command. The matrix covers
msgbox.npc, preparemsg and move.player, typing/Enter/mouse completion, LF/CRLF and normal,
trailing spaces, trailing tabs and an earlier auto-pointer edit (72 cases).

Initial help must wait for the typed command's separator or explicit completion, create one
resource block, place the caret inside it and preserve earlier source/model state. Replacing
auto with an explicit address must preserve the exact edit. Deleting that operand and typing
further whitespace must not recreate it. A subsequent command must still receive initial
help. Backspacing the > in an earlier auto token must never toggle assistance off.

Separate queued-work checks require a planned insertion, then supersede it with an explicit
address, Undo, or navigation. Replaying the captured callback must preserve both editors'
source/carets and all model state. The group passes 789 assertions.

The initial regression failed on 71f4abc7 because deleting the operand immediately reinserted
auto. Earlier read-only probes also reproduced trailing-whitespace rejection and a sticky
InsertAutoActive=false after editing an older auto token. Further coverage was added during
the repair, including tabs, first-separator timing and cancellation.

Assistance is planned without source mutation and applied after native input settles using
the existing dispatcher, guarded by source, address, body membership, draft state and compile
generation. The ordinary editor pipeline replaces the pending compilation; no second compiler,
allocator, history writer or delay timer is introduced. Existing explicit opt-outs/Spartan mode
remain supported; pointer edits do not toggle them. Goto/call script destinations gain no new
auto support. Native keyboard/trackpad/real-ROM checks remain user validation.

An excessive stale caret is clamped to current source bounds. Space acceptance for the short
msgbox alias is also protected by the existing ScriptAssistance group: the physical space supplies
the separator, without a second synthetic space or deletion of existing source whitespace.

Fresh complete verification: **70 groups / 21,645 assertions**, zero failures or skipped
groups; [exact counts and warning details](verification.json). The first full attempt caught
the typed msgbox alias separator regression; the final fresh run includes its repair and
all 364 existing ScriptAssistance assertions.
