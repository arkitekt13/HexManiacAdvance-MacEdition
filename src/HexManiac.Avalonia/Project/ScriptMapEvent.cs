using System;
using Avalonia;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;

namespace HavenSoft.HexManiac.AvaloniaUI.Project;

public enum ScriptMapEventKind { Object, Script, Signpost }

// Values only: a displayed preview must not retain the detached ROM or live event editors.
public sealed record ScriptMapEvent(int MapID, ScriptMapEventKind Kind, int Index, int RecordAddress,
   int X, int Y, int? ObjectID, int? SpriteID, Rect Bounds, int? SignpostKind = null) {
   public string Label => Kind switch {
      ScriptMapEventKind.Object => $"Object {ObjectID} · Sprite {SpriteID}",
      ScriptMapEventKind.Signpost => $"Signpost {Index + 1}",
      _ => $"Script event {Index + 1}",
   };

   public bool TryResolve(ViewPort source, out ScriptMapDestination destination) {
      destination = null;
      try { return Resolve(source, out destination); }
      catch (Exception ex) when (ex is ArgumentException or InvalidOperationException or IndexOutOfRangeException or NotImplementedException) { return false; }
   }

   private bool Resolve(ViewPort source, out ScriptMapDestination destination) {
      destination = null;
      if (MapID < 0 || source.ScriptReferenceMapID != MapID || source.UpdateInProgress || !source.InitializationWorkload.IsCompleted) return false;
      var header = BlockMapViewModel.GetMapModel(source.Model, MapID / 1000, MapID % 1000, () => new NoDataChangeDeltaModel());
      var events = header?.GetSubTable("events");
      if (events == null || events.Count == 0) return false;
      string countField = Kind switch { ScriptMapEventKind.Object => "objectCount", ScriptMapEventKind.Signpost => "signpostCount", _ => "scriptCount" };
      if (!events[0].HasField(countField) || Index < 0 || Index >= events[0].GetValue(countField)) return false;
      var rows = events[0].GetSubTable(Kind switch { ScriptMapEventKind.Object => "objects", ScriptMapEventKind.Signpost => "signposts", _ => "scripts" });
      if (rows == null || Index < 0 || Index >= rows.Count) return false;
      var row = rows[Index];
      string pointerField = Kind == ScriptMapEventKind.Signpost ? "arg" : "script";
      if (row.Start != RecordAddress || !row.HasField("x") || !row.HasField("y") || !row.HasField(pointerField) ||
          row.GetValue("x") != X || row.GetValue("y") != Y) return false;
      if (Kind == ScriptMapEventKind.Object && (!row.HasField("id") || !row.HasField("graphics") || row.GetValue("id") != ObjectID)) return false;
      if (Kind == ScriptMapEventKind.Signpost && (!row.HasField("kind") || row.GetValue("kind") != SignpostKind)) return false;
      // Signpost arg is a union. Item/secret-base payloads must never be read as pointers.
      bool script = Kind != ScriptMapEventKind.Signpost || SignpostKind is >= 0 and <= 4;
      int? sprite = Kind == ScriptMapEventKind.Object ? row.GetValue("graphics") : null;
      destination = new ScriptMapDestination(Kind, Index, ObjectID, sprite, script ? row.GetAddress(pointerField) : -1, SignpostKind);
      return true;
   }
}

public sealed record ScriptMapDestination(ScriptMapEventKind Kind, int Index, int? ObjectID, int? SpriteID, int Address, int? SignpostKind = null) {
   public bool IsScript => Kind != ScriptMapEventKind.Signpost || SignpostKind is >= 0 and <= 4;
   public string Description {
      get {
         if (!IsScript) return $"Signpost {Index + 1} · " + (SignpostKind is >= 5 and <= 7 ? "Hidden item" : SignpostKind == 8 ? "Secret base" : "Unsupported kind") + " — no script";
         var label = Kind switch {
            ScriptMapEventKind.Object => $"Object {ObjectID} · Sprite {SpriteID}",
            ScriptMapEventKind.Signpost => $"Signpost {Index + 1}",
            _ => $"Script event {Index + 1}",
         };
         return label + (Address < 0 ? " · No script" : $" · Script {Address:X6}");
      }
   }
}
