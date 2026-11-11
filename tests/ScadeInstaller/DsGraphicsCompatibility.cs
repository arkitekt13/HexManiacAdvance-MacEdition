using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using HavenSoft.HexManiac.AvaloniaUI.Project;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.Models.Runs.Sprites;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using SkiaSharp;
using HexManiac.Core.Models.Runs.Sprites;

// Opt-in investigation only: supplied ROM bytes and IPS stay outside Git; no new patch utility.
internal static class DsGraphicsCompatibility {
   internal sealed record IpsRecord(int Start, byte[] Bytes);
   internal sealed record MapResult(int Id, int Width, int Height, string Cells, string Permissions, string Attributes, string Pixels, string Error);
   public static int Run(string sourcePath, string patchPath, string reportDirectory, bool renderMaps = true) {
      int checks = 0;
      void Check(bool value, string message) { checks++; if (!value) throw new Exception(message); }
      string Hash(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes));
      var source = File.ReadAllBytes(sourcePath); var originalHash = Hash(source);
      var patch = File.ReadAllBytes(patchPath);
      string profile = Hash(patch) switch {
         "73ADDD57354ACEBD58EC55B7B7E3FBEA14EE389A5CA90B762261260EDFD8033D" => "DS v1.6.3",
         "CBC79D5613C24D682119C6A43BA191BAAE39A0C7FAB8A0CBF53311891A3C8B7C" => "Ikarus FR v3.2",
         _ => null,
      };
      Check(profile != null, "review uses an exact supplied graphics patch");
      var records = ReadIps(patch);
      Check((uint)Patcher.CalcCRC32(source) == 0xDD88761C, "exact clean base required");
      Directory.CreateDirectory(reportDirectory);
      var temporary = Path.Combine(Path.GetTempPath(), "hma-ds-compatibility-" + Guid.NewGuid().ToString("N"));
      Directory.CreateDirectory(temporary);
      try {
         var hubol = new BasicModel(source.ToArray());
         var root = new DirectoryInfo(AppContext.BaseDirectory);
         while (root != null && !File.Exists(Path.Combine(root.FullName, "src/HexManiac.Core/ViewModels/QuickEditItems/hubol_dpe_cfru.ups"))) root = root.Parent;
         if (root == null) throw new DirectoryNotFoundException("Run the opt-in review from a repository checkout containing HUBOL.");
         var hubolPatch = File.ReadAllBytes(Path.Combine(root.FullName, "src/HexManiac.Core/ViewModels/QuickEditItems/hubol_dpe_cfru.ups"));
         Check(Patcher.ApplyUPSPatch(hubol, hubolPatch, () => new NoTrackChange(), false, out var direction) >= 0 &&
            direction == Patcher.UpsPatchDirection.SourceToDestination, "HUBOL generated with all UPS checksums enforced");
         var scade = ScadePackage.Load(ScadePackage.DefaultDirectory, out var reason);
         Check(scade != null, "SCADE assets valid");
         var definitions = File.ReadAllLines(Path.Combine(ScadePackage.DefaultDirectory, "SCADE.toml"));
         var results = new List<object>();
         foreach (var (name, bytes, suppliedMetadata) in new[] {
            ("vanilla", source, (string[])null), ("hubol", hubol.RawData, (string[])null), ("scade", scade.Create(source), definitions),
         }) {
            Console.WriteLine("DS_REVIEW_START:" + name);
            string[] workingMetadata = suppliedMetadata;
            var fs = new StubFileSystem {
               MetadataFor = path => Path.GetExtension(path) == ".gba" ? workingMetadata ?? Array.Empty<string>() : Array.Empty<string>(),
               SaveMetadata = (_, lines) => { workingMetadata = lines; return true; },
               Save = _ => throw new InvalidOperationException("Review opening/rendering must never save a ROM."),
            };
            var editor = new EditorViewModel(fs, InstantDispatch.Instance);
            ViewPort Open(byte[] data, string suffix) {
               var view = editor.OpenFileAsync(new LoadedFile(Path.Combine(temporary, name + suffix + ".gba"), data.ToArray())).GetAwaiter().GetResult();
               if (view == null) throw new Exception(editor.ErrorMessage);
               view.Model.InitializationWorkload.GetAwaiter().GetResult();view.InitializationWorkload.GetAwaiter().GetResult();
               return view;
            }
            var before = Open(bytes, "-before");
            var beforeMetadata = before.Model.ExportMetadata(null, editor.Singletons.MetadataInfo).Serialize();
            var anchors = new StoredMetadata(beforeMetadata).NamedAnchors;
            var candidate = new BasicModel(bytes.ToArray());
            var delta = new ModelDelta();
            Check(Patcher.ApplyIPSPatch(candidate, patch, delta) >= 0, name + " IPS accepted");
            var patched = candidate.RawData.ToArray();
            // Independent application of parsed literal/RLE records, not the HMA patcher.
            var independent = bytes.ToArray();
            foreach (var record in records) record.Bytes.CopyTo(independent, record.Start);
            Check(independent.SequenceEqual(patched), name + " independent record application agrees");
            Check(patched.Length == bytes.Length, name + " size unchanged");
            var undo = delta.Revert(candidate);
            Check(candidate.RawData.SequenceEqual(bytes), name + " patch delta Undo exact");
            undo.Revert(candidate);
            Check(candidate.RawData.SequenceEqual(patched), name + " patch delta Redo exact");
            workingMetadata = beforeMetadata;
            var after = Open(patched, "-after");
            var actualChanges = Enumerable.Range(0, bytes.Length).Where(i => bytes[i] != patched[i]).ToArray();
            var coverage = new HashSet<int>(records.SelectMany(r => Enumerable.Range(r.Start, r.Bytes.Length)));
            Check(actualChanges.All(coverage.Contains), name + " changes confined to IPS records");
            var types = new Dictionary<string, int>();
            var affectedSprites = new Dictionary<int, ISpriteRun>();
            foreach (var index in actualChanges) {
               var run = before.Model.GetNextRun(index);
               var type = run.Start <= index && index < run.Start + run.Length ? run.GetType().Name : "Unformatted";
               types[type] = types.GetValueOrDefault(type) + 1;
               if (type != "Unformatted" && run is ISpriteRun sprite) affectedSprites.TryAdd(run.Start, sprite);
            }
            var spriteResults = new List<object>();
            foreach (var (address, sprite) in affectedSprites) {
               string beforeError = null, afterError = null;
               try { if (sprite.GetPixels(before.Model, 0, 0) is not { Length: > 0 }) throw new InvalidDataException("No decoded sprite pixels."); } catch (Exception ex) { beforeError = ex.GetType().Name + ": " + ex.Message; }
               try {
                  var next = after.Model.GetNextRun(address) as ISpriteRun;
                  if (next == null) throw new InvalidOperationException("Sprite format missing after patch.");
                  if (next.GetPixels(after.Model, 0, 0) is not { Length: > 0 }) throw new InvalidDataException("No decoded sprite pixels.");
               } catch (Exception ex) { afterError = ex.GetType().Name + ": " + ex.Message; }
               string rawAfterError = null;
               int? rawAfterLength = null;
               string rawBeforeHash = null, rawAfterHash = null;
               int[] stableIncomingPointers = sprite.PointerSources.Where(p => before.Model.ReadPointer(p) == address && after.Model.ReadPointer(p) == address).ToArray();
               if (sprite.FormatString.StartsWith("`lz")) {
                  try {
                     var data = LZRun.Decompress(after.Model, address);
                     rawAfterLength = data?.Length;
                     var originalData = LZRun.Decompress(before.Model, address);
                     if (originalData != null) rawBeforeHash = Hash(originalData);
                     if (data != null) rawAfterHash = Hash(data);
                     if (data == null) throw new InvalidDataException("Original sprite address no longer decompresses as valid GBA LZ data.");
                  } catch (Exception ex) { rawAfterError = ex.GetType().Name + ": " + ex.Message; }
               }
               spriteResults.Add(new { address, format = sprite.FormatString, beforeError, afterError, rawAfterError, rawAfterLength, originalExpectedBytes = sprite.SpriteFormat.ExpectedByteLength, rawBeforeHash, rawAfterHash,
                  originalAnchor = before.Model.GetAnchorFromAddress(-1, address),
                  incomingOwners = stableIncomingPointers.Select(p => new { source = p, owner = before.Model.GetAnchorFromAddress(-1, before.Model.GetNextRun(p).Start) }).ToArray(), stableIncomingPointers });
            }
            var tableResults = new List<object>();
            foreach (var tableName in new[] { "data.pokemon.names", "data.abilities.names", "data.abilities.descriptions", "data.pokemon.stats", "data.pokemon.moves.stats.battle", "data.items.stats", "data.trainers.stats" }) {
               var oldTable = before.Model.GetTable(tableName); var newTable = after.Model.GetTable(tableName);
               if (oldTable == null || newTable == null) { tableResults.Add(new { name = tableName, unavailable = true, beforeAvailable = oldTable != null, afterAvailable = newTable != null }); continue; }
               int count = Math.Min(oldTable.Length, newTable.Length);
               int changed = Enumerable.Range(0, count).Count(i => before.Model[oldTable.Start + i] != after.Model[newTable.Start + i]);
               tableResults.Add(new { name = tableName, beforeAddress = oldTable.Start, afterAddress = newTable.Start, beforeCount = oldTable.ElementCount, afterCount = newTable.ElementCount, changedBytes = changed });
            }
            var abilityNames = before.Model.GetTable("data.abilities.names");
            var descriptionTable = before.Model.GetTable("data.abilities.descriptions");
            var invalidDescriptionPointers = new List<object>();
            if (descriptionTable != null) for (int i = 0; i < descriptionTable.ElementCount; i++) {
               int at = descriptionTable.Start + i * descriptionTable.ElementLength;
               int oldPointer = before.Model.ReadPointer(at), newPointer = after.Model.ReadPointer(at);
               if (oldPointer >= 0 && oldPointer < bytes.Length && (newPointer < 0 || newPointer >= patched.Length)) {
                  string ability = abilityNames == null ? i.ToString() : new ModelTable(before.Model, abilityNames)[i].GetStringValue("name");
                  invalidDescriptionPointers.Add(new { index = i, ability, address = at, oldPointer, newPointer });
               }
            }
            var changedScriptRuns = actualChanges.Select(i => (index: i, run: before.Model.GetNextRun(i)))
               .Where(pair => pair.run.Start <= pair.index && pair.index < pair.run.Start + pair.run.Length).Select(pair => pair.run).OfType<XSERun>().DistinctBy(r => r.Start)
               .Select(r => new { address = r.Start, length = r.Length, stableIncomingPointers = r.PointerSources.Where(p => before.Model.ReadPointer(p) == r.Start && after.Model.ReadPointer(p) == r.Start).ToArray() }).ToArray();
            var affectedEventScripts = new List<object>();
            var parsedScriptBodyChanges = new List<object>();
            var parsedScriptErrors = new List<object>();
            var parser = new ScriptParser(before.Model.GetShortGameCode(), editor.Singletons.ScriptLines, 2);
            var scriptRanges = new Dictionary<int, List<(int start, int length, int changed)>>();
            List<(int start, int length, int changed)> ChangedBodies(int root) {
               if (scriptRanges.TryGetValue(root, out var cached)) return cached;
               var bodies = new List<(int, int, int)>();scriptRanges[root] = bodies;
               if (root < 0 || root >= bytes.Length) return bodies;
               try {
                  foreach (int segment in parser.CollectScripts(before.Model, root)) {
                     int length = parser.GetScriptSegmentLength(before.Model, segment);
                     int index = Array.BinarySearch(actualChanges, segment);if (index < 0) index = ~index;
                     int changed = 0;
                     while (index < actualChanges.Length && actualChanges[index] < (long)segment + length) { changed++;index++; }
                     if (changed > 0) bodies.Add((segment, length, changed));
                  }
               } catch (Exception ex) { parsedScriptErrors.Add(new { root, error = ex.GetType().Name + ": " + ex.Message }); }
               return bodies;
            }
            var changedRoots = changedScriptRuns.Select(r => r.address).ToHashSet();
            foreach (var bank in AllMapsModel.Create(before.Model, () => new NoTrackChange())) foreach (var map in bank) {
               if (map == null) continue;
               var events = map.Events;
               void Record(string kind, int index, int script) {
                  foreach (var body in ChangedBodies(script)) {
                     int oldLabels = 0, newLabels = 0;
                     string beforeBody = parser.Parse(before.Model, body.start, body.length, ref oldLabels);
                     string afterBody = parser.Parse(after.Model, body.start, Math.Max(1, parser.GetScriptSegmentLength(after.Model, body.start)), ref newLabels);
                     parsedScriptBodyChanges.Add(new { map = map.Group * 1000 + map.Map, mapName = BlockMapViewModel.MapIDToText(before.Model, map.Group * 1000 + map.Map), kind, index, root = script, body = body.start, body.length, changedBytes = body.changed, beforeBody, afterBody });
                  }
                  if (changedRoots.Contains(script) || script >= 0 && script < Math.Min(bytes.Length, patched.Length) && before.Model[script] != after.Model[script]) affectedEventScripts.Add(new { map = map.Group * 1000 + map.Map, kind, index, script,
                     beforeOpcode = before.Model[script], afterOpcode = after.Model[script] });
               }
               for (int n = 0; n < events.Objects.Count; n++) Record("object", n, events.Objects[n].ScriptAddress);
               for (int n = 0; n < events.Scripts.Count; n++) Record("script", n, events.Scripts[n].ScriptAddress);
               for (int n = 0; n < events.Signposts.Count; n++) if (events.Signposts[n].HasScript) Record("signpost", n, events.Signposts[n].ScriptAddress);
               var headers = map.Element.GetSubTable("mapscripts");
               if (headers != null) for (int n = 0; n < headers.Count; n++) {
                  int type = headers[n].GetValue("type"), target = headers[n].GetAddress("pointer");
                  if (type == 2 || type == 4) {
                     for (int row = 0; row < 1024 && target >= 0 && target + 8 <= bytes.Length; row++, target += 8) {
                        if (before.Model.ReadMultiByteValue(target, 2) == 0) break;
                        Record("conditional-header", n * 1024 + row, before.Model.ReadPointer(target + 4));
                     }
                  } else if (type != 0) Record("map-header", n, target);
               }
            }
            var beforeMaps = renderMaps ? Maps(before, name + "-before") : new List<MapResult>();
            var afterMaps = renderMaps ? Maps(after, name + "-after") : new List<MapResult>();
            if (renderMaps) Check(beforeMaps.Count == afterMaps.Count, name + " map inventory stable");
            var comparisons = beforeMaps.Select(b => {
               var a = afterMaps.Single(m => m.Id == b.Id);
               return new { id = b.Id, beforeError = b.Error, afterError = a.Error,
                  dimensionsChanged = b.Width != a.Width || b.Height != a.Height,
                  cellsChanged = b.Cells != a.Cells, permissionsChanged = b.Permissions != a.Permissions, attributesChanged = b.Attributes != a.Attributes,
                  pixelsChanged = b.Pixels != a.Pixels };
            }).ToArray();
            Check(before.Model.RawData.SequenceEqual(bytes) && after.Model.RawData.SequenceEqual(patched), name + " opening and rendering preserve ROM bytes");
            var missing = anchors.Where(a => after.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, a.Name) != a.Address).Select(a => a.Name).ToArray();
            // Force serialization, write only the disposable output, then use the normal open path again.
            int saves = 0;
            fs.Save = file => { File.WriteAllBytes(file.Name, file.Contents); saves++; return true; };
            fs.SaveMetadata = (_, lines) => { workingMetadata = lines; return true; };
            after.ChangeHistory.ClearHistory(needsSave: true);
            after.Save.Execute(fs);
            Check(saves == 1, name + " real save executed");
            var serialized = new StoredMetadata(workingMetadata);
            var alteredFormats = anchors.Where(a => !serialized.NamedAnchors.Any(n => n.Name == a.Name && n.Address == a.Address && n.Format == a.Format)).Select(a => a.Name).ToArray();
            var reopened = Open(File.ReadAllBytes(after.FullFileName), "-reopened");
            Check(reopened.Model.RawData.SequenceEqual(patched), name + " patched bytes survive save/reopen");
            var missingReopened = anchors.Where(a => reopened.Model.GetAddressFromAnchor(new NoDataChangeDeltaModel(), -1, a.Name) != a.Address).Select(a => a.Name).ToArray();
            results.Add(new { name, beforeLength = bytes.Length, afterLength = patched.Length,
               beforeSha256 = Hash(bytes), afterSha256 = Hash(patched), changedBytes = actualChanges.Length,
               changedBytesByOriginalFormat = types, tableResults, invalidDescriptionPointers, changedScriptRuns, affectedEventScripts, parsedScriptBodyChanges, parsedScriptErrors, scriptRootsExamined = scriptRanges.Keys.Count(r => r >= 0 && r < bytes.Length), affectedSprites = spriteResults,
               anchorsBefore = anchors.Count, missingAfter = missing, alteredAfterSerialization = alteredFormats, missingReopened,
               mapCount = beforeMaps.Count, maps = comparisons });
            Console.WriteLine($"DS_REVIEW_DONE:{name}:maps={comparisons.Length}:newFailures={comparisons.Count(m => m.beforeError == null && m.afterError != null)}:changedImages={comparisons.Count(m => m.pixelsChanged)}");
            File.WriteAllText(Path.Combine(reportDirectory, "results.json"), JsonSerializer.Serialize(new { profile, sourceSha256 = originalHash, patchSha256 = Hash(patch), ipsRecords = records.Count, requestedWriteBytes = records.Sum(r => r.Bytes.Length), variants = results }, new JsonSerializerOptions { WriteIndented = true }));
            GC.Collect();GC.WaitForPendingFinalizers();
         }
         Check(Hash(File.ReadAllBytes(sourcePath)) == originalHash, "uploaded source unchanged");
         Console.WriteLine($"CHECKS_PASSED:{checks}");
         return checks;

         List<MapResult> Maps(ViewPort view, string label) {
            var list = new List<MapResult>();
            var maps = AllMapsModel.Create(view.Model, () => new NoTrackChange());
            foreach (var bank in maps) foreach (var map in bank) {
               if (map == null) continue;
               int id = map.Group * 1000 + map.Map;
               int width = 0, height = 0;
               string cells = null, permissions = null, attributes = null, pixels = null, error = null;
               try {
                  var layout = map.Layout; width = layout.Width; height = layout.Height;
                  if (width <= 0 || height <= 0 || (long)width * height * 256 > 16_777_216) throw new InvalidOperationException("Invalid/oversized map dimensions.");
                  var cellBytes = view.Model.RawData.AsSpan(layout.BlockMap.Start, width * height * 2).ToArray();
                  cells = Hash(cellBytes);
                  permissions = Hash(Enumerable.Range(0, width * height).Select(i => (byte)(cellBytes[i * 2 + 1] >> 2)).ToArray());
                  var block1 = layout.PrimaryBlockset.FullBlocksetModel; var block2 = layout.SecondaryBlockset.FullBlocksetModel;
                  int pmax = BlockmapRun.GetMaxUsedBlock(view.Model, layout.BlockMap.Start, width, height, block1.PrimaryBlocks);
                  int smax = BlockmapRun.GetMaxUsedBlock(view.Model, layout.BlockMap.Start, width, height, 1024) - block1.PrimaryBlocks;
                  attributes = Hash(BlockmapRun.ReadBlockAttributes(pmax, smax, block1, block2).Where(a => a != null).SelectMany(a => a).ToArray());
                  var vm = new BlockMapViewModel(new StubFileSystem(), new MapTutorialsViewModel(), view, new Format(view.Model), null, map.Group, map.Map) {
                     IncludeBorders = false, ShowEvents = MapDisplayOptions.NoEvents, IsSelected = false,
                  };
                  var data = vm.PixelData;
                  if (data == null) throw new InvalidOperationException("No rendered pixels.");
                  pixels = Hash(MemoryMarshal.AsBytes(data.AsSpan()).ToArray());
                  if (id is 3000 or 3001 or 4000) {
                     using var bitmap = new SKBitmap(vm.PixelWidth, vm.PixelHeight);
                     for (int y = 0; y < vm.PixelHeight; y++) for (int x = 0; x < vm.PixelWidth; x++) {
                        int c = data[y * vm.PixelWidth + x];
                        bitmap.SetPixel(x, y, new SKColor((byte)(((c >> 10) & 31) * 255 / 31), (byte)(((c >> 5) & 31) * 255 / 31), (byte)((c & 31) * 255 / 31), 255));
                     }
                     using var image = SKImage.FromBitmap(bitmap);using var encoded = image.Encode(SKEncodedImageFormat.Png, 100);
                     using var stream = File.Create(Path.Combine(reportDirectory, label + "-" + id + ".png"));encoded.SaveTo(stream);
                  }
               } catch (Exception ex) { error = ex.GetType().Name + ": " + ex.Message; }
               list.Add(new(id, width, height, cells, permissions, attributes, pixels, error));
            }
            return list;
         }
      } finally { Directory.Delete(temporary, true); }
   }
   private static List<IpsRecord> ReadIps(byte[] patch) {
      var result = new List<IpsRecord>();int offset = 5;
      while (!patch.AsSpan(offset, 3).SequenceEqual("EOF"u8)) {
         int address = (patch[offset] << 16) | (patch[offset + 1] << 8) | patch[offset + 2];
         int size = (patch[offset + 3] << 8) | patch[offset + 4];offset += 5;
         byte[] bytes;
         if (size != 0) { bytes = patch.AsSpan(offset, size).ToArray();offset += size; }
         else { size = (patch[offset] << 8) | patch[offset + 1];bytes = Enumerable.Repeat(patch[offset + 2], size).ToArray();offset += 3; }
         result.Add(new(address, bytes));
      }
      if (offset + 3 != patch.Length) throw new InvalidDataException("This review expects the supplied no-truncation IPS.");
      return result;
   }
}
