# Script sections separated by unused bytes

Build this project and run its DLL, or use `scripts/verify-mac.sh`. All ROMs,
addresses and metadata are synthetic. No user files or native UI are used.

The baseline combines a goto with its single-owner destination across a one-byte
FF gap, emitting `.raw FF` that the compiler rejects. FindLength can also include
two-/three-byte gaps even though CollectScripts does not combine those sections.
The regression initially fails because padded sections do not remain separate.

The repair keeps referenced script sections separate across unknown padding,
leaving their bytes and target addresses intact. Existing adjacent sections and
valid single-byte separators (end and nop) still combine. It does not introduce
raw directives to the compiler, strip unknown bytes, change opcode/numeric rules
or alter resource allocation.

117 assertions cover one-/two-/three-byte FF gaps, single/shared callers, every
generated source body's compilation, reading/comment-only byte and structural
metadata preservation, child growth, retained rejected source/diagnostics and
correction/retry. Blocked child growth forces actual relocation with a deferred
dialogue resource, preserved gap/neighbor bytes and shared pointer updates.
Exact Undo/Redo compares ROM bytes and structural metadata, excluding the existing
FreeSpaceSearch cache hint which ordinary history does not rewind.
