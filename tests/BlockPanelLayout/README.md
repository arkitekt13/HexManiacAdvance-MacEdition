# Block panel layout

The actual block-header controls are detached from MapTab and rendered with a
small layout-only model. No ROM is used. Six width/font combinations (256/320/512
pixels and 14/20-point text) check that Block ID, automatic movement permission,
the movement selector and Edit Block do not overlap or exceed their panel. Real
checkbox clicks also verify that the existing two-way permission binding works.

Build the project and run its DLL. An optional absolute PNG path saves a preview.
The baseline fails at 256px/14pt: the permission checkbox (5,139,212,15) overlaps
Block ID (191,137,65,18). Measured rows fix the overlap; 48 assertions pass.
