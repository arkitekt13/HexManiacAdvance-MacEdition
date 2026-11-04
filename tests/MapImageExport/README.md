# Map PNG export

Mac map exports save native-resolution PNGs beside the ROM in `MapImages`, using the map's
name and bank/map ID; connected exports add `-connected`. Existing destinations prompt for
Overwrite, Save Updated Copy (numbered from 2), or Cancel; closing the prompt cancels. Unsaved
ROMs and unavailable destination folders use the save picker. Successful export offers Reveal
in Finder. Other filesystem implementations retain clipboard export.

```sh
dotnet restore tests/MapImageExport/MapImageExport.csproj
dotnet build tests/MapImageExport/MapImageExport.csproj --no-restore --disable-build-servers -p:UseSharedCompilation=false -m:1
dotnet tests/MapImageExport/bin/Debug/net8.0/MapImageExport.dll
```

Checks use isolated temporary folders, the production destination policy, the Mac PNG encoder,
and a synthetic map passed through the actual Core export method. They cover conflict choices,
numbering, a competing writer, partial encoding failure, temp cleanup, unavailable destinations,
unsaved ROMs, safe filenames, PNG dimensions, all three display options, connected naming and
restoration after success/failure, and unchanged ROM bytes, metadata and history. The isolated
Core method test supplies its dependencies directly to avoid unrelated editor startup requirements.
No user ROM or settings are read or changed.

Manual: Edit Map Header > Export Image > This Map, test each event option and conflict action,
then export All Connected Maps; check sprite/marker appearance, native dialog cancellation,
read-only folders, Finder reveal and the saved PNG in Preview.
