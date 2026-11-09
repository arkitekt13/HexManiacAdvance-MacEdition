using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Updates;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new Exception(message); }
byte[] Releases(string tag = "v0.6.2", string asset = "HexManiacAdvance-osx-arm64.zip", bool draft = false, bool prerelease = false,
   string repo = AppIdentity.RepositoryUrl, string state = "uploaded", long size = 2000) => JsonSerializer.SerializeToUtf8Bytes(new[] { new {
      tag_name = tag, draft, prerelease, html_url = repo + "/releases/tag/" + tag,
      assets = new[] { new { name = asset, state, size, browser_download_url = repo + "/releases/download/" + tag + "/" + asset } }
   }});
bool Available(byte[] json, string current = "0.6.1", Architecture arch = Architecture.Arm64) => MacUpdateChecker.HasCompatibleUpdate(json, current, arch);
Check(AppIdentity.VersionNumber == "1.0.0", "The Mac updater reads the independent 1.0.0 app version.");
Check(!Available(Releases("v1.0.0"),AppIdentity.VersionNumber) && !Available(Releases("v0.6.2"),AppIdentity.VersionNumber), "Same Mac release and newer upstream 0.x versions do not update Mac 1.0.");
Check(Available(Releases("v1.0.0"),"0.6.1"), "Existing 0.6.1 clients can discover the Mac 1.0 release.");
Check(Available(Releases("v1.0.1"),AppIdentity.VersionNumber), "A later Mac 1.0.1 release is discovered.");
Check(Available(Releases()), "Newer ARM64 Mac release is available.");
Check(!Available(Releases("v0.6.1")) && !Available(Releases("v0.5.9")), "Same and older releases are not updates.");
Check(!Available(Releases("v0.6.1.0")), "Normalize omitted revision to zero.");
Check(Available(Releases("v0.6.1.1")), "Revision changes are updates.");
Check(!Available(Releases("v0.6.2-beta")), "Unrecognized/preview tags are ignored.");
Check(!Available(Releases(draft:true)) && !Available(Releases(prerelease:true)), "Do not offer drafts or prereleases.");
Check(!Available(Releases(asset:"HexManiacAdvance-win-x64.zip")), "Windows artifacts are ignored.");
Check(!Available(Releases(asset:"HexManiacAdvance-osx-x64.zip")), "Wrong Mac architecture is ignored.");
Check(Available(Releases(asset:"HexManiacAdvance-macos-x86_64.dmg"), arch:Architecture.X64), "Intel Mac archive recognized.");
Check(Available(Releases(asset:"HexManiacAdvance-mac-universal.zip"), arch:Architecture.X64) &&
   Available(Releases(asset:"HexManiacAdvance-mac-universal.zip")), "Universal Mac archive supports both architectures.");
Check(!Available(Releases(asset:"HexManiacAdvance-mac-universal.zip"), arch:Architecture.Arm), "Unsupported architectures are ignored.");
Check(!Available(Releases(asset:"HexManiacAdvance-mac-arm64-source.zip")), "Source archives are not application updates.");
Check(!Available(Releases(asset:"HexManiacAdvance-mac.zip")), "Ambiguous architecture is not offered.");
Check(!Available(Releases(repo:"https://github.com/haven1433/HexManiacAdvance")), "Upstream release cannot become a Mac fork update.");
Check(!Available(Releases(asset:"HexManiacAdvance-osx-arm64.txt")), "Non-installable attachment ignored.");
Check(!Available(Releases(state:"new")) && !Available(Releases(size:0)), "Incomplete or empty downloads ignored.");
Check(!Available(Encoding.UTF8.GetBytes("[]")), "No published releases is a normal no-update result.");

var root = Path.Combine(Path.GetTempPath(), "hma-updates-" + Guid.NewGuid().ToString("N"));
Directory.CreateDirectory(root);
try {
   var now = DateTimeOffset.UtcNow;
   var payload = Releases();
   var handler = new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(payload) }));
   using var client = new HttpClient(handler);
   var cache = Path.Combine(root, "cache.json");
   MacUpdateChecker Checker(string path = null, string version = "0.6.1", Architecture arch = Architecture.Arm64) =>
      new(client, path ?? cache, version, arch, () => now);
   Check(await Checker().CheckAsync() == true && handler.Calls == 1, "Successful request offers compatible update.");
   Check(handler.Url == MacUpdateChecker.ApiUrl && handler.UserAgent.Contains("HexManiacAdvance-Mac/0.6.1"), "Request targets fork API with required client identity.");
   Check(await Checker().CheckAsync() == true && handler.Calls == 1, "Reuse fresh fork-specific cache without network.");
   now = now.AddDays(1);
   Check(await Checker().CheckAsync() == true && handler.Calls == 2, "Refresh at daily boundary.");
   Check(await Checker(version:"0.6.2").CheckAsync() == false && handler.Calls == 3, "App upgrade invalidates old cache.");
   Check(await Checker(arch:Architecture.X64).CheckAsync() == false && handler.Calls == 4, "Architecture change invalidates cache.");
   var foreignCache = Path.Combine(root,"foreign.json");
   File.WriteAllText(foreignCache, JsonSerializer.Serialize(new { Repository="https://github.com/haven1433/HexManiacAdvance", Version="0.6.1", Architecture="Arm64", CheckedUtc=now, Available=true }));
   payload = Encoding.UTF8.GetBytes("[]");
   Check(await Checker(foreignCache).CheckAsync() == false, "Never reuse an upstream cache flag.");
   payload = Releases();
   var beforeCorrupt = handler.Calls;
   File.WriteAllText(cache, "corrupted cache");
   Check(await Checker().CheckAsync() == true && handler.Calls == beforeCorrupt + 1, "Corrupt cache recovers through network.");
   now = now.AddDays(-2);
   Check(await Checker().CheckAsync() == true && handler.Calls == beforeCorrupt + 2, "Clock rollback does not freeze refresh.");
   var pending = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
   using var delayed = new HttpClient(new FakeHandler((_, _) => pending.Task));
   var delayedCheck = new MacUpdateChecker(delayed, Path.Combine(root,"delayed.json"), "0.6.1", Architecture.Arm64).CheckAsync();
   Check(!delayedCheck.IsCompleted, "Delayed network request yields instead of blocking caller.");
   pending.SetResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new ByteArrayContent(Releases()) });
   Check(await delayedCheck == true, "Delayed response completes normally.");
   foreach (var status in new[] { HttpStatusCode.Forbidden, HttpStatusCode.NotFound, HttpStatusCode.TooManyRequests, HttpStatusCode.InternalServerError }) {
      using var failed = new HttpClient(new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(status))));
      var path = Path.Combine(root, "failed-" + (int)status + ".json");
      Check(await new MacUpdateChecker(failed, path, "0.6.1", Architecture.Arm64).CheckAsync() == null && !File.Exists(path), "Failed HTTP request is not cached as success: " + status);
   }
   foreach (var bad in new[] { "{bad json", "{}", new string('x', 1024 * 1024 + 1) }) {
      using var invalid = new HttpClient(new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(bad) })));
      var path = Path.Combine(root, "bad.json");
      Check(await new MacUpdateChecker(invalid, path, "0.6.1", Architecture.Arm64).CheckAsync() == null && !File.Exists(path), "Malformed or oversized response is quiet and not cached.");
   }
   using var chunked = new HttpClient(new FakeHandler((_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) {
      Content = new StreamContent(new StreamingBytes(new byte[1024 * 1024 + 1]))
   })));
   Check(await new MacUpdateChecker(chunked, Path.Combine(root,"chunked.json"), "0.6.1", Architecture.Arm64).CheckAsync() == null,
      "Enforce streamed body limit even without Content-Length.");
   using var offline = new HttpClient(new FakeHandler((_, _) => throw new HttpRequestException("offline")));
   Check(await new MacUpdateChecker(offline, Path.Combine(root,"offline.json"), "0.6.1", Architecture.Arm64).CheckAsync() == null, "Offline startup is quiet.");
   now = now.AddDays(2);
   var oldCache = File.ReadAllText(cache);
   Check(await new MacUpdateChecker(offline, cache, "0.6.1", Architecture.Arm64, () => now).CheckAsync() == true && File.ReadAllText(cache) == oldCache,
      "Failed refresh preserves previous Mac result and does not advance success timestamp.");
   using var stalled = new HttpClient(new FakeHandler(async (_, token) => {
      await Task.Delay(Timeout.InfiniteTimeSpan, token); return new HttpResponseMessage(HttpStatusCode.OK);
   }));
   Check(await new MacUpdateChecker(stalled, Path.Combine(root,"timeout.json"), "0.6.1", Architecture.Arm64, timeout:TimeSpan.FromMilliseconds(50)).CheckAsync() == null,
      "Bounded timeout ends stalled request quietly.");
   using var cancel = new CancellationTokenSource();
   var cancellationCheck = new MacUpdateChecker(stalled, Path.Combine(root,"cancel.json"), "0.6.1", Architecture.Arm64).CheckAsync(cancel.Token);
   cancel.Cancel();
   Check(await cancellationCheck == null && !File.Exists(Path.Combine(root,"cancel.json")), "Shutdown cancellation stops pending check without cache writes.");
   var blocked = Path.Combine(root,"blocked"); File.WriteAllText(blocked,"keep");
   Check(await Checker(Path.Combine(blocked,"cache.json")).CheckAsync() == true && File.ReadAllText(blocked) == "keep", "Cache write failure does not suppress a valid result.");
   Directory.SetCurrentDirectory(Path.GetDirectoryName(typeof(App).Assembly.Location)!);
   var editor = new EditorViewModel(new StubFileSystem { MetadataFor = _ => Array.Empty<string>() });
   var notifications = 0;
   editor.PropertyChanged += (_, e) => { if (e.PropertyName == nameof(editor.IsNewVersionAvailable)) notifications++; };
   editor.IsNewVersionAvailable = true;
   editor.IsNewVersionAvailable = true;
   editor.IsNewVersionAvailable = false;
   Check(notifications == 2, "Update binding receives asynchronous flag changes only when value changes.");
} finally { Directory.Delete(root, true); }
Console.WriteLine("PASS: compatible fork releases, daily cache, yielding network requests, notification, timeout, cancellation and failure behavior.");
Console.WriteLine($"CHECKS_PASSED:{checks}");

sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respond) : HttpMessageHandler {
   public int Calls;
   public string Url, UserAgent;
   protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) {
      Calls++; Url = request.RequestUri!.ToString(); UserAgent = request.Headers.UserAgent.ToString();
      return respond(request, cancellationToken);
   }
}

sealed class StreamingBytes(byte[] data) : MemoryStream(data) { public override bool CanSeek => false; }
