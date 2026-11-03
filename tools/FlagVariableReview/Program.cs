using System.Text;
using System.Text.Json;
using HavenSoft.HexManiac.Core.Models;
using HavenSoft.HexManiac.Core.Models.Code;
using HavenSoft.HexManiac.Core.Models.Runs;
using HavenSoft.HexManiac.Core.ViewModels;
using HavenSoft.HexManiac.Core.ViewModels.DataFormats;
using HavenSoft.HexManiac.Core.ViewModels.Images;
using HavenSoft.HexManiac.Core.ViewModels.Map;
using HavenSoft.HexManiac.Performance;

// Characterization only: report current helper behavior, without making shortcomings
// required behavior in the normal regression suite. No supplied ROM or native UI.
if(args.Length!=1)throw new ArgumentException("Expected a JSON report path.");
var output=Path.GetFullPath(args[0]);Directory.SetCurrentDirectory(AppContext.BaseDirectory);
const int code=0x12000;
var results=new Dictionary<string,object>();
(PokemonModel Model,ScriptParser Parser) Fixture(byte[] script) {
   var m=new SyntheticMapFixture(8,8).Model;var token=new ModelDelta();
   token.ChangeData(m,0xAC,Encoding.ASCII.GetBytes("BPRE"));m.WriteValue(token,0xBC,0);
   m.LoadMetadata(BaseModel.GetDefaultMetadatas().First());
   m.WritePointer(token,0x128,0x200);m.WritePointer(token,0x200,0x300);m.WritePointer(token,0x204,0x400);
   m.WritePointer(token,0x300,code);m.WriteMultiByteValue(0x304,2,token,0);
   m.WriteMultiByteValue(0x400,2,token,0);m.WritePointer(token,0x402,code);
   const string events="[objects<[script<> flag:]1> scripts<[trigger: script<>]1> signposts<[arg<>]0>]1";
   var format=m.GetNextRun(0x100).FormatString.Replace("events<>","events<"+events+">")
      .Replace("mapscripts<>","mapscripts<[type. pointer<>]!00>");
   var error=ArrayRun.TryParse(m,format,0x100,SortedSpan<int>.None,out var root);
   if(error.HasError)throw new InvalidOperationException(error.ErrorMessage);
   m.ObserveAnchorWritten(new NoDataChangeDeltaModel(),"data.maps.banks",root);
   token.ChangeData(m,code,script);
   if(!m.IsFRLG())throw new InvalidOperationException("Review fixture must be recognized as FRLG.");
   return(m,new ScriptParser(m.GetShortGameCode(),new Singletons().ScriptLines,2));
}
EventTemplate Template(PokemonModel m,ScriptParser parser)=>new(InstantDispatch.Instance,m,parser,
   new[]{new ReadonlyPixelViewModel(16,16,new short[256])});
var (sourceModel,sourceParser)=Fixture(new byte[]{0x19,0x36,0x40,0x34,0x40,2});
var sourceVariables=Flags.GetUsedVariables(sourceModel,sourceParser);
if(!sourceVariables.Contains(0x4036))throw new InvalidOperationException("The connected script's destination variable must be detected.");
var sourceTemplate=Template(sourceModel,sourceParser);
results["copyvarSourceOnly"]=new{script="copyvar 0x4036 0x4034; end",destinationDetected=sourceVariables.Contains(0x4036),
   sourceDetected=sourceVariables.Contains(0x4034),suggestedVariable=sourceTemplate.FindNextUnusedVariable()};

var (staleModel,staleParser)=Fixture(new byte[]{0x29,0x21,0,2});var stale=Template(staleModel,staleParser);
var first=stale.FindNextUnusedFlag();staleModel.WriteMultiByteValue(code+1,2,new ModelDelta(),0x23);
results["manualEditAfterInitialization"]=new{initialFlag=0x21,firstSuggestedFlag=first,
   manuallyChangedTo=0x23,freshScanDetectsChange=Flags.GetUsedItemFlags(staleModel,staleParser).Contains(0x23),
   nextSuggestedFlag=stale.FindNextUnusedFlag()};

var (headerModel,headerParser)=Fixture(new byte[]{2});var setup=new ModelDelta();
headerModel.WritePointer(setup,0x300,Pointer.NULL);headerModel.WritePointer(setup,0x402,Pointer.NULL);
headerModel.WritePointer(setup,0x12C,0x600);headerModel.WriteValue(setup,0x600,2);headerModel.WritePointer(setup,0x601,0x650);
headerModel.WriteMultiByteValue(0x650,2,setup,0x4034);headerModel.WriteMultiByteValue(0x652,2,setup,1);
headerModel.WritePointer(setup,0x654,code);
var headerRoots=Flags.GetAllTopLevelScripts(headerModel);
if(!headerRoots.Contains(code))throw new InvalidOperationException("The detached header script must be found through the header table alone.");
results["headerConditionVariable"]=new{conditionVariable=0x4034,headerScriptFound=headerRoots.Contains(code),
   conditionVariableDetected=Flags.GetUsedVariables(headerModel,headerParser).Contains(0x4034)};

var varScript=new List<byte>();for(int variable=0x4034;variable<=0x40FF;variable++)varScript.AddRange(new byte[]{0x16,(byte)variable,(byte)(variable>>8),0,0});varScript.Add(2);
var (fullVars,fullVarParser)=Fixture(varScript.ToArray());
results["persistentVariableRangeExhausted"]=new{lastNormalVariable=0x40FF,suggestedVariable=Template(fullVars,fullVarParser).FindNextUnusedVariable()};
var flagScript=new List<byte>();for(int flag=0x21;flag<0x500;flag++)flagScript.AddRange(new byte[]{0x29,(byte)flag,(byte)(flag>>8)});flagScript.Add(2);
var (fullFlags,fullFlagParser)=Fixture(flagScript.ToArray());
results["ordinaryFlagRangeExhausted"]=new{firstTrainerFlag=0x500,suggestedFlag=Template(fullFlags,fullFlagParser).FindNextUnusedFlag()};

var (reserveModel,reserveParser)=Fixture(new byte[]{2});var reserve=Template(reserveModel,reserveParser);var before=reserveModel.RawData.ToArray();
var flag1=reserve.FindNextUnusedFlag();var flag2=reserve.FindNextUnusedFlag();
results["sessionReservations"]=new{first=flag1,second=flag2,romUnchanged=reserveModel.RawData.SequenceEqual(before),
   standaloneFreshScanSeesReservation=Flags.GetUsedItemFlags(reserveModel,reserveParser).Contains(flag1)};
Directory.CreateDirectory(Path.GetDirectoryName(output)!);
File.WriteAllText(output,JsonSerializer.Serialize(new{scope="Current production scanners/helpers on fabricated map models; no application helper changes or supplied ROMs.",results},new JsonSerializerOptions{WriteIndented=true}));
Console.WriteLine(JsonSerializer.Serialize(results,new JsonSerializerOptions{WriteIndented=true}));
