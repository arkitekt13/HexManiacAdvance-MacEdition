# Ikarus Visual Upgrade — SCADE (Experimental)

Exact-source compatibility package for fresh bundled SCADE 2026-11-15 output.
The BPS relocates conflicting assets; the TOML retains all 556 SCADE named
definitions and 79 lists, adding 83 relocation/reservation entries.

The utility creates a new named ROM/TOML pair before opening it. Original files
remain untouched. Edited bases, wrong versions and already-patched games are
rejected by full source SHA-256. This is separate from the vanilla Ikarus IPS.

Automated map, asset and save/reopen checks and a basic native emulator smoke test
pass. Broader battles, regional animations and progression remain unverified.
See docs/IKARUS_SCADE_COMPATIBILITY_EXPERIMENT.md and Creator-Credits.txt.

SCADE: ShinyMiner & Team — https://github.com/Shiny-Miner/CFRU-expansion
Ikarus: Ikarus & Many Contributors; fixed release by LibertyTwins —
https://www.pokecommunity.com/threads/ikarus-tileset-patch-v3-2-fixed-version-gen-4-tiles-for-fire-red.532686/
