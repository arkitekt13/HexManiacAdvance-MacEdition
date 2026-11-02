using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using HavenSoft.HexManiac.Core.Models;

namespace HavenSoft.HexManiac.AvaloniaUI.Updates;

/// <summary>Optional, bounded update discovery; never downloads or installs a release.</summary>
public sealed class MacUpdateChecker {
   public const string ApiUrl = "https://api.github.com/repos/arkitekt13/HexManiacAdvance-mac/releases?per_page=30";
   public static string DefaultCachePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
      "HexManiacAdvance", "mac-update-check.json");
   private const int MaximumResponseBytes = 1024 * 1024;
   private readonly HttpClient client;
   private readonly string cachePath, version;
   private readonly Architecture architecture;
   private readonly Func<DateTimeOffset> utcNow;
   private readonly TimeSpan timeout;
   private record Cache(string Repository, string Version, string Architecture, DateTimeOffset CheckedUtc, bool Available);

   public MacUpdateChecker(HttpClient client, string cachePath, string version, Architecture architecture,
      Func<DateTimeOffset> utcNow = null, TimeSpan? timeout = null) {
      this.client = client;
      this.cachePath = cachePath;
      this.version = version;
      this.architecture = architecture;
      this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
      this.timeout = timeout ?? TimeSpan.FromSeconds(5);
   }

   /// <summary>Null means no trustworthy result; a failed request never becomes a daily cache hit.</summary>
   public async Task<bool?> CheckAsync(CancellationToken cancellationToken = default) {
      Cache cached = null;
      try {
         if (File.Exists(cachePath)) cached = JsonSerializer.Deserialize<Cache>(File.ReadAllText(cachePath));
         if (cached?.Repository != AppIdentity.RepositoryUrl || cached.Version != version || cached.Architecture != architecture.ToString()) cached = null;
      } catch (Exception error) when (error is IOException or UnauthorizedAccessException or JsonException or ArgumentException) { }
      var now = utcNow();
      if (cancellationToken.IsCancellationRequested) return null;
      if (cached != null && cached.CheckedUtc <= now && now - cached.CheckedUtc < TimeSpan.FromDays(1)) return cached.Available;
      using var bounded = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
      bounded.CancelAfter(timeout);
      try {
         using var request = new HttpRequestMessage(HttpMethod.Get, ApiUrl);
         request.Headers.UserAgent.ParseAdd("HexManiacAdvance-Mac/" + version);
         request.Headers.Accept.ParseAdd("application/vnd.github+json");
         request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
         using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, bounded.Token).ConfigureAwait(false);
         // List releases returns an empty array for a repository without releases; 404 is not a successful check.
         if (response.StatusCode != HttpStatusCode.OK) return cached?.Available;
         if (response.Content.Headers.ContentLength > MaximumResponseBytes) return cached?.Available;
         using var stream = await response.Content.ReadAsStreamAsync(bounded.Token).ConfigureAwait(false);
         using var body = new MemoryStream();
         var buffer = new byte[8192];
         int count;
         while ((count = await stream.ReadAsync(buffer, bounded.Token).ConfigureAwait(false)) != 0) {
            if (body.Length + count > MaximumResponseBytes) return cached?.Available;
            body.Write(buffer, 0, count);
         }
         bounded.Token.ThrowIfCancellationRequested();
         var available = HasCompatibleUpdate(body.ToArray(), version, architecture);
         try {
            AtomicFileWriter.WriteAllLines(cachePath, new[] { JsonSerializer.Serialize(new Cache(
               AppIdentity.RepositoryUrl, version, architecture.ToString(), now, available)) });
         } catch (Exception error) when (error is IOException or UnauthorizedAccessException or ArgumentException) { }
         return available;
      } catch (OperationCanceledException) {
         return cancellationToken.IsCancellationRequested ? null : cached?.Available;
      } catch (Exception error) when (error is HttpRequestException or IOException or JsonException or InvalidOperationException or ArgumentException) {
         return cached?.Available;
      }
   }

   public static bool HasCompatibleUpdate(byte[] json, string currentVersion, Architecture architecture) {
      if (!TryVersion(currentVersion, out var current)) throw new ArgumentException("Unrecognized app version.");
      using var document = JsonDocument.Parse(json);
      if (document.RootElement.ValueKind != JsonValueKind.Array) throw new JsonException("Expected a release list.");
      return document.RootElement.EnumerateArray().Any(release => {
         if (release.ValueKind != JsonValueKind.Object ||
             !release.TryGetProperty("draft", out var draft) || draft.ValueKind != JsonValueKind.False ||
             !release.TryGetProperty("prerelease", out var preview) || preview.ValueKind != JsonValueKind.False ||
             !release.TryGetProperty("tag_name", out var tag) || tag.ValueKind != JsonValueKind.String ||
             !TryVersion(tag.GetString(), out var candidate) || candidate <= current ||
             !release.TryGetProperty("html_url", out var url) || url.ValueKind != JsonValueKind.String ||
             !url.GetString().StartsWith(AppIdentity.RepositoryUrl + "/releases/tag/", StringComparison.Ordinal) ||
             !release.TryGetProperty("assets", out var assets) || assets.ValueKind != JsonValueKind.Array) return false;
         return assets.EnumerateArray().Any(asset => IsCompatibleAsset(asset, architecture));
      });
   }

   private static bool TryVersion(string text, out Version version) {
      version = null;
      if (text == null || !Regex.IsMatch(text, @"^v?\d+\.\d+\.\d+(\.\d+)?$", RegexOptions.CultureInvariant)) return false;
      if (!Version.TryParse(text.TrimStart('v'), out var parsed)) return false;
      version = new Version(parsed.Major, parsed.Minor, parsed.Build, Math.Max(0, parsed.Revision));
      return true;
   }

   private static bool IsCompatibleAsset(JsonElement asset, Architecture architecture) {
      if (architecture is not (Architecture.Arm64 or Architecture.X64)) return false;
      if (asset.ValueKind != JsonValueKind.Object || !asset.TryGetProperty("name", out var name) || name.ValueKind != JsonValueKind.String ||
          !asset.TryGetProperty("state", out var state) || state.GetString() != "uploaded" ||
          !asset.TryGetProperty("size", out var size) || !size.TryGetInt64(out var length) || length <= 0 ||
          !asset.TryGetProperty("browser_download_url", out var url) || url.ValueKind != JsonValueKind.String ||
          !url.GetString().StartsWith(AppIdentity.RepositoryUrl + "/releases/download/", StringComparison.Ordinal)) return false;
      var file = name.GetString().ToLowerInvariant();
      if (!(file.EndsWith(".zip") || file.EndsWith(".dmg") || file.EndsWith(".pkg") || file.EndsWith(".tar.gz"))) return false;
      var tokens = Regex.Split(file, @"[^a-z0-9]+");
      if (tokens.Any(t => t is "source" or "sources" or "symbols")) return false;
      if (!tokens.Any(t => t is "mac" or "macos" or "osx" or "darwin")) return false;
      return tokens.Contains("universal") || architecture switch {
         Architecture.Arm64 => tokens.Contains("arm64") || tokens.Contains("aarch64"),
         Architecture.X64 => tokens.Contains("x64") || tokens.Contains("amd64") || file.Contains("x86_64"),
         _ => false,
      };
   }
}
