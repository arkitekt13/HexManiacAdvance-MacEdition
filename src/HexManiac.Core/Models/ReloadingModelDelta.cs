using System;

namespace HavenSoft.HexManiac.Core.Models {
   /// <summary>A byte edit that rebuilds derived formats from project metadata on Undo/Redo.</summary>
   public sealed class ReloadingModelDelta : ModelDelta {
      private readonly ModelDelta bytes;
      private readonly string[] metadata;
      private readonly IMetadataInfo info;
      private readonly Action restoreTracking;

      private ReloadingModelDelta(ModelDelta bytes, string[] metadata, IMetadataInfo info, Action restoreTracking) {
         this.bytes = bytes;
         this.metadata = metadata;
         this.info = info;
         this.restoreTracking = restoreTracking;
         HasDataChange = bytes.HasDataChange;
      }

      /// <summary>Apply already-validated bytes in isolation; the caller publishes the returned token to history after success.</summary>
      public static ReloadingModelDelta Apply(IDataModel model, byte[] candidate, IMetadataInfo info) {
         if (candidate == null || candidate.Length != model.Count || candidate.Length == 0 || candidate.Length > Patcher.MaximumPatchedLength)
            throw new ArgumentException("The reloadable patch must preserve the ROM size.", nameof(candidate));
         var metadata = model.ExportMetadata(null, info).Serialize();
         var tracking = (model as BaseModel)?.CaptureChangeTracking();
         var attempt = new ModelDelta();
         try {
            for (int i = 0; i < candidate.Length; i++) {
               if (model[i] != candidate[i]) attempt.ChangeData(model, i, candidate[i]);
            }
            Reload(model, metadata);
            return new ReloadingModelDelta(attempt, metadata, info, tracking);
         } catch {
            attempt.Revert(model);
            Reload(model, metadata);
            tracking?.Invoke();
            throw;
         }
      }

      /// <summary>Restore bytes, named definitions and the previous edited-byte highlighting as one history operation.</summary>
      public override ModelDelta Revert(IDataModel model) {
         var reverseMetadata = model.ExportMetadata(null, info).Serialize();
         var reverseTracking = (model as BaseModel)?.CaptureChangeTracking();
         var reverse = bytes.Revert(model);
         try {
            Reload(model, metadata);
            restoreTracking?.Invoke();
            return new ReloadingModelDelta(reverse, reverseMetadata, info, reverseTracking);
         } catch {
            reverse.Revert(model);
            Reload(model, reverseMetadata);
            reverseTracking?.Invoke();
            throw;
         }
      }

      private static void Reload(IDataModel model, string[] metadata) {
         model.Load(model.RawData, new StoredMetadata(metadata));
         model.InitializationWorkload.GetAwaiter().GetResult();
         model.ClearCacheScope();
      }
   }
}
