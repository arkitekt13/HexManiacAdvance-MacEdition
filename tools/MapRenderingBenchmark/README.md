# Map rendering/presentation benchmark

Run from the repository root:

```sh
dotnet run --project tools/MapRenderingBenchmark/MapRenderingBenchmark.csproj --configuration Release -- artifacts/map-performance/current.json
```

The executable uses actual Core synthetic maps and an offscreen Avalonia/Skia PixelImage.
It records a cold pixel build, 100 cached reads, and seven samples of ten alternating zooms
at three sizes. It checks immutable source bytes and rendered-pixel hashes, counts Source
publications, and reports UI-thread allocations and raw elapsed samples. No elapsed-time
threshold determines correctness. Normal dependency restore may require NuGet access;
the benchmark executable itself uses no ROM or network service.

This measures bitmap-presentation work, not native layout, GPU frame rate, input latency,
or real-ROM/NPC complexity. Compare exact hashes and publication counts along with timing.
The fixture is linked from tests/MapRendering so tests and benchmarks use identical inputs.
See tests/MAP_PERFORMANCE.md for baseline and optimized reports and coverage limitations.
