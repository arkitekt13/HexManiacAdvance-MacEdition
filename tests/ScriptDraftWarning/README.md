# Resolved rejected-draft persistence warnings

Build this project and run its DLL, or use `scripts/verify-mac.sh`. Tests use the
synthetic WorkflowFixture with intercepted file operations; no personal ROM or
settings are involved.

The baseline blocks Save for invalid script source correctly but leaves the
shell's red warning visible after a successful correction. It initially fails at
the correction/discard warning-clear assertion.

The viewport now reports when the warning's rejected drafts are all resolved.
The shell clears the banner only if that exact warning is still displayed and
belongs to the same viewport. Subscriptions follow tab listener add/remove.
Any direct error assignment relinquishes the old warning's ownership.

16 assertions cover correction, explicit Undo draft discard, exact preserved
model state, another failed edit, newer unrelated errors, direct error assignment,
two ROMs with identical warning text/address, and multiple rejected script bodies.
The warning remains while any draft in its owning ROM is unresolved. Error logs
remain available; unrelated banners are not cleared.
