# Welcome map background

`background.png` currently contains the Tutorial Village map export supplied by
arkitekt13, first added and refreshed on 2026-11-08. The current supplied PNG is
1312 × 960 (about 267 KiB), including the exported border. The original map export
and ROM are untouched by this asset update.

Replace this file as the village evolves, then build or publish normally. It is
copied into `resources/Optional/Welcome/background.png` beside the app's binaries.
No code changes are needed. Rebuild rather than modify a signed distributed bundle.

The existing proportional fill crops to each window's shape and applies a dark
fade behind the controls. A fixed 1280 × 800 crop is not required; preserving this
export retains room to crop at different window sizes. Current previews use the
full image, centered by the existing Image control, with no permanent trimming.

Missing or invalid images retain the plain background. The image is decoded once
per attachment, downsampled to at most 1600 pixels wide and released when the
welcome control leaves the visual tree. Files over 32 MiB are ignored.
