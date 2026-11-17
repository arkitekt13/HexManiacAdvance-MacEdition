# Exact-SCADE Ikarus installer checks

Run `dotnet run --project tests/ScadeIkarusInstaller` or `scripts/verify-mac.sh`.
Default checks use fabricated ROM bytes and the reviewed bundled assets. They
exercise asset identities, original SCADE definitions, malformed/tampered assets,
source/output rejection, paired publication order, file/directory collisions,
racing publication, rollback, staging cleanup, normal custom-TOML opening and
Save/reopen, native menu scope and experimental confirmation/cancellation.

For the full opt-in route:

```
dotnet run --project tests/ScadeIkarusInstaller -- --real-rom /absolute/path/to/clean-firered-1.0.gba
```

This reads the provided clean base without modifying it, generates fresh SCADE
and the compatibility output in disposable storage, verifies exact hashes and
production native menu eligibility, then opens the new named pair normally.
All 639 anchors must load with unchanged addresses/formats, initial opening must
leave the supplied TOML byte-identical, and every definition must survive an
actual edit/Save/reopen. The source byte array and original file are hashed again.
Temporary files are removed. This does not replace manual gameplay review.
