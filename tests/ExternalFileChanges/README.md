# External file change regression

Run `scripts/verify-mac.sh`. Real native filesystem watchers and disposable synthetic files
exercise direct writes, rapid saves, atomic replacement, deletion/recreation, rename away/back,
similar filenames, identical content, truncation, short and longer exclusive locks, symbolic-link
targets, callback exceptions, local-save acknowledgment, canonical listener removal, and disposal.
An injected UI queue verifies cancellation and snapshot delivery without opening native windows.
Shared-engine checks protect unsaved edits both before and after UI work is queued.

The monitor watches the original path and an existing symbolic link's resolved target. Native
events are debounced for 150 ms; size/timestamp reconciliation every second covers missed native
events. Changed content must produce two equal complete snapshots before delivery. Retries run
in bounded batches, with reconciliation restarting checks for longer-lived locks. Missing, empty,
or oversized files retain the current editor data and wait for a later change after the batch.
Content hashes suppress duplicate notifications and the application's own saves. Tabs unregister
on close; the filesystem also disposes all remaining subscriptions when the window closes.

Limits: only nonempty ROMs up to 64 MiB are automatically reloaded; this is not a two-file ROM/TOML
transaction. Two stable reads reduce partial-write risk but cannot prove an external writer has
finished if it pauses between writes. Unsaved edits are kept without automatic merging or a new
conflict dialog; ignored external changes require deliberate reconciliation/reopening. A renamed
ROM is not automatically followed to a new filename. Retargeting a symbolic link or moving its
parent directory requires reopening to rebuild native watchers. Reconciliation relies on size or
timestamp changes when native notifications are absent. Native application appearance remains
a manual check; these tests do not use a personal ROM or launch an emulator.
