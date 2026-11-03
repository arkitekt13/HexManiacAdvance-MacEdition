using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Tools;
using HavenSoft.HexManiac.Performance;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false }).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
Application.Current!.Resources["FileSystem"] = new StubFileSystem();
var checks = 0;
void Check(bool condition, string message) { checks++; if (!condition) throw new InvalidOperationException(message); }
void Layout(TabView view) {
   Dispatcher.UIThread.RunJobs(); view.Measure(new Size(1440, 1000)); view.Arrange(new Rect(0, 0, 1440, 1000));
}
Control[] Controls(TabView view) => view.GetVisualDescendants().OfType<Control>().ToArray();
ItemsControl Previews(TabView view, TrainerPokemonTeamElementViewModel team) => Controls(view).OfType<ItemsControl>()
   .Single(control => ReferenceEquals(control.DataContext, team) && control.ItemsSource is IEnumerable<IPixelViewModel>);

foreach (var (type, members) in new[] { (0, 1), (1, 2), (2, 3), (3, 6) }) {
   var fixture = new SyntheticTrainerFixture(members, (byte)type, true, true);
   fixture.Navigate(0);
   var originalBytes = fixture.Model.RawData.ToArray();
   var metadata = fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize();
   var view = new TabView { DataContext = fixture.ViewPort };
   var window = new Window { Width = 1440, Height = 1000, Content = view };
   window.Show(); Layout(view);
   using var bitmap = new RenderTargetBitmap(new PixelSize(1440, 1000)); bitmap.Render(view);
   for (int row = 0; row < 3; row++) {
      fixture.Navigate(row); Layout(view);
      var team = fixture.TeamEditor; var controls = Controls(view);
      var editor = controls.OfType<TextEditor>().SingleOrDefault(control => ReferenceEquals(control.DataContext, team.ElementContent));
      Check(editor != null, "Trainer-specific TextEditor must win over the generic base stream template.");
      Check(editor!.IsEffectivelyVisible && editor.EditBox.Text == team.Content, "Trainer editor must be visible with exact bound text.");
      Check(!controls.OfType<TextBox>().Any(control => control.Name == "StreamTextBox" && ReferenceEquals(control.DataContext, team)), "Generic stream text box must not shadow the trainer editor.");
      foreach (var label in new[] { "Default Moves", "Default Items" })
         Check(controls.OfType<AngleButton>().Any(button => ReferenceEquals(button.DataContext, team) && Equals(button.Content, label)
            && button.Command?.CanExecute(null) == true && button.IsEffectivelyVisible), "Trainer default-command button must be visible, enabled, and bound to the current team.");
      var previews = ((IEnumerable<IPixelViewModel>)Previews(view, team).ItemsSource).ToArray();
      Check(previews.Length == members, "Bound preview count must match team size.");
      var images = controls.OfType<PixelImage>().Where(control => previews.Any(preview => ReferenceEquals(control.DataContext, preview))).ToArray();
      Check(images.Length == members && images.All(image => image.IsEffectivelyVisible && image.Source is WriteableBitmap source && source.PixelSize == new PixelSize(64, 64)), "Every team preview must publish its real 64x64 bitmap.");
   }
   Check(originalBytes.SequenceEqual(fixture.Model.RawData), "Rendering/navigating trainer templates must preserve all bytes.");
   Check(metadata.SequenceEqual(fixture.Model.ExportMetadata(null, fixture.ViewPort.Singletons.MetadataInfo).Serialize()), "Trainer presentation must preserve metadata.");
   Check(!fixture.ViewPort.Undo.CanExecute(null) && fixture.Errors.Count == 0, "Trainer presentation must create no history/errors.");

   if (type == 1 || type == 2) {
      fixture.Navigate(0); Layout(view);
      var label = type == 1 ? "Default Moves" : "Default Items";
      var button = Controls(view).OfType<AngleButton>().Single(control => ReferenceEquals(control.DataContext, fixture.TeamEditor) && Equals(control.Content, label));
      button.Command.Execute(null); Layout(view); fixture.ViewPort.ChangeHistory.ChangeCompleted();
      var run = fixture.Team(0);
      // This fixture has no learnsets/base stats: existing default-command behavior uses zero.
      Check(run.ElementCount == members && run.StructType == type, "Default button must target the selected trainer and retain team count/format.");
      Check(Enumerable.Range(0, members).All(member => Enumerable.Range(0, type == 1 ? 4 : 1).All(field =>
         fixture.Model.ReadMultiByteValue(run.Start + member * run.ElementLength + 6 + field * 2, 2) == 0)), "Visible default button must invoke the existing zero-data fallback.");
      fixture.ViewPort.Undo.Execute(null); Layout(view);
      Check(originalBytes.SequenceEqual(fixture.Model.RawData), "Undo after a visible default button must restore exact data.");
   }

   if (type == 1) {
      fixture.Navigate(0); Layout(view);
      var team = fixture.TeamEditor;
      var editor = Controls(view).OfType<TextEditor>().Single(control => ReferenceEquals(control.DataContext, team.ElementContent));
      var originalPointer = fixture.Team(0).Start;
      var otherPointers = Enumerable.Range(1, SyntheticTrainerFixture.Rows - 1).Select(row => fixture.Model.ReadPointer(fixture.Parent(row) + 36)).ToArray();
      var text = editor.EditBox.Text;
      var blocks = text.Split(Environment.NewLine + Environment.NewLine, StringSplitOptions.RemoveEmptyEntries).Select(block => block.Trim()).Where(block => block.Length > 0);
      editor.EditBox.Text = string.Join(Environment.NewLine + Environment.NewLine, blocks.Concat(new[] { blocks.First() })) + Environment.NewLine;
      Layout(view); fixture.ViewPort.ChangeHistory.ChangeCompleted();
      Check(fixture.Team(0).ElementCount == 3 && fixture.Team(0).Start != originalPointer, "Editing the real trainer TextBox must grow and repoint the team.");
      Check(((IEnumerable<IPixelViewModel>)Previews(view, fixture.TeamEditor).ItemsSource).Count() == 3, "Preview binding must follow team growth.");
      Check(otherPointers.SequenceEqual(Enumerable.Range(1, SyntheticTrainerFixture.Rows - 1).Select(row => fixture.Model.ReadPointer(fixture.Parent(row) + 36))), "UI editing must preserve unrelated trainer pointers.");
      var moved = fixture.Team(0).Start;
      fixture.ViewPort.Undo.Execute(null); Layout(view);
      Check(originalBytes.SequenceEqual(fixture.Model.RawData) && fixture.Team(0).Start == originalPointer, "UI edit undo must restore exact bytes and original pointer.");
      fixture.ViewPort.Redo.Execute(null); Layout(view);
      Check(fixture.Team(0).ElementCount == 3 && fixture.Team(0).Start == moved, "UI edit redo must restore the grown team and pointer.");
      fixture.ViewPort.Undo.Execute(null); Layout(view);
      Check(originalBytes.SequenceEqual(fixture.Model.RawData) && fixture.Errors.Count == 0, "Final UI edit undo must leave exact original data without errors.");
   }
   window.Close(); Dispatcher.UIThread.RunJobs();
}

// A real pointer table exercises sprite/palette templates plus the generic text fallback.
var data = new byte[0x3000]; Array.Fill(data, (byte)0xFF);
for (int row = 0; row < 2; row++) {
   void Pointer(int offset, int target) => BitConverter.GetBytes(0x08000000 + target).CopyTo(data, 0x100 + row * 12 + offset);
   Pointer(0, 0x800 + row * 128); Pointer(4, 0x600 + row * 32); Pointer(8, 0x1000 + row * 32);
   Array.Fill(data, (byte)((row + 1) * 17), 0x800 + row * 128, 128);
   for (int color = 0; color < 16; color++) BitConverter.GetBytes((short)(color * (row + 1))).CopyTo(data, 0x600 + row * 32 + color * 2);
   PCSString.Convert($"\"TEXT{row}\"").CopyTo(data, 0x1000 + row * 32);
}
var singletons = new Singletons();
var model = new PokemonModel(data, new StoredMetadata(anchors: new[] {
   new StoredAnchor(0x100, "assets.pointers", "[sprite<`ucs4x2x2|assets.palette`> palette<`ucp4`> text<\"\">]2"),
   new StoredAnchor(0x600, "assets.palette", "`ucp4`"),
}), singletons);
var viewport = new ViewPort("stream-templates", model, InstantDispatch.Instance, singletons, fs: new StubFileSystem());
var errors = new List<string>(); viewport.OnError += (_, message) => errors.Add(message);
viewport.InitializationWorkload.GetAwaiter().GetResult(); viewport.Goto.Execute(0x100);
var sourceBytes = model.RawData.ToArray();
var assetMetadata = model.ExportMetadata(null, singletons.MetadataInfo).Serialize();
var assetView = new TabView { DataContext = viewport };
var assetWindow = new Window { Width = 1440, Height = 1000, Content = assetView }; assetWindow.Show(); Layout(assetView);
for (int row = 0; row < 2; row++) {
   viewport.Goto.Execute(0x100 + row * 12); Layout(assetView);
   var fields = viewport.Tools.TableTool.Groups.SelectMany(group => group.Members).ToArray();
   var sprite = fields.OfType<SpriteElementViewModel>().Single();
   var palette = fields.OfType<PaletteElementViewModel>().Single();
   var text = fields.OfType<TextStreamElementViewModel>().Single();
   var controls = Controls(assetView);
   Check(controls.OfType<PixelImage>().Any(control => ReferenceEquals(control.DataContext, sprite) && control.IsEffectivelyVisible && control.Source is WriteableBitmap bitmap && bitmap.PixelSize == new PixelSize(16, 16)), "Sprite-specific template must display the real sprite bitmap.");
   Check(controls.OfType<AngleButton>().Any(button => ReferenceEquals(button.Command, sprite.OpenEditor) && button.IsEffectivelyVisible), "Sprite Edit command must be visible and bound.");
   Check(controls.OfType<PaletteControl>().Any(control => ReferenceEquals(control.DataContext, palette.Colors) && control.IsEffectivelyVisible), "Palette-specific template must display its bound color control.");
   Check(palette.Colors.Elements.Count == 16 && palette.Colors.SourcePalettePointer == 0x104 + row * 12, "Palette binding must follow the selected pointer row.");
   Check(!controls.OfType<TextBox>().Any(control => control.Name == "StreamTextBox" && (ReferenceEquals(control.DataContext, sprite) || ReferenceEquals(control.DataContext, palette))), "Generic template must not shadow sprite/palette controls.");
   var generic = controls.OfType<TextBox>().Single(control => control.Name == "StreamTextBox" && ReferenceEquals(control.DataContext, text));
   Check(generic.IsEffectivelyVisible && generic.Text.Contains($"TEXT{row}"), "Ordinary text streams must retain the generic text template.");
}
Check(sourceBytes.SequenceEqual(model.RawData), "Asset presentation must preserve exact bytes.");
Check(assetMetadata.SequenceEqual(model.ExportMetadata(null, singletons.MetadataInfo).Serialize()), "Asset presentation must preserve metadata.");
Check(!viewport.Undo.CanExecute(null) && errors.Count == 0, "Asset presentation must create no history/errors.");
assetWindow.Close(); Dispatcher.UIThread.RunJobs();
Console.WriteLine("PASS: production trainer/sprite/palette templates, ordinary text fallback, navigation, and actual trainer UI edit/repoint/undo/redo.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
