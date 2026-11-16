# Dialogue and movement scaffolding

Run `dotnet run --project tests/DialogueScaffolding/DialogueScaffolding.csproj` or the complete
Mac regression suite. Real keyboard/caret behavior is also covered by ScriptAssistance.

Typing a supported dialogue command followed by the space before its final text pointer now
inserts `<auto>` plus a complete `{ }` block, with an editable blank line and the caret inside.
This covers common msgbox variants, preparemsg and loadpointer's text operand. The source's
existing LF/CRLF/CR style and following commands are preserved. Existing blocks are not duplicated.
Final movement operands also get a complete block, including move.player, move.camera, move.npc
and applymovement. Their bodies contain movement instructions, not dialogue text.
A new trailing group containing only text pointers, such as `single.battle Grunt `,
gets both `<auto>` operands and both blocks together, with the caret in the first.
`single.battle.continue.silent` and `.music` retain ordinary entry because their
required winscript pointer follows the text operands. Earlier explicit resource
pointers and existing blocks (including those preceded by comments) are preserved.
Other resources retain ordinary pointer assistance.
Spartan mode and the existing automatic-insertion setting retain their opt-outs.

This replaces the old immediate/compiler-time missing-brace completion. The compiler still
normalizes complete inline blocks, but an unfinished manual block is a rejected draft with
diagnostics: it is not silently closed or allowed to consume a following end command as text.
The legacy normalization utility retains its default compatibility mode for direct callers;
the application compilation path explicitly disables completion.

Synthetic tests cover seven dialogue command forms and three newline styles, no direct model
writes from source scaffolding, normal deferred allocation and exact text bytes, known caller
pointers after relocation, complete Undo/Redo, existing-block/resource exclusions, opt-outs,
rejected-draft atomicity, Save/Save As/Close protection and correction/retry.
Movement checks additionally verify exact enum bytes and FE termination, unchanged actor IDs,
and preserved following commands across LF/CRLF/CR source.

No user ROM is used. After relaunch, manually type a message command and space, type dialogue
immediately, verify the following command remains outside the block, then Undo/Redo.

Trainer regressions cover LF/CRLF/CR, normal/rematch/canlose forms, exact text-pointer
ordering, untouched continuation destinations, first-block caret, existing blocks
and addresses, end-of-editor notification ordering, atomic failure/correction,
save rejection, Undo/Redo and stale queued assistance after navigation/Undo.
