# Signpost script growth

Synthetic ROM/map only. Build the project and run its DLL. Ten signposts deliberately
contain the same pointer-shaped bytes: kinds 0–4 are script entries; kinds 5–8/255
are item/base/unsupported data and must remain bit-identical. The script is formatted
without pointer sources, matching the normal signpost template's union representation.
A named neighbor forces actual script relocation rather than expansion in place.

The baseline fails when the script moves from 018800 to 01C000 while its signpost
arg still points at 018800 and compilation reports success. Tests also cover shared
object pointers, exact Undo/Redo, rejected source/diagnostics, correction/retry and
separate dialogue-resource growth. The repaired regression passes 48 assertions, including an injected second-pass
compiler error after args have been retargeted, exact transaction rollback and retry.
The repair updates raw script-kind entry references inside the existing transaction;
it runs only when an XSE entry moves, and does not turn union fields into pointers.
