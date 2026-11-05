# Welcome map background

Place the final image here as **background.png**, then build or publish normally.
It is copied into `resources/Optional/Welcome/background.png` beside the app's
binaries. No code changes are needed. Rebuild the app rather than modify a signed
distributed bundle.

An 80 × 50 tile map exports to 1280 × 800 pixels, a suitable starting size.
The image fills the window with proportional cropping and a dark fade behind
the existing controls. Missing or invalid images retain the plain background.
The image is decoded once per attachment, downsampled to 1600 pixels wide and
released when the welcome control leaves the visual tree. Files over 32 MiB
are ignored. This folder intentionally ships without a placeholder image.
