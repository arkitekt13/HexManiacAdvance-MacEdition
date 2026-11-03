using System.Text;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.Core;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

namespace HavenSoft.HexManiac.Review;

// All bytes, scripts and addresses are fabricated. No supplied ROM or native UI.
public sealed class WorkflowFixture : IDisposable {
   public const int Root = 0x1000, Other = 0x1800, Text = 0x3000;
   public static readonly int[] Callers = { 0x100, 0x200 };
   public static readonly byte[] Sentinel = { 0xDE, 0xAD, 0xBE, 0xEF, 0x91, 0x82, 0x73, 0x64 };
   public WorkflowDispatch Dispatch { get; } = new();
   public Singletons Services { get; }
   public PokemonModel Model { get; }
   public ViewPort View { get; }
   public CodeTool Tool => View.Tools.CodeTool;
   public CodeBody Body => Tool.Contents[0];
   public string Folder { get; } = Path.Combine(Path.GetTempPath(), "hma-script-workflow-" + Guid.NewGuid().ToString("N"));
   public string FileName => Path.Combine(Folder, "synthetic.gba");
   public string MetadataFile => Path.ChangeExtension(FileName, ".toml");
   public StubFileSystem FileSystem { get; }
   public int Saves { get; private set; }
   public int Prompts { get; private set; }
   public List<string> Messages { get; } = new();
   public List<string> CompileErrors { get; } = new();
   public int Neighbor { get; }
   public int TextNeighbor { get; }
   public WorkflowFixture() {
      Directory.CreateDirectory(Folder);
      Services = new Singletons(Dispatch);
      var data = Enumerable.Repeat((byte)0xFF, 0x10000).ToArray();
      Encoding.ASCII.GetBytes("BPRE").CopyTo(data, 0xAC); data[0xBC] = 0;
      Model = new PokemonModel(data, singletons: Services);
      Model.LoadMetadata(BaseModel.GetDefaultMetadatas().First());
      var parser = new ScriptParser(Model.GetShortGameCode(), Services.ScriptLines, 2);
      var token = new ModelDelta();
      void Seed(int address, string source, string name) {
         var bytes = parser.Compile(token, Model, address, ref source, out _, out _);
         if (bytes == null) throw new InvalidOperationException("Fixture seed failed.");
         token.ChangeData(Model, address, bytes);
         parser.FormatScript<XSERun>(token, Model, address, bytes.Length);
         Model.ObserveAnchorWritten(token, name, new XSERun(address));
      }
      Seed(Root, Source(1, 1), "workflow.root");
      Seed(Other, Source(1, 7, "Other NPC.", 0x3800), "workflow.other");
      Model.ObserveAnchorWritten(token, "workflow.dialogue", Model.GetNextRun(Text));
      foreach (var caller in Callers) {
         token.ChangeData(Model, caller, new byte[] { 4, 0, 0, 0, 8, 2 });
         Model.WritePointer(token, caller + 1, Root);
         parser.FormatScript<XSERun>(token, Model, caller);
      }
      Neighbor = Root + parser.FindLength(Model, Root);
      TextNeighbor = Text + Model.GetNextRun(Text).Length;
      token.ChangeData(Model, Neighbor, Sentinel);
      token.ChangeData(Model, Root - 8, Sentinel);
      token.ChangeData(Model, TextNeighbor, Sentinel);
      Model.ObserveAnchorWritten(token, "workflow.neighbor", new NoInfoRun(Neighbor));
      FileSystem = new StubFileSystem {
         Save = file => { AtomicFileWriter.Write(file.Name, stream => stream.Write(file.Contents)); Saves++; return true; },
         SaveMetadata = (name, lines) => { AtomicFileWriter.WriteAllLines(Path.ChangeExtension(name, ".toml"), lines); return true; },
         RequestNewName = (_, _, _) => FileName,
         TrySavePrompt = _ => { Prompts++; return null; },
      };
      View = new ViewPort(FileName, Model, Dispatch, Services, fs: FileSystem);
      View.OnError += (_, message) => Messages.Add(message);
      View.InitializationWorkload.GetAwaiter().GetResult();
      View.Goto.Execute(Root); Tool.Mode = CodeMode.Script; Tool.IsSelected = true;
      Dispatch.Flush();
      View.ChangeHistory.ClearHistory(false); View.ChangeHistory.TagAsSaved(); Dispatch.Flush();
      Tool.ScriptParser.CompileError += (_, error) => CompileErrors.Add(error.Message);
      AtomicFileWriter.Write(FileName, stream => stream.Write(Model.RawData));
      AtomicFileWriter.WriteAllLines(MetadataFile, Model.ExportMetadata(null, Services.MetadataInfo).Serialize());
   }
   public static string Source(int count, int value, string text = "Hello!", int textAddress = Text) =>
      "lock\n" + string.Concat(Enumerable.Repeat($"setvar 0x4001 {value}\n", count)) +
      $"loadpointer 0 <{textAddress:X6}>\n{{\n{text}\n}}\nrelease\nend";
   public void Edit(string source, bool settle = true) {
      Body.Editor.Content = source;
      if (settle) { Dispatch.Flush(); View.ChangeHistory.ChangeCompleted(); }
   }
   public WorkflowState State() => new(Model.RawData.ToArray(), Metadata());
   public string[] Metadata() => Model.ExportMetadata(null, Services.MetadataInfo).Serialize()
      .Where(line => !line.StartsWith("FreeSpaceSearch = ", StringComparison.Ordinal)).ToArray();
   public PokemonModel Reopen() {
      var model = new PokemonModel(File.ReadAllBytes(FileName), new StoredMetadata(File.ReadAllLines(MetadataFile)), Services);
      model.InitializationWorkload.GetAwaiter().GetResult();
      return model;
   }
   public void Dispose() { Dispatch.Clear(); Directory.Delete(Folder, true); }
}
public record WorkflowState(byte[] Bytes, string[] Metadata) {
   public bool EqualsModel(WorkflowFixture fixture) => Bytes.SequenceEqual(fixture.Model.RawData) && Metadata.SequenceEqual(fixture.Metadata());
}

// Preserve production debounce/coalescing while exposing deterministic boundaries.
// This runs scheduled callbacks synchronously; it does not simulate thread races.
public sealed class WorkflowDispatch : IWorkDispatcher {
   readonly List<Timer> timers = new();
   public int Executed { get; private set; }
   public int Replaced { get; private set; }
   public int Pending => timers.Count(t => t.Action != null);
   public Action[] CapturePendingCallbacks() => timers.Where(t => t.Action != null).Select(t => t.Action).ToArray();
   public void Flush() {
      for (int pass = 0; Pending > 0; pass++) {
         if (pass >= 32) throw new InvalidOperationException("Deferred work did not settle.");
         foreach (var timer in timers.ToArray()) {
            var action = timer.Action; timer.Action = null;
            if (action != null) { Executed++; action(); }
         }
      }
   }
   public void Clear() { foreach (var timer in timers) timer.Reset(); }
   public Task WaitForRenderingAsync() => Task.CompletedTask;
   public void BlockOnUIWork(Action action) => action();
   public Task DispatchWork(Action action) { action?.Invoke(); return Task.CompletedTask; }
   public Task RunBackgroundWork(Action action) => DispatchWork(action);
   public IDelayWorkTimer CreateDelayTimer() { var timer = new Timer(this); timers.Add(timer); return timer; }
   sealed class Timer(WorkflowDispatch owner) : IDelayWorkTimer {
      public Action Action;
      public bool HasScheduledWork => Action != null;
      public DelayWorkResult DelayCall(TimeSpan delay, Action action) {
         bool replace = Action != null; if (replace) owner.Replaced++;
         Action = action; return replace ? DelayWorkResult.WorkScheduledAndPreviousWorkCleared : DelayWorkResult.WorkScheduled;
      }
      public void Reset() => Action = null;
   }
}
