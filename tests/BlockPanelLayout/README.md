# Block panel layout

The actual block-header controls are detached from MapTab and rendered with a
small layout-only model. No ROM is used. Six width/font combinations (256/320/512
pixels and 14/20-point text) check that Block ID, automatic movement permission,
the movement selector and Edit Block do not overlap or exceed their panel. Real
checkbox clicks also verify that the existing two-way permission binding works.

Build the project and run its DLL. An optional absolute PNG path saves a preview.
The baseline fails at 256px/14pt: the permission checkbox (5,139,212,15) overlaps
Block ID (191,137,65,18). Measured rows fix the overlap. The follow-up Movement-row regression also checks
that its label and selector share a center and that the label fits inside the
selector height; the previous fixed 30px/20px heights fail that check. Removing
those conflicting heights fixes the reported protruding label; 60 assertions pass.

Block ID now sits to the left of Edit Block in a shared three-column row: label,
reserved value space and button. Seven representative IDs (0 through 1023) check
that every column stays in exactly the same position and the text fits at each
width/font combination. The expanded group passes 156 assertions.

The Block ID label/value group is centered within the space before the independently
reserved Edit Block column. Its numeric field remains 48px wide so digit changes
do not shift the group. Centering, narrow-width non-overlap and existing movement
row alignment bring this group to 162 assertions.
