using System.Globalization;
using System.Text;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.Tools;

// Freeze existing matching and edit behavior before changing argument lookup.
// All bytes/addresses are fabricated. No supplied ROM, native UI, or timing threshold.
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
var originalCulture=CultureInfo.CurrentCulture;
CultureInfo.CurrentCulture=CultureInfo.InvariantCulture;
var checks=0;
void Check(bool condition,string message){checks++;if(!condition)throw new InvalidOperationException(message);}
var singletons=new Singletons();
PokemonModel Model() {
   var data=Enumerable.Repeat((byte)0xFF,0x10000).ToArray();
   Encoding.ASCII.GetBytes("BPRE").CopyTo(data,0xAC);data[0xBC]=0;
   var model=new PokemonModel(data,singletons:singletons);
   model.LoadMetadata(BaseModel.GetDefaultMetadatas().First());return model;
}
try {
   var model=Model();
   void Match(string input,string[] options,int expected) {
      var copy=options.ToArray();
      Check(ArrayRunEnumSegment.TryMatch(input,options,out var actual),$"Expected a match for {input}.");
      Check(actual==expected,$"{input}: expected {expected}, got {actual}.");
      Check(options.SequenceEqual(copy),"Matching must not mutate its source options.");
   }
   Match("Potion",new[]{"Super Potion","Potion","Potion~2"},1);
   Match("POTION~2",new[]{"Super Potion","Potion","Potion~2"},2);
   Match("\"Mixed Name\"",new[]{"Other","\"Mixed Name\""},1);
   Match("  mixed NAME  ",new[]{"Other","Mixed Name"},1);
   Match("spotion",new[]{"Other","Super Potion"},1);
   Match("Potion~2",new[]{"Potion","Potion","Potion"},1);
   Match("1",new[]{"1","Else"},1); // unquoted decimal has numeric priority
   Match("\"1\"",new[]{"1","Else"},0); // quoted decimal matches the option instead
   Match("2",new string[]{"Zero",null,null},2);
   Match("\"2\"",new string[]{"Zero",null,null},2); // null slots retain implicit numeric names
   Match("0x4001",new[]{"Other","0x4001"},1);
   Match("0xCAFE",new[]{"Other","0xCAFE token"},1); // partial names precede hex fallback
   Match("$2A",new[]{"Other","$2A"},1);
   foreach(var input in new[]{""," ","NoSuchAlias","Potion~invalid"})
      Check(!ArrayRunEnumSegment.TryMatch(input,new[]{"Potion"},out _),$"{input}: unmatched/malformed input must fail.");
   var sparse=new string[0x8001];sparse[0x4001]="temp1";sparse[0x8000]="result";
   Match("temp1",sparse,0x4001);Match("RESULT",sparse,0x8000);
   Match("16385",sparse,0x4001);Match("\"16384\"",sparse,0x4000);
   Check(!ArrayRunEnumSegment.TryMatch("\"16385\"",sparse,out _),"A named slot must not acquire an implicit quoted numeric name.");

   byte[] Build(PokemonModel source,string declaration,string input,bool succeeds=true) {
      var before=source.RawData.ToArray();var bytes=new List<byte>();
      var error=new ScriptArg(declaration).Build(source,0x500,input,bytes,new LabelLibrary(source,new Dictionary<string,int>()));
      Check((error==null)==succeeds,$"{declaration}/{input}: unexpected parse outcome: {error}");
      Check(source.RawData.SequenceEqual(before),"Building a numeric argument must not write model bytes.");
      if(!succeeds)Check(bytes.Count==0,"Failed argument builds must not emit partial bytes.");
      return bytes.ToArray();
   }
   void Value(PokemonModel source,string declaration,string input,int expected,int width) {
      var actual=Build(source,declaration,input);
      var bytes=Enumerable.Range(0,width).Select(i=>(byte)(expected>>(8*i)));
      Check(actual.SequenceEqual(bytes),$"{declaration}/{input}: wrong value or little-endian width.");
   }
   foreach(var declaration in new[]{"value.","value:","value::"}) {
      int width=declaration.EndsWith("::")?4:declaration.EndsWith(":")?2:1;
      foreach(var input in new[]{"42","0x2A","0X2A","$2A"})Value(model,declaration,input,42,width);
      Value(model,declaration,"-1",-1,width);
      Build(model,declaration,"NoSuchAlias",false);Build(model,declaration,"0x",false);
   }
   Value(model,"value.","0xFF",255,1);Value(model,"value:","0xFFFF",65535,2);
   Value(model,"value::","0xFFFFFFFF",-1,4);
   model.SetList(new NoDataChangeDeltaModel(),"SafetyAliases", "Other","0x4001","0xCAFE token","$2A");
   Value(model,"value:SafetyAliases","0x4001",1,2);
   Value(model,"value:SafetyAliases","0xCAFE",2,2);
   Value(model,"value:SafetyAliases","$2A",3,2);
   Value(model,"value:SafetyAliases","0x1234",0x1234,2);
   Value(model,"value:SafetyAliases+3","0x4001",4,2);
   Value(model,"value:SafetyAliases-1","0x4001",0,2);
   Value(model,"value:SafetyAliases+3","42",45,2);
   Value(model,"value:SafetyAliases-1","$40",63,2);
   Value(model,"value:|h","0x4001",0x4001,2);
   Value(model,"value:scriptvariablealiases","temp1",0x4001,2);
   Value(model,"value:scriptvariablealiases","0x4001",0x4001,2);
   Value(model,"value:scriptvariablealiases","16385",0x4001,2);

   // SetList is a low-level metadata operation; normal Refresh clears the model cache.
   // Future lookup preparation must follow that boundary and never leak across models.
   model.SetList(new NoDataChangeDeltaModel(),"MutableAliases","Other","Target");
   Value(model,"value:MutableAliases","Target",1,2);
   model.SetList(new NoDataChangeDeltaModel(),"MutableAliases","Target","Other","Added");
   model.ClearCacheScope();
   Value(model,"value:MutableAliases","Target",0,2);Value(model,"value:MutableAliases","Added",2,2);
   model.SetList(new NoDataChangeDeltaModel(),"MutableAliases","Only");
   model.ClearCacheScope();
   Build(model,"value:MutableAliases","Target",false);
   var other=Model();other.SetList(new NoDataChangeDeltaModel(),"MutableAliases","Unused","Only");
   Value(other,"value:MutableAliases","Only",1,2);Value(model,"value:MutableAliases","Only",0,2);

   // Run each spelling through actual script relocation with two callers and embedded text.
   foreach(var variable in new[]{"0x4001","16385","temp1"}) {
      var m=Model();var viewport=new ViewPort("script-safety.bin",m,InstantDispatch.Instance,singletons);
      viewport.InitializationWorkload.GetAwaiter().GetResult();
      var tool=viewport.Tools.CodeTool;var parser=tool.ScriptParser;var setup=new ModelDelta();
      const int root=0x1000,text=0x3000;
      string Script(int count,int value)=>string.Concat(Enumerable.Repeat($"setvar {variable} {value}\n",count))+
         "loadpointer 0 <003000>\n{\nHello!\n}\nrelease\nend";
      var source=Script(1,1);var seed=parser.Compile(setup,m,root,ref source,out _,out _);
      Check(seed!=null,"Initial script must compile.");setup.ChangeData(m,root,seed);
      parser.FormatScript<XSERun>(setup,m,root,seed.Length);
      m.ObserveAnchorWritten(setup,"safety.root",new XSERun(root));
      var textRun=(PCSRun)m.GetNextRun(text);
      m.ObserveAnchorWritten(setup,"safety.dialogue",textRun);
      foreach(var caller in new[]{0x100,0x200}) {
         setup.ChangeData(m,caller,new byte[]{4,0,0,0,8,2});m.WritePointer(setup,caller+1,root);
         parser.FormatScript<XSERun>(setup,m,caller);
      }
      var neighbor=root+seed.Length;var neighborBytes=new byte[]{0xDE,0xAD,0xBE,0xEF,0x91,0x82,0x73,0x64};
      setup.ChangeData(m,neighbor,neighborBytes);m.ObserveAnchorWritten(setup,"safety.neighbor",new NoInfoRun(neighbor));
      setup.ChangeData(m,root-8,neighborBytes);
      viewport.Goto.Execute(root);tool.Mode=CodeMode.Script;tool.IsSelected=true;
      viewport.ChangeHistory.ClearHistory(false);
      Check(!viewport.Undo.CanExecute(null),"Fixture setup must not become an undoable user edit.");
      // FindFreeSpace updates this navigation/search preference outside ModelDelta.
      // Undo restores ROM/format metadata, not the last free-space search cursor.
      string[] Metadata()=>m.ExportMetadata(null,singletons.MetadataInfo).Serialize()
         .Where(line=>!line.StartsWith("FreeSpaceSearch = ",StringComparison.Ordinal)).ToArray();
      var baseline=m.RawData.ToArray();var baselineMetadata=Metadata();
      var dialogue=m.RawData.Skip(text).Take(textRun.Length).ToArray();
      void Integrity(int destination,int statements,int value) {
         Check(!tool.ShowErrorText && tool.Contents.All(body=>!body.HasError),"Valid script edits must compile without errors.");
         foreach(var caller in new[]{0x100,0x200}) {
            Check(m.ReadPointer(caller+1)==destination,"Every caller must track the script after relocation.");
            Check(m.GetNextRun(destination).PointerSources.Contains(caller+1),"The script must retain its caller source metadata.");
         }
         for(int i=0;i<statements;i++)Check(m.RawData.Skip(destination+i*5).Take(5).SequenceEqual(new byte[]{0x16,1,0x40,(byte)value,0}),"Every setvar must preserve its variable/value and opcode.");
         Check(m.ReadPointer(destination+statements*5+2)==text,"The embedded text pointer must remain correct.");
         Check(m.RawData.Skip(text).Take(dialogue.Length).SequenceEqual(dialogue),"Script edits must preserve dialogue bytes.");
         Check(m.GetNextRun(text) is PCSRun,"Dialogue must retain its PCS format.");
         Check(m.RawData.Skip(neighbor).Take(8).SequenceEqual(neighborBytes) && m.RawData.Skip(root-8).Take(8).SequenceEqual(neighborBytes),"Script edits must preserve neighboring bytes.");
         Check(m.GetAddressFromAnchor(new NoDataChangeDeltaModel(),-1,"safety.root")==destination,"The root anchor must track relocation.");
         Check(m.GetAddressFromAnchor(new NoDataChangeDeltaModel(),-1,"safety.neighbor")==neighbor,"The neighboring anchor must stay fixed.");
      }
      tool.Contents[0].Content=Script(24,2);viewport.ChangeHistory.ChangeCompleted();
      var relocated=tool.Contents[0].Address;
      Check(relocated!=root,"Blocked script growth must actually exercise relocation.");Integrity(relocated,24,2);
      var expanded=m.RawData.ToArray();var expandedMetadata=Metadata();
      tool.Contents[0].Content=Script(1,3);viewport.ChangeHistory.ChangeCompleted();
      Check(tool.Contents[0].Address==relocated,"Shrinking must preserve the relocated script address.");Integrity(relocated,1,3);
      var shrunk=m.RawData.ToArray();var shrunkMetadata=Metadata();
      void State(byte[] bytes,string[] metadata,string stage) {
         Check(m.RawData.SequenceEqual(bytes),$"{stage}: undo/redo must restore every byte; differences: "+string.Join(", ",Enumerable.Range(0,bytes.Length).Where(i=>m[i]!=bytes[i]).Take(12).Select(i=>$"{i:X6}:{m[i]:X2}!={bytes[i]:X2}")));
         var actualMetadata=Metadata();
         Check(actualMetadata.SequenceEqual(metadata),$"{stage}: undo/redo must restore pointer and anchor metadata; removed: "+string.Join(";",metadata.Except(actualMetadata))+"; added: "+string.Join(";",actualMetadata.Except(metadata)));
      }
      // Relocation already uses several history records around InsertCustomChange.
      // Verify the complete history round-trip, including every intermediate record,
      // rather than imposing a new one-edit/one-undo contract on the existing compiler.
      var states=new List<(byte[] Bytes,string[] Metadata)>{(shrunk,shrunkMetadata)};
      viewport.Undo.Execute(null);State(expanded,expandedMetadata,"Undo shrink");Integrity(relocated,24,2);
      states.Add((m.RawData.ToArray(),Metadata()));
      while(viewport.Undo.CanExecute(null)) {
         Check(states.Count<16,"Undo history must terminate within the fabricated edit sequence.");
         viewport.Undo.Execute(null);
         states.Add((m.RawData.ToArray(),Metadata()));
      }
      State(baseline,baselineMetadata,"Undo complete expansion");Integrity(root,1,1);
      for(int i=states.Count-2;i>=0;i--) {
         Check(viewport.Redo.CanExecute(null),"Every undone record must remain redoable.");
         viewport.Redo.Execute(null);State(states[i].Bytes,states[i].Metadata,$"Redo record {i}");
      }
      State(shrunk,shrunkMetadata,"Complete redo");Integrity(relocated,1,3);
      Check(!viewport.Redo.CanExecute(null),"Redo must consume all recorded changes without extra work.");
      tool.Contents[0].Content="setvar NoSuchVariableAlias 1\nend";
      Check(tool.ShowErrorText || tool.Contents.Any(body=>body.HasError),"Invalid variable input must report a compile error.");
      State(shrunk,shrunkMetadata,"Rejected invalid variable");
      tool.Contents[0].Content=Script(1,3);
      State(shrunk,shrunkMetadata,"Corrected invalid variable");Integrity(relocated,1,3);
      Console.WriteLine($"PASS: {variable} growth/relocation/shrink, callers, dialogue, neighbors, exact undo/redo ({states.Count-1} history records).");
   }
   Console.WriteLine("PASS: existing argument precedence, numeric forms/widths, offsets, sparse aliases, list replacement/model isolation, and script mutation safety.");
   Console.WriteLine($"CHECKS_PASSED:{checks}");
} finally { CultureInfo.CurrentCulture=originalCulture; }
