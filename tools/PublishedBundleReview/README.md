# Published bundle startup probe

Build this .NET 8 console project, then pass an absolute `.app` path:

```sh
dotnet run --project tools/PublishedBundleReview -- /absolute/path/HexManiacAdvance.app
```

The probe resolves application dependencies from the published bundle (no application project
reference), calls its startup/editor loader from an unrelated working directory with an
injected temporary profile, loads real script resources, saves/reloads preferences and checks
for bundled runtime libraries. Seven checks print JSON; no native window, emulator, real ROM
or personal preferences are used. This tests the shipped managed loader/resources, not a
native apphost launch on a clean machine without .NET or a Gatekeeper approval.
