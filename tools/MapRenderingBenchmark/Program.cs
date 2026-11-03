using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Performance;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;

if (args.Length != 1) throw new ArgumentException("Expected a JSON result path.");
var output = Path.GetFullPath(args[0]);
AppBuilder.Configure<Application>().UseStandardRuntimePlatformSubsystem()
   .UseWindowingSubsystem(() => { }, "offscreen").UseSkia().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
void PumpUntil(Func<bool> ready) {
   var timer = Stopwatch.StartNew();
   while (!ready()) {
      Dispatcher.UIThread.RunJobs();
      if (timer.Elapsed.TotalSeconds > 10) throw new TimeoutException("Map presentation stalled.");
      Thread.Yield();
   }
   Dispatcher.UIThread.RunJobs();
}
string Hash(short[] pixels) => Convert.ToHexString(SHA256.HashData(MemoryMarshal.AsBytes(pixels.AsSpan())));
var results = new List<object>();
// Warm JIT and the real renderer/control once before measuring independent map sizes.
var warm = new SyntheticMapFixture(8, 8);
var warmImage = new PixelImage { DataContext = warm.Map };
PumpUntil(() => warmImage.Source is WriteableBitmap);
warm.Map.SpriteScale = 2;
PumpUntil(() => warmImage.Width == warm.Map.PixelWidth * 2);
foreach (var (width, height) in new[] { (16, 16), (48, 48), (96, 64) }) {
   var fixture = new SyntheticMapFixture(width, height);
   var originalBytes = fixture.Model.RawData.ToArray();
   var start = Stopwatch.GetTimestamp();
   var allocation = GC.GetAllocatedBytesForCurrentThread();
   var pixels = fixture.Map.PixelData;
   var coldMs = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
   var coldAllocated = GC.GetAllocatedBytesForCurrentThread() - allocation;
   var pixelHash = Hash(pixels);
   start = Stopwatch.GetTimestamp();
   for (int i = 0; i < 100; i++)
      if (!ReferenceEquals(pixels, fixture.Map.PixelData)) throw new InvalidOperationException("Map cache was not reused.");
   var cached100Ms = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
   var image = new PixelImage { DataContext = fixture.Map };
   PumpUntil(() => image.Source is WriteableBitmap);
   var uploads = 0;
   image.PropertyChanged += (_, e) => { if (e.Property == Image.SourceProperty) uploads++; };
   var timings = new List<double>();
   var allocations = new List<long>();
   for (int sample = 0; sample < 7; sample++) {
      start = Stopwatch.GetTimestamp();
      allocation = GC.GetAllocatedBytesForCurrentThread();
      for (int step = 0; step < 10; step++) {
         var scale = step % 2 == 0 ? 2 : 1;
         fixture.Map.SpriteScale = scale;
         PumpUntil(() => image.Width == fixture.Map.PixelWidth * scale);
      }
      timings.Add(Stopwatch.GetElapsedTime(start).TotalMilliseconds);
      allocations.Add(GC.GetAllocatedBytesForCurrentThread() - allocation);
   }
   if (!originalBytes.SequenceEqual(fixture.Model.RawData) || Hash(fixture.Map.PixelData) != pixelHash)
      throw new InvalidOperationException("Zoom modified map bytes or pixels.");
   var zoomMedianMs = timings.Order().ElementAt(3);
   results.Add(new { width, height, pixelWidth = fixture.Map.PixelWidth, pixelHeight = fixture.Map.PixelHeight,
      coldMs, coldAllocated, cached100Ms, zoomSamples = 7, zoomStepsPerSample = 10, zoomMedianMs,
      zoomMedianUiAllocated = allocations.Order().ElementAt(3), zoomUploads = uploads,
      zoomSamplesMs = timings, pixelSha256 = pixelHash });
   Console.WriteLine($"{width}x{height}: cold {coldMs:F3}ms; 10 zoom steps {zoomMedianMs:F3}ms median; {uploads} bitmap publications across 70 zooms");
}
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output, JsonSerializer.Serialize(new {
   runtime = RuntimeInformation.FrameworkDescription, platform = RuntimeInformation.OSDescription,
   scope = "Real Core synthetic-map rendering plus offscreen PixelImage presentation; no native layout/GPU/input/ROM measurement; zoom allocations count UI thread only",
   results,
}, new JsonSerializerOptions { WriteIndented = true }));
