using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Map;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HexManiac.Core.Models.Runs.Sprites;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;

namespace HavenSoft.HexManiac.Core.ViewModels.Map {
   public class MapHeaderViewModel : ViewModelCore, INotifyPropertyChanged {
      private ModelArrayElement map;
      private readonly Func<ModelDelta> tokenFactory;
      private readonly Format format;
      // music: layoutID: regionSectionID. cave. weather. mapType. allowBiking. flags.|t|allowEscaping.|allowRunning.|showMapName::: floorNum. battleType.

      private static readonly ObservableCollection<string> weatherOptions = new();
      private static readonly ObservableCollection<string> caveOptions = new();
      private static readonly ObservableCollection<string> battleOptions = new();
      static MapHeaderViewModel() {
         // weather
         weatherOptions.Add("Indoor");
         weatherOptions.Add("Sunny Clouds");
         weatherOptions.Add("Outdoor");
         weatherOptions.Add("Rain");
         weatherOptions.Add("Snow");
         weatherOptions.Add("Thunderstorm");
         weatherOptions.Add("Fog - Horizontal");
         weatherOptions.Add("Ash");
         weatherOptions.Add("Sandstorm");
         weatherOptions.Add("Fog - Diagonal");
         weatherOptions.Add("Underwater");
         weatherOptions.Add("Shade");
         weatherOptions.Add("Drought");
         weatherOptions.Add("Downpour");
         weatherOptions.Add("Underwater - Bubbles");
         weatherOptions.Add("Alternating Storm");
         weatherOptions.Add("16 - Unused");
         weatherOptions.Add("17 - Unused");
         weatherOptions.Add("18 - Unused");
         weatherOptions.Add("19 - Unused");
         weatherOptions.Add("Cycle - Route 119");
         weatherOptions.Add("Cycle - Route 123");

         // cave
         caveOptions.Add("Normal");
         caveOptions.Add("Flash Usable");
         caveOptions.Add("Flash Not Usable");

         // battle
         battleOptions.Add("Normal");
         battleOptions.Add("Gym");
         battleOptions.Add("Indoor 1 (Magma)");
         battleOptions.Add("Indoor 2 (Aqua)");
         battleOptions.Add("Elite 1");
         battleOptions.Add("Elite 2");
         battleOptions.Add("Elite 3");
         battleOptions.Add("Elite 4");
         battleOptions.Add("Big Red Poké ball");
      }

      public ObservableCollection<string> WeatherOptions => weatherOptions;

      public ObservableCollection<string> CaveOptions => caveOptions;

      public ObservableCollection<string> BattleOptions => battleOptions;

      public MapHeaderViewModel(ModelArrayElement element, Format format, Func<ModelDelta> tokens) {
         (map, this.format, tokenFactory) = (element, format, tokens);
         if (element == null) return;
         if (element.Model.TryGetList("songnames", out var songnames)) {
            for (int i = 0; i < songnames.Count; i++) {
               var name = songnames[i] ?? $"song_{i}";
               MusicOptions.Add(name);
            }
         }
         if (element.Model.TryGetList("maptypes", out var mapTypes)) {
            foreach (var name in mapTypes) MapTypeOptions.Add(name);
         }
         Refresh();
      }

      private int blocksetSelectionDepth;
      private bool synchronizingBlocksets, blocksetRefreshPending;

      public void UpdateFromModel() {
         if (synchronizingBlocksets) return;
         if (blocksetSelectionDepth > 0) { blocksetRefreshPending = true; return; }
         blocksetRefreshPending = false;
         if (map == null) return;
         var layoutTable = map.GetSubTable(Format.Layout);
         if (layoutTable == null) return;
         var layout = layoutTable[0];
         var primaryAddress = layout.GetAddress(Format.PrimaryBlockset);
         var secondaryAddress = layout.GetAddress(Format.SecondaryBlockset);
         // An open dropdown can retain a valid choice after another header rebuilds
         // the shared cache. The committed pointers are authoritative.
         if (!format.BlocksetCache.Primary.Any(option => option.Address == primaryAddress) ||
             !format.BlocksetCache.Secondary.Any(option => option.Address == secondaryAddress)) format.Refresh();
         var primary = format.BlocksetCache.Primary;
         var secondary = format.BlocksetCache.Secondary;
         var newPrimary = primary.IndexOf(primary.FirstOrDefault(option => option.Address == primaryAddress));
         var newSecondary = secondary.IndexOf(secondary.FirstOrDefault(option => option.Address == secondaryAddress));
         if (OptionsCurrent(PrimaryOptions, primary) && OptionsCurrent(SecondaryOptions, secondary) &&
             primaryIndex == newPrimary && secondaryIndex == newSecondary) return;

         synchronizingBlocksets = true;
         try {
            var primaryChanged = SynchronizeOptions(PrimaryOptions, primary);
            var secondaryChanged = SynchronizeOptions(SecondaryOptions, secondary);
            var oldPrimary = primaryIndex;
            var oldSecondary = secondaryIndex;
            (primaryIndex, secondaryIndex) = (newPrimary, newSecondary);
            if (primaryChanged || oldPrimary != newPrimary) NotifyPropertyChanged(nameof(PrimaryIndex));
            if (secondaryChanged || oldSecondary != newSecondary) NotifyPropertyChanged(nameof(SecondaryIndex));
         } finally { synchronizingBlocksets = false; }
      }

      private static bool OptionsCurrent(ObservableCollection<BlocksetOption> target, ObservableCollection<BlocksetOption> source) {
         if (target.Count != source.Count) return false;
         for (int i = 0; i < target.Count; i++) if (!target[i].IsCurrent(source[i])) return false;
         return true;
      }

      private static bool SynchronizeOptions(ObservableCollection<BlocksetOption> target, ObservableCollection<BlocksetOption> source) {
         if (target.Select(option => option.Address).SequenceEqual(source.Select(option => option.Address))) {
            for (int i = 0; i < target.Count; i++) target[i].UpdateFrom(source[i]);
            return false;
         }
         target.Clear();
         foreach (var option in source) target.Add(option);
         return true;
      }

      /// <summary>The UI calls this after its native selection event has finished.</summary>
      public void CompleteBlocksetSelection() {
         if (blocksetRefreshPending) UpdateFromModel();
      }

      public void Refresh() {
         format.Refresh();
         UpdateFromModel();
         // Data operations also use Refresh to invalidate graphics at unchanged addresses.
         NotifyPropertiesChanged(nameof(PrimaryIndex), nameof(SecondaryIndex));
      }

      public ObservableCollection<BlocksetOption> PrimaryOptions { get; } = new();
      public ObservableCollection<BlocksetOption> SecondaryOptions { get; } = new();
      private int primaryIndex, secondaryIndex;
      public int PrimaryIndex {
         get => primaryIndex;
         set {
            if (synchronizingBlocksets || blocksetSelectionDepth > 0 || primaryIndex == value || !value.InRange(0, PrimaryOptions.Count)) return;
            blocksetSelectionDepth++;
            try {
               primaryIndex = value;
               UpdateBlocksets();
               NotifyPropertyChanged();
            } finally { blocksetSelectionDepth--; }
         }
      }
      public int SecondaryIndex {
         get => secondaryIndex;
         set {
            if (synchronizingBlocksets || blocksetSelectionDepth > 0 || secondaryIndex == value || !value.InRange(0, SecondaryOptions.Count)) return;
            blocksetSelectionDepth++;
            try {
               secondaryIndex = value;
               UpdateBlocksets();
               NotifyPropertyChanged();
            } finally { blocksetSelectionDepth--; }
         }
      }
      private void UpdateBlocksets() {
         var map = new MapModel(this.map);
         if (map.Layout.Element == null) return;
         if (!primaryIndex.InRange(0, PrimaryOptions.Count)) return;
         if (!secondaryIndex.InRange(0, SecondaryOptions.Count)) return;
         var primaryAddress = PrimaryOptions[primaryIndex].Address;
         var secondaryAddress = SecondaryOptions[secondaryIndex].Address;
         map.Layout.Element.SetAddress(Format.PrimaryBlockset, primaryAddress);
         map.Layout.Element.SetAddress(Format.SecondaryBlockset, secondaryAddress);
      }

      // flags.|t|allowBiking.|allowEscaping.|allowRunning.|showMapName.
      public int Music { get => GetValue(); set => SetValue(value); }
      public int LayoutID { get => GetValue(); set => SetValue(value); }
      public int RegionSectionID { get => GetValue(); set => SetValue(value); }
      public int Cave { get => GetValue(); set => SetValue(value); }
      public int Weather { get => GetValue(); set => SetValue(value); }
      public int MapType { get => GetValue(); set => SetValue(value); }
      public bool AllowBiking { get => GetBool(); set => SetBool(value); }
      public bool AllowEscaping { get => GetBool(); set => SetBool(value); }
      public bool AllowRunning { get => GetBool(); set => SetBool(value); }
      public bool ShowMapName { get => GetBool(); set => SetBool(value); }
      public int FloorNum { get => GetValue(); set => SetValue(value); }
      public int BattleType { get => GetValue(); set => SetValue(value); }

      public bool ShowFloorNumField => map.HasField("floorNum");                // FR/LG only
      public bool ShowAllowBikingField => map.HasField("allowBiking") || (map.HasField("flags") && map.GetTuple("flags").HasField("allowBiking"));       // not for R/S

      public bool HasMusicOptions => MusicOptions.Count > 0;
      public ObservableCollection<string> MusicOptions { get; } = new();

      public bool HasMapTypeOptions => MapTypeOptions.Count > 0;
      public ObservableCollection<string> MapTypeOptions { get; } = new();

      private int GetValue([CallerMemberName] string name = null) {
         name = char.ToLower(name[0]) + name.Substring(1);
         if (!map.HasField(name)) return -1;
         return map.GetValue(name);
      }

      // when we call SetValue, get the latest token
      private void SetValue(int value, [CallerMemberName] string name = null) {
         if (value == GetValue(name)) return;
         map = new(map.Model, map.Table.Start, (map.Start - map.Table.Start) / map.Table.ElementCount, tokenFactory, map.Table);
         var originalName = name;
         name = char.ToLower(name[0]) + name.Substring(1);
         map.SetValue(name, value);
         NotifyPropertyChanged(originalName);
      }

      private bool GetBool([CallerMemberName] string name = null) {
         name = char.ToLower(name[0]) + name.Substring(1);
         if (map.HasField(name)) {
            return map.GetValue(name) != 0;
         } else if (map.HasField("flags")) {
            var tuple = map.GetTuple("flags");
            if (!tuple.HasField(name)) return false;
            return tuple.GetValue(name) != 0;
         }

         return false;
      }

      private void SetBool(bool value, [CallerMemberName] string name = null) {
         var originalName = name;
         name = char.ToLower(name[0]) + name.Substring(1);
         if (map.HasField(name)) {
            map.SetValue(name, value ? 1 : 0);
            NotifyPropertyChanged(originalName);
         } else if (map.HasField("flags")) {
            var tuple = map.GetTuple("flags");
            if (tuple.HasField(name)) {
               tuple.SetValue(name, value ? 1 : 0);
               NotifyPropertyChanged(originalName);
            }
         }
      }
   }

   public class BlocksetOption : ViewModelCore { // changing this to a record would interfere with combobox selection changes.
      private Lazy<IPixelViewModel> render;
      public IDataModel Model { get; }
      public int Address { get; }
      public string NameHint { get; private set; }
      public IPixelViewModel Render => render?.Value;
      public string AddressText => Address.ToAddress() + NameHint;

      internal bool IsCurrent(BlocksetOption source) => Address == source.Address && ReferenceEquals(Model, source.Model) &&
         ReferenceEquals(render, source.render) && NameHint == source.NameHint;

      internal void UpdateFrom(BlocksetOption source) {
         if (IsCurrent(source)) return;
         render = source.render;
         if (NameHint != source.NameHint) {
            NameHint = source.NameHint;
            NotifyPropertyChanged(nameof(AddressText));
         }
         NotifyPropertyChanged(nameof(Render));
      }

      public BlocksetOption(IDataModel model, int address) {
         Model = model;
         Address = address;

         var sources = model.GetNextRun(address)?.PointerSources ?? Enumerable.Empty<int>();
         var layoutRuns = sources.Select(source => model.GetNextRun(source)).Distinct();
         var layoutSources = layoutRuns.SelectMany(run => run?.PointerSources ?? Enumerable.Empty<int>());
         var mapRuns = layoutSources.Select(source => model.GetNextRun(source)).Where(run => model.GetAnchorFromAddress(-1, run.Start) != "data.maps.layouts" && run is ITableRun);
         var elements = mapRuns.Select(run => new ModelTable(model, (ITableRun)run)[0]).Where(element => element.HasField("regionSectionID"));
         NameHint = elements.Select(element => element.GetEnumValue("regionSectionID")).Distinct().OrderBy(s => s).FirstOrDefault() ?? string.Empty;
         if (NameHint != string.Empty) NameHint = $" (ex. {NameHint})";

         if (!model.SpartanMode) render = new Lazy<IPixelViewModel>(() => new BlocksetModel(Model, Address).RenderBlockset(.5));
      }
   }
}
