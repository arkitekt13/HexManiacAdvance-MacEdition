using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Threading;
using HavenSoft.HexManiac.AvaloniaUI;
using HavenSoft.HexManiac.AvaloniaUI.Controls;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;

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
   Check(errors.Count==0,"Valid text edits must not report errors.");
   window.Close();Dispatcher.UIThread.RunJobs();
}
Console.WriteLine("PASS: real PCS and fixed-name table text, reversed/forward selections, collapsed caret, text replacement/shrink, subsequent typing, immutable selection, metadata, and undo/redo.");
Console.WriteLine($"CHECKS_PASSED:{checks}");
