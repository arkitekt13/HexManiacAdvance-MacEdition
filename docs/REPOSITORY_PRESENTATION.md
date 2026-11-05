# GitHub repository presentation

Updated November 4, 2026 for helper distribution. Changes to About and Issues are repository
settings, not Git objects; this record makes their before/after values reviewable and reversible.
README/setup/credits and the issue template are tracked normally in Git.

| Setting | Before | After |
| --- | --- | --- |
| Description | macOS build of HexManiacAdvance - an Avalonia port of the Pokemon GBA ROM editor, with a built-in emulator, live memory viewer and MCP server. HexManiac.Core unmodified. | HexManiacAdvance Mac Edition: a macOS GBA ROM editor built on Haven's HMA and xavierkeys' Mac port, with improved map/script workflows, Mac controls, external emulator launch and regression-tested fixes. |
| Website | Empty | https://github.com/arkitekt13/HexManiacAdvance-MacEdition/blob/avalonia-port/README.md#on-macos |
| Issues | Disabled | Enabled for helper bug reports |

No repository name, visibility, default branch, merge policy, collaborators or optional app
features changed in this presentation update. Revert tracked documents with a normal Git revert;
to revert the external settings use the About editor and Settings > General > Features > Issues
with the preceding values. Turning Issues off also breaks the app's existing issue links.

Attribution uses the original commit author name xavierkeys and links the verified fork parent
[Xkeys-bot/HexManiacAdvance-mac](https://github.com/Xkeys-bot/HexManiacAdvance-mac); the old
`github.com/xavierkeys` target could not be verified. Haven's original project/wiki remain
prominent, and arkitekt13 is identified only as this fork's maintainer.
