# Trainer table presentation investigation

From the repository root with .NET 8:

```sh
dotnet run --project tools/TrainerTableBenchmark/TrainerTableBenchmark.csproj --configuration Release -- artifacts/trainer-table-performance/current.json
```

The executable requires no ROM or network connection after dependency restore. It uses
the existing trainer schema from `tableReference.txt`, default Trainer/Team partitions,
and symbolic lists from `default.toml` and `default.bpre.bpge.toml`. The 32 AI-bit labels
are needed to obtain the real 40-byte parent layout. No game addresses are imported.
All addresses, trainer names, teams, and graphics are fabricated. Avalonia.Headless
11.2.3 is a benchmark dependency only.

Six fixtures each contain 768 independent trainer/team pairs, 412 Pokémon names, 512
move names, 512 item names, and 64 trainer class/portrait options. The cases isolate
one versus six members, default versus explicit moves/items, Pokémon preview graphics,
trainer portrait choices, and mixed formats/counts. Cases without portrait graphics use
a name-only source for that field. Artwork uses uncompressed 64×64 4bpp sprites and a
fabricated palette, so decompression complexity is excluded. These counts are controlled
benchmark inputs, not claims about a stock ROM's tables.

Every phase uses two warmups and seven measured 100-operation samples. Reports retain
individual elapsed/allocation samples, medians, fixture/text/pixel hashes, runtime identity,
and untimed reuse/binding diagnostics. Allocations are cumulative managed bytes on the
calling thread, not retained RAM, native graphics memory, or total-process allocation.
No timing threshold determines correctness.

The phases separate production navigation, serialization, error validation, prefixed/bare
move lookup, generic/specialized stream construction, dropdown preparation, preview
generation, and real TabView binding/layout. Production navigation uses the current
template order unchanged. The report records which team control actually appears;
backend preview generation must not be mistaken for previews displayed in the current UI.

After production timings, the final case runs an explicit in-memory experiment: move
the trainer template ahead of the generic stream template and recreate that group's
presenters. It checks the specialized editor, exact preview pixels, and presence of both
default commands. This changes no application XAML, executes neither default command,
and is not included in the production timing phase. Likewise, prebuilt portrait choices
and bare move-name matching are component diagnostics, not shipped caches or fixes.

Checks cover real selection/text bindings, team counts/flags, immutable read-only bytes
and metadata, no navigation undo history, actual stream editing, growth relocation or
shrinkage, unchanged unrelated parent fields/team pointers, undo/redo, and exact byte and
anchor-definition restoration. The free-space search cursor is not required to rewind
after an edit; it is not ROM data. Shared teams, learnsets/evolutions, default-move/item
command behavior, real move legality, game execution, native input, and the Mac compositor
remain outside coverage.

The program exits nonzero on a failed check and prints `BENCHMARK_CHECKS_PASSED:N` only
after all cases finish. It is separate from `verify-mac` and the upstream test suites.
See [TRAINER_PERFORMANCE.md](../../tests/TRAINER_PERFORMANCE.md) for findings and next steps.
