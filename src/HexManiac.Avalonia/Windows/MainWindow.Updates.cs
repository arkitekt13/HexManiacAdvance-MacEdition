using System;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using HavenSoft.HexManiac.AvaloniaUI.Updates;

[assembly: System.Runtime.CompilerServices.InternalsVisibleTo("UpdateNotice")]

namespace HavenSoft.HexManiac.AvaloniaUI.Windows;

public partial class MainWindow {
   private bool checkingUpdates;
   private string manualUpdateMessage;
   private CancellationTokenSource manualUpdateCancellation;
   internal bool ManualUpdateCheckRequested { get; private set; }
   internal Func<CancellationToken, Task<bool?>> ManualUpdateRequest { get; set; } = RequestFreshUpdateAsync;

   private static async Task<bool?> RequestFreshUpdateAsync(CancellationToken token) {
      using var client = new HttpClient();
      var checker = new MacUpdateChecker(client, MacUpdateChecker.DefaultCachePath,
         AppIdentity.VersionNumber, RuntimeInformation.ProcessArchitecture);
      return await Task.Run(() => checker.CheckAsync(token, forceRefresh: true), token);
   }

   private async void CheckForUpdatesClick(object sender, EventArgs e) => await CheckForUpdatesAsync();

   internal async Task CheckForUpdatesAsync() {
      if (checkingUpdates || editor == null) return;
      var owner = editor;
      using var cancellation = new CancellationTokenSource();
      manualUpdateCancellation = cancellation;
      void ClosedWhileChecking(object sender, EventArgs e) => cancellation.Cancel();
      Closed += ClosedWhileChecking;
      checkingUpdates = true;
      ManualUpdateCheckRequested = true;
      updateNoticeDismissed = false;
      manualUpdateMessage = "Checking for updates…";
      var menu = FindMenuItem(NativeMenu.GetMenu(this), "Check for Updates…");
      if (menu != null) menu.IsEnabled = false;
      RefreshUpdateNotice();
      try {
         var available = await ManualUpdateRequest(cancellation.Token);
         if (cancellation.IsCancellationRequested || !ReferenceEquals(editor, owner)) return;
         if (available != null) owner.IsNewVersionAvailable = available.Value;
         manualUpdateMessage = available switch {
            true => "A newer version of Mac Edition is available.",
            false => "Mac Edition " + AppIdentity.VersionNumber + " is up to date.",
            null => "Could not check for updates. Check your internet connection and try again.",
         };
      } catch (OperationCanceledException) {
         // Closing/replacing the window cancels quietly; no project data is touched.
      } catch (Exception ex) when (ex is HttpRequestException or System.IO.IOException) {
         if (!cancellation.IsCancellationRequested && ReferenceEquals(editor, owner))
            manualUpdateMessage = "Could not check for updates. Check your internet connection and try again.";
      } finally {
         Closed -= ClosedWhileChecking;
         if (ReferenceEquals(manualUpdateCancellation, cancellation)) manualUpdateCancellation = null;
         checkingUpdates = false;
         if (menu != null) menu.IsEnabled = true;
         RefreshUpdateNotice();
      }
   }
}
