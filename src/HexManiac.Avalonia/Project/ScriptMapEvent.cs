using System;
using Avalonia;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Map;

namespace HavenSoft.HexManiac.AvaloniaUI.Project;

public enum ScriptMapEventKind { Object, Script }

// Values only: a displayed preview must not retain the detached ROM or live event editors.
public sealed record ScriptMapEvent(int MapID, ScriptMapEventKind Kind, int Index, int RecordAddress,
   int X, int Y, int? ObjectID, int? SpriteID, Rect Bounds) {
   public string Label => Kind == ScriptMapEventKind.Object
      ? $"Object {ObjectID} · Sprite {SpriteID}" : $"Script event {Index + 1}";

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
      var rows = events[0].GetSubTable(Kind == ScriptMapEventKind.Object ? "objects" : "scripts");
      if (rows == null || Index < 0 || Index >= rows.Count) return false;
      var row = rows[Index];
      if (row.Start != RecordAddress || !row.HasField("x") || !row.HasField("y") || !row.HasField("script") ||
          row.GetValue("x") != X || row.GetValue("y") != Y) return false;
      if (Kind == ScriptMapEventKind.Object && (!row.HasField("id") || !row.HasField("graphics") || row.GetValue("id") != ObjectID)) return false;
      int? sprite = Kind == ScriptMapEventKind.Object ? row.GetValue("graphics") : null;
      destination = new ScriptMapDestination(Kind, Index, ObjectID, sprite, row.GetAddress("script"));
      return true;
   }
}

public sealed record ScriptMapDestination(ScriptMapEventKind Kind, int Index, int? ObjectID, int? SpriteID, int Address) {
   public string Description => (Kind == ScriptMapEventKind.Object ? $"Object {ObjectID} · Sprite {SpriteID}" : $"Script event {Index + 1}") +
      (Address < 0 ? " · No script" : $" · Script {Address:X6}");
}
