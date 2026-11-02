# Pokémon table presentation benchmark

Run from the repository root with .NET 8:

```sh
dotnet run --project tools/PokemonTableBenchmark/PokemonTableBenchmark.csproj --configuration Release -- artifacts/pokemon-table-performance/current.json
```

Normal restore requires NuGet access. Avalonia.Headless 11.2.3 is a dependency of this
benchmark only; the application does not gain a headless dependency. The executable uses
no ROM or network service. It exits nonzero if a correctness check fails and writes
`BENCHMARK_CHECKS_PASSED:N` only after all fixtures finish successfully.

The fixture uses the existing `data.pokemon.stats` format from `tableReference.txt`, real
PokemonModel/ViewPort/TableTool instances, and a Pokémon group containing names and base
stats. Its binary data, addresses, names, item names, and ability names are fabricated.
It does not contain portraits, evolutions, Pokédex data, scripts, or every table in the
normal Pokémon group. It tests 412, 1,024, and 2,048 species; the last two are stress cases.
Adding 250 small unrelated arrays isolates sensitivity to table discovery, rather than
claiming a particular ROM has that number of tables.

Each phase has two warmups and seven measured samples of 100 operations. The report
contains medians, individual elapsed/allocation samples, fixture hashes, and runtime
identity. Allocation means cumulative managed bytes allocated on the calling thread,
not retained memory, native graphics memory, or total-process allocation. Timings are
descriptive; no timing threshold determines success.

Measurements separate:

- Real navigation through the parent names table, with no controls attached.
- Main-selector candidate construction versus prebuilt choices in a controlled experiment.
- Filter preparation without native keyboard input or an open popup.
- Table-section/list discovery and one held-item dropdown constructor.
- Real navigation with production TabView templates, bindings, and headless layout.
- Forced full-tab Skia drawing, both static and with navigation.

The headless setup uses real Skia drawing in an in-memory window, as described in the
[Avalonia headless documentation](https://docs.avaloniaui.net/docs/testing/setting-up-the-headless-platform).
It does not show a native Mac window. Explicit dispatcher pumping and layout are part
of the measured UI phase; forced bitmap drawing is reported separately because it
does not model the native renderer's retained drawing behavior or compositor.
Do not convert these results into native frame rates or input-latency claims.

Separate untimed instrumented models count anchor/run access, list notifications, and
collection replacement. Timing models have no counter overrides. Checks cover selection,
actual bound HP text and visible controls, immutable bytes/metadata during navigation, absence of navigation
undo history, and real name editing followed by undo/redo and exact restoration. The
prebuilt-choice experiment changes no editor code and is not an invalidation-safe cache
implementation. It must not be shipped as one without further tests.

See [TABLE_PERFORMANCE.md](../../tests/TABLE_PERFORMANCE.md) for recorded results, source
analysis, and recommended follow-up work.
