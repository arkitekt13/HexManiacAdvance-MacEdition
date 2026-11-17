# Mac Edition 1.0.1

A targeted crash repair for Apple Silicon Macs. All 1.0.0 features and bundled utilities remain included.

- Fix a crash when opening maps or selecting tiles in SCADE projects with missing level-up learnset pointers, including the reported SCADE + Ikarus Cerulean project.
- Keep readable species available for trainer move validation without changing game data or bundled patches.
- Preserve the independent Core/TOML version (0.6.1), so this app version change does not trigger project metadata migration.

The repair passed 89 Mac regression groups (25,324 assertions), 36 focused trainer checks, and disposable-copy tests of the reported project and vanilla/HUBOL/SCADE bases. These checks do not establish complete gameplay compatibility between ROM upgrades.

## Updating

Save your work and quit HMA, download the Apple Silicon ZIP, then replace the app in Applications. Keep each project’s `.gba` and matching `.toml` together. The app’s update notice opens the release download page; it does not install updates automatically. Startup checks are cached for up to a day.

See the [1.0.0 feature notes](RELEASE_NOTES_1.0.0.md) for the full feature list and utility creator credits. No game ROMs or emulators are included.
