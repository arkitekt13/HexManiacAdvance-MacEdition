using HavenSoft.HexManiac.Core.Models;
using HexManiac.Core.Models.Runs.Sprites;

static class RenderBounds {
   public static void Run(Action<bool,string> check,Singletons singletons) {
      // Recognizable game codes choose the right layout without importing real-ROM defaults.
      singletons=new Singletons(singletons.MetadataInfo,new Dictionary<string,GameReferenceTables>());
      foreach(bool firered in new[]{false,true}) foreach(bool secondary in new[]{false,true}) {
         int primary=firered?640:512;
         int limit=secondary?1024-primary:primary;
         foreach(int count in new[]{2,9,limit}) foreach(bool formatted in new[]{false,true}) {
            var data=new byte[0x30000];
            System.Text.Encoding.ASCII.GetBytes(firered?"BPRE":"BPEE").CopyTo(data,0xAC);
            const int header=0x100,tiles=0x1000,palette=0x9000,blocks=0x10000;
            int attributes=blocks+count*16;
            data[header+1]=(byte)(secondary?1:0);
            void Pointer(int address,int target)=>BitConverter.GetBytes(0x08000000+target).CopyTo(data,address);
            Pointer(header+4,tiles);Pointer(header+8,palette);Pointer(header+12,blocks);
            Pointer(header+(firered?20:16),attributes);
            Array.Fill(data,(byte)0x11,tiles,32);Array.Fill(data,(byte)0x22,tiles+32,32);
            BitConverter.GetBytes((short)0x001F).CopyTo(data,palette+2);
            BitConverter.GetBytes((short)0x03E0).CopyTo(data,palette+4);
            for(int b=0;b<count;b++) for(int t=0;t<8;t++)
               BitConverter.GetBytes((ushort)((secondary?primary:0)+(b%2))).CopyTo(data,blocks+b*16+t*2);
            var model=new PokemonModel(data,new StoredMetadata(anchors:new[]{
               new StoredAnchor(blocks,"preview.blocks",formatted?(count==9?$"[tile:]{count*8}":$"[a: b: c: d: e: f: g: h:]{count}"):""),
               new StoredAnchor(attributes,"preview.attributes",$"[value{(firered?"::":":") }]{count}"),
            }),singletons);
            var before=model.RawData.ToArray();var metadata=model.ExportMetadata(null,singletons.MetadataInfo).Serialize();
            var blockset=new BlocksetModel(model,header);
            check(formatted?model.GetNextRun(blocks).Length==count*16:blockset.ReadBlocks(-1).Length==count,$"Fixture must estimate {count} blocks; got {blockset.ReadBlocks(-1).Length}.");
            var image=blockset.RenderBlockset(.5);
            check(image.PixelWidth==128&&image.PixelHeight==((count+7)/8)*16,
               $"Preview must render only {count} valid blocks (FRLG={firered}, secondary={secondary}); got {image.PixelWidth}x{image.PixelHeight}.");
            check(image.SpriteScale==.5&&image.PixelData.Length==image.PixelWidth*image.PixelHeight,"Preview scale and storage must retain existing semantics.");
            bool exact=true;string mismatch="";
            for(int y=0;y<image.PixelHeight;y++) for(int x=0;x<image.PixelWidth;x++) {
               int b=(y/16)*8+x/16;
               short expected=b>=count?(short)0:(b%2==0?(short)0x7C00:(short)0x03E0);
               if(image.PixelData[y*image.PixelWidth+x]!=expected){exact=false;if(mismatch=="")mismatch=$"({x},{y}) expected={expected:X4} actual={image.PixelData[y*image.PixelWidth+x]:X4}";}
            }
            check(exact,$"Valid pixels must remain exact (FRLG={firered}, secondary={secondary}, count={count}, formatted={formatted}): {mismatch}");
            check(before.SequenceEqual(model.RawData)&&metadata.SequenceEqual(model.ExportMetadata(null,singletons.MetadataInfo).Serialize()),"Rendering must not alter ROM bytes, pointers, anchors or metadata.");
         }
      }
   }
}
