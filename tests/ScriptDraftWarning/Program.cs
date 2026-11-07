using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Review;
Directory.SetCurrentDirectory(AppContext.BaseDirectory);
int checks=0;void Check(bool ok,string message){checks++;if(!ok)throw new Exception(message);}
foreach(bool discard in new[]{false,true}){
 using var f=new WorkflowFixture();var editor=new EditorViewModel(f.FileSystem,InstantDispatch.Instance);editor.Add(f.View);var source=f.Body.Content;var before=f.State();
 f.Edit(source+"\nnot_a_command");f.View.Save.Execute(f.FileSystem);
 Check(editor.ShowError&&f.Tool.HasUncompiledScriptChanges,"Rejected source must still show its persistence warning.");
 Check(before.EqualsModel(f),"Rejected draft and blocked Save preserve model state.");
 if(discard){f.View.Undo.Execute(null);f.Dispatch.Flush();}else f.Edit(source);
 Check(!f.Tool.HasUncompiledScriptChanges&&!editor.ShowError&&string.IsNullOrEmpty(editor.ErrorMessage),"Successful correction or explicit draft discard must clear its stale persistence warning.");
 Check(before.EqualsModel(f),"Correction/discard does not alter the original model.");
}
using(var f=new WorkflowFixture()){
 var editor=new EditorViewModel(f.FileSystem,InstantDispatch.Instance);editor.Add(f.View);var source=f.Body.Content;
 f.Edit(source+"\nnot_a_command");f.View.Save.Execute(f.FileSystem);var warning=editor.ErrorMessage;
 f.Edit(source+"\nanother_bad_command");Check(editor.ErrorMessage==warning&&editor.ShowError,"A remaining rejected draft must retain its warning.");
 f.View.RaiseError("An unrelated error");f.Edit(source);
 Check(editor.ErrorMessage=="An unrelated error"&&editor.ShowError,"Compiling a draft cannot dismiss an unrelated newer error.");
 f.Edit(source+"\nnot_a_command");f.View.Save.Execute(f.FileSystem);editor.ErrorMessage="Directly assigned error";f.Edit(source);
 Check(editor.ErrorMessage=="Directly assigned error","A direct error assignment must relinquish prior warning ownership.");
}
using(var a=new WorkflowFixture())using(var b=new WorkflowFixture()){
 var editor=new EditorViewModel(a.FileSystem,InstantDispatch.Instance);editor.Add(a.View);editor.Add(b.View);var asource=a.Body.Content;var bsource=b.Body.Content;
 a.Edit(asource+"\nnot_a_command");a.View.Save.Execute(a.FileSystem);b.Edit(bsource+"\nnot_a_command");b.View.Save.Execute(b.FileSystem);var warning=editor.ErrorMessage;
 a.Edit(asource);Check(editor.ShowError&&editor.ErrorMessage==warning&&b.Tool.HasUncompiledScriptChanges,"A different ROM's identical-address warning cannot be dismissed by correcting this ROM.");
 b.Edit(bsource);Check(!editor.ShowError,"The warning clears when its owning ROM is corrected.");
}
using(var f=new WorkflowFixture()){
 var editor=new EditorViewModel(f.FileSystem,InstantDispatch.Instance);editor.Add(f.View);var root=f.Body.Content;
 f.Edit(root+"\nnot_a_command");f.View.Goto.Execute(WorkflowFixture.Other);f.Dispatch.Flush();var other=f.Body.Content;
 f.Edit(other+"\nnot_a_command");f.View.Save.Execute(f.FileSystem);Check(f.Tool.HasUncompiledScriptChanges&&editor.ShowError,"Multiple rejected script bodies block persistence.");
 f.Edit(other);Check(f.Tool.HasUncompiledScriptChanges&&editor.ShowError,"Fixing only one draft cannot clear the unresolved warning.");
 f.View.Goto.Execute(WorkflowFixture.Root);f.Dispatch.Flush();f.Edit(root);Check(!f.Tool.HasUncompiledScriptChanges&&!editor.ShowError,"Fixing the last draft clears the persistence warning.");
}
Console.WriteLine($"CHECKS_PASSED:{checks}");
