# Tutorial patch framework

Run through `scripts/verify-mac.sh`, or build this project and execute its DLL.
All ROM/patch bytes are fabricated. Checks cover asset/manifest availability,
exact FireRed 1.1 identity and hashes, forward-only UPS application, expansion,
output verification, source metadata/history preservation, new-copy writes,
existing ROM/sidecar protection and failed-output cleanup.

The actual script workflow fixture also checks pending/rejected draft gating,
retained diagnostics, queue preservation, base mismatch after compilation,
Undo back to the base and preservation of Redo. The native menu's missing-asset
state is checked in TabMenus. Native save-picker cancellation and real tutorial
gameplay remain manual checks once the final assets are available.
