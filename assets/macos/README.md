# Mac Edition app icon

`AppIcon.png` is square source artwork for the Mac Edition icon: clean white HMA
lettering over a small pixel-map scene based on arkitekt13's Tutorial Village
welcome background. The artwork was generated for this project using that supplied
map as its reference. It does not replace the original Windows character icon.
The full-bleed background and safely inset lettering accommodate system masking.

`AppIcon.icns` contains the standard 16, 32, 128, 256 and 512 point representations
at 1x and 2x resolution, up to 1024 pixels. Regenerate it on macOS with:

```sh
bash scripts/build-mac-icon.sh
```

`publish-mac.sh` copies the committed ICNS into `Contents/Resources` and declares
`CFBundleIconFile` before signing. Finder, Dock and the application launcher use
the bundle icon. Missing artwork blocks publishing and preserves the previous app.

Do not modify an installed signed/notarized app to add the icon: prepare a fresh
release bundle, sign it, notarize it and distribute a newly verified archive.
