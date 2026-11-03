using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

AppBuilder.Configure<App>().UseStandardRuntimePlatformSubsystem().UseSkia()
   .UseHeadless(new AvaloniaHeadlessPlatformOptions{UseHeadlessDrawing=false}).WithInterFont().SetupWithoutStarting();
SynchronizationContext.SetSynchronizationContext(new AvaloniaSynchronizationContext());
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
Application.Current!.Resources["FileSystem"]=new StubFileSystem();
int checks=0;
void Check(bool value,string message){checks++;if(!value)throw new InvalidOperationException(message);}
foreach(var table in new[]{false,true}) {
   var singletons=new Singletons();
   var model=new PokemonModel(Enumerable.Repeat((byte)0xFF,1024).ToArray(),singletons:singletons);
   var viewport=new ViewPort("text-fixture.bin",model,InstantDispatch.Instance,singletons){Width=16,Height=32};
   var errors=new List<string>();viewport.OnError+=(_,message)=>errors.Add(message);
   viewport.Edit(table?"@000100 ^names[name\"\"16]2 \"PALLET TOWN\" @000110 \"ROUTE ONE\" ":"@000100 ^name\"\" \"PALLET TOWN\" ");
   viewport.ChangeHistory.ChangeCompleted();viewport.ChangeHistory.TagAsSaved();
   var tool=viewport.Tools.StringTool;tool.Address=0x100;
   viewport.Tools.SelectedIndex=Enumerable.Range(0,viewport.Tools.Count).First(i=>ReferenceEquals(viewport.Tools[i],tool));
   viewport.ChangeHistory.ClearHistory(needsSave:false);viewport.ChangeHistory.TagAsSaved();
   Check(tool.Enabled && tool.Content.StartsWith("PALLET TOWN"),"Fixture must open actual PCS/name-table text in the text tool.");
   var view=new TabView{DataContext=viewport};var window=new Window{Width=1440,Height=1000,Content=view};window.Show();
   void Layout(){Dispatcher.UIThread.RunJobs();view.Measure(new Size(1440,1000));view.Arrange(new Rect(0,0,1440,1000));}
   Layout();var box=view.FindControl<TextBox>("StringToolTextBox")!;
   var original=model.RawData.ToArray();var metadata=model.ExportMetadata(null,singletons.MetadataInfo).Serialize();
   for(int repeat=0;repeat<3;repeat++) {
      box.SelectionStart=7;box.SelectionEnd=2;
      Check(tool.ContentIndex==2 && tool.ContentSelectionLength==5,"Backward selection must reach Core as a sorted index and positive length.");
      box.SelectionStart=1;box.SelectionEnd=9;
      Check(tool.ContentIndex==1 && tool.ContentSelectionLength==8,"Forward selection must preserve the exact range.");
      box.SelectionStart=4;box.SelectionEnd=4;
      Check(tool.ContentIndex==4 && tool.ContentSelectionLength==0,"A collapsed caret must have zero selection length.");
   }
   Check(original.SequenceEqual(model.RawData) && !viewport.Undo.CanExecute(null),"Text selection must not edit ROM bytes or history.");
   Check(metadata.SequenceEqual(model.ExportMetadata(null,singletons.MetadataInfo).Serialize()),"Text selection must preserve metadata.");
   box.Focus();box.SelectionStart=11;box.SelectionEnd=0;
   window.KeyTextInput("NEWNAME");Layout();
   Check(tool.Content.StartsWith("NEWNAME"),"Replacing a reversed selection must rename the actual text.");
   Check(tool.ContentSelectionLength>=0 && tool.ContentIndex>=0 && tool.ContentIndex+tool.ContentSelectionLength<=tool.Content.Length,
      "Caret/selection updates while text shrinks must remain within current content.");
   if(table)Check(tool.Content.Contains("ROUTE ONE"),"Renaming one table name must preserve the next name.");
   var renamed=model.RawData.ToArray();Check(!renamed.SequenceEqual(original),"Typing must actually modify encoded text bytes.");
   viewport.ChangeHistory.ChangeCompleted();viewport.Undo.Execute(null);Layout();
   Check(original.SequenceEqual(model.RawData),"Undo must restore every encoded text byte.");
   Check(metadata.SequenceEqual(model.ExportMetadata(null,singletons.MetadataInfo).Serialize()),"Undo must restore text metadata.");
   viewport.Redo.Execute(null);Layout();Check(renamed.SequenceEqual(model.RawData),"Redo must restore renamed text bytes.");
   box.SelectionStart=0;box.SelectionEnd=0;window.KeyTextInput("X");Layout();
   Check(tool.Content.StartsWith("XNEWNAME"),"Subsequent typing and selection subscriptions must still work after undo/redo.");
   box.Focus();box.CaretIndex=0;
   foreach(var character in "ABC") {
      var caret=box.CaretIndex;window.KeyTextInput(character.ToString());Layout();
      Check(box.IsFocused && box.CaretIndex==caret+1,"Name/text editing must retain focus and advance the caret for every keystroke.");
   }
   Check(errors.Count==0,"Valid text edits must not report errors.");
   window.Close();Dispatcher.UIThread.RunJobs();
}
// Map names use a pointer table, unlike the fixed-width names above.
{
   var singletons=new Singletons();var data=Enumerable.Repeat((byte)0xFF,0x1000).ToArray();
   BitConverter.GetBytes(0x08000200).CopyTo(data,0x100);BitConverter.GetBytes(0x08000240).CopyTo(data,0x104);
   PCSString.Convert("\"PALLET TOWN\"").CopyTo(data,0x200);PCSString.Convert("\"ROUTE ONE\"").CopyTo(data,0x240);
   var model=new PokemonModel(data,new StoredMetadata(anchors:new[]{
      new StoredAnchor(0x100,HardcodeTablesModel.MapNameTable,"[name<\"\">]2"),
   }),singletons);
   var viewport=new ViewPort("map-names-fixture.bin",model,InstantDispatch.Instance,singletons,fs:new StubFileSystem());
   viewport.InitializationWorkload.GetAwaiter().GetResult();viewport.Goto.Execute(0x100);
   var view=new TabView{DataContext=viewport};var window=new Window{Width=1440,Height=1000,Content=view};window.Show();
   void Layout(){Dispatcher.UIThread.RunJobs();window.Measure(new Size(1440,1000));window.Arrange(new Rect(0,0,1440,1000));AvaloniaHeadlessPlatform.ForceRenderTimerTick();}
   TextBox Box()=>view.GetVisualDescendants().OfType<TextBox>().Single(b=>b.Name=="StreamTextBox" && b.DataContext is TextStreamElementViewModel);
   Layout();var box=Box();Check(box.Text=="PALLET TOWN","The actual map-name pointer format must open its name stream.");
   viewport.ChangeHistory.ClearHistory(false);viewport.ChangeHistory.TagAsSaved();
   var before=model.RawData.ToArray();var metadata=model.ExportMetadata(null,singletons.MetadataInfo).Serialize();
   box.Focus();box.SelectAll();
   foreach(var character in "NEW TOWN") {
      window.KeyTextInput(character.ToString());Layout();
      Check(ReferenceEquals(box,Box()) && box.IsFocused,"Map-name stream refresh must retain the same focused editor for every keystroke.");
   }
   Check(box.Text=="NEW TOWN" && model.ReadPointer(0x100)==0x200,"Typing a shorter map name must keep its pointer and final text.");
   var expected=PCSString.Convert("\"NEW TOWN\"");
   Check(model.RawData.Skip(0x200).Take(expected.Count).SequenceEqual(expected),"The map name must have exact encoded bytes.");
   Check(model.ReadPointer(0x104)==0x240 && model.RawData.Skip(0x240).Take(16).SequenceEqual(before.Skip(0x240).Take(16)),"Renaming a map must preserve its neighboring name and pointer.");
   viewport.ChangeHistory.ChangeCompleted();var renamed=model.RawData.ToArray();
   viewport.Undo.Execute(null);Layout();
   Check(before.SequenceEqual(model.RawData) && metadata.SequenceEqual(model.ExportMetadata(null,singletons.MetadataInfo).Serialize()),"Undo restores all map-name bytes and metadata.");
   viewport.Redo.Execute(null);Layout();Check(renamed.SequenceEqual(model.RawData),"Redo restores the renamed map exactly.");
   viewport.Goto.Execute(0x104);Layout();Check(Box().Text=="ROUTE ONE","Map-name navigation must refresh to the other entry.");
   viewport.Goto.Execute(0x100);Layout();Check(Box().Text=="NEW TOWN","Returning to the edited name must refresh to its saved model content.");
   window.Close();Dispatcher.UIThread.RunJobs();
}
Console.WriteLine("PASS: PCS/fixed names and map-name pointer streams, continuous focus/caret, selections, exact bytes/metadata, navigation and undo/redo.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
