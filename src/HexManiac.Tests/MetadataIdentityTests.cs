using HavenSoft.HexManiac.Core.Models;
using System;
using System.Reflection;
using Xunit;

namespace HavenSoft.HexManiac.Tests {
   public class MetadataIdentityTests : BaseViewModelTestClass {
      private static Version DeclaredVersion {
         get {
            var attribute = typeof(Singletons).Assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
            Assert.NotNull(attribute);
            return new Version(attribute.Version);
         }
      }
      private static string ExpectedVersion => DeclaredVersion.ToString(DeclaredVersion.Revision > 0 ? 4 : 3);

      [Fact]
      public void CoreMetadataVersion_UsesDeclaredFileVersionOnEveryPlatform() {
         Assert.Equal(ExpectedVersion, Singletons.MetadataInfo.VersionNumber);
         Assert.Equal(DeclaredVersion.Revision <= 0, Singletons.MetadataInfo.IsPublicRelease);
      }

      [Fact]
      public void ExportedMetadata_RoundTrip_PreservesDeclaredCoreVersion() {
         var metadata = Model.ExportMetadata(Singletons.MetadataInfo);
         var reloaded = new StoredMetadata(metadata.Serialize());

         Assert.Equal(ExpectedVersion, metadata.Version);
         Assert.Equal(ExpectedVersion, reloaded.Version);
      }

      [Fact]
      public void OlderMetadata_IsRecognizedAsNeedingMigration() {
         Assert.True(StoredMetadata.NeedVersionUpdate("0.0.1", Singletons.MetadataInfo.VersionNumber));
         Assert.False(StoredMetadata.NeedVersionUpdate(ExpectedVersion, Singletons.MetadataInfo.VersionNumber));
      }
   }
}
