# Signing and notarizing Mac Edition 1.0.0

The development app remains ad-hoc signed. A separate Apple Silicon 1.0.0 release copy
was Developer ID signed and accepted by Apple's notary service on November 16, 2026.
Its stapled ticket, deep strict signatures and Gatekeeper assessment pass, including
after ZIP extraction. Public GitHub release publication is separate from notarization.
See the [release verification record](../tests/MacPackaging/notarization-1.0.0-verification.json).

## 1. Set up the signing certificate

Open Xcode Settings and its Accounts / Apple Accounts pane. Sign in with the Apple
Developer Program account and select the intended team. Open Manage Certificates and
create a **Developer ID Application** certificate, if that option is available for the
account. This is the certificate for distributing the app outside the App Store.
An existing certificate can instead be imported together with its private key.

If creation is unavailable in Xcode, follow [Apple's Developer ID certificate guide](https://developer.apple.com/help/account/certificates/create-developer-id-certificates)
using the Account Holder's access. Keychain Access's macOS menu bar contains Certificate
Assistant > Request a Certificate From a Certificate Authority. Use Saved to disk, leave
the CA email blank, upload the request on Apple's website and install the downloaded
certificate in the same Mac's login keychain. Do not revoke an existing certificate just to add this Mac.
Confirm the identity is available with:

```sh
security find-identity -v -p codesigning
```

## 2. Set up notarization access locally

Create a Keychain profile interactively in Terminal:

```sh
xcrun notarytool store-credentials "HMA-notary"
```

Enter the Apple Account, developer Team ID and app-specific password in the local prompts.
Credentials belong in Keychain, not repository files or chat. Apple's
[notarization workflow](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow)
also supports API-key authentication if preferred.

## 3. Prepare and verify the signed release

Use a separate staging copy of the verified app. Sign its nested native code and then
the app with the same Developer ID identity, secure timestamps and Hardened Runtime.
The .NET apphost needs the JIT entitlement; use [Microsoft's macOS publishing guidance](https://learn.microsoft.com/en-us/dotnet/core/deploying/macos)
when preparing the entitlements. Check the embedded libraries and any helper executables
individually rather than relying on deep signing to discover everything.

Verify signatures and inspect the resulting entitlements. Test launching, saving/reopening
a disposable project, external emulator selection, and tutorial creation with the signed app.
Keep version 1.0.0 and the existing app identifier stable.

The tested signing helper creates a new copy, relocates the self-contained payload into
Contents/Resources and generates a matching SDK apphost bound to the managed assembly
there. This keeps managed DLLs out of native-code locations. It preserves every managed
assembly/resource byte, signs native components inside out, enables only the .NET JIT
entitlement and records hashes/signatures. It refuses to overwrite an existing destination.

```sh
python3 scripts/sign-mac-release.py \
  --source artifacts/mac/osx-arm64/HexManiacAdvance.app \
  --destination artifacts/releases/NEW-STAGING-FOLDER/HexManiacAdvance.app \
  --identity "YOUR DEVELOPER ID APPLICATION IDENTITY"
```

The helper needs the .NET 8 SDK and matching runtime host pack. Its HostWriter tool does
not rebuild or change the verified managed application. The packaged-project review tool
supports both development and release layouts; run its console host on an isolated copy
of the actual self-contained runtime, never add test binaries inside the signed release.

The existing publish-mac.sh and package-mac.sh produce ad-hoc development builds. Running
them after Developer ID signing would replace the signed app. Final release packaging
must instead use the verified signed staging copy.

## 4. Notarize and distribute

Archive the signed app with ditto and submit that ZIP through notarytool using the Keychain
profile and --wait. Require Accepted and inspect the submission log. Staple the ticket to
the app, validate it with stapler, and check Gatekeeper's assessment. Recreate the final
ZIP from the stapled app and calculate its SHA-256 afterward. Apple documents this
[submit, inspect and staple sequence](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow).

For an already-prepared payload containing the notarized app, LICENSE, dependency notices,
START-HERE.txt and BUILD-INFO.txt, scripts/package-notarized-mac.py validates the input,
copies it with ditto, archives it, extracts it and repeats signature/ticket/Gatekeeper checks.
It publishes a new ZIP, SHA-256 sidecar and verification record without replacing files.
It never invokes the ad-hoc publishing script or re-signs the app.

Test the downloaded ZIP's installation and launch before publishing the GitHub release.
Include the license, applicable notices and concise instructions. Ship patches and metadata,
never a base or patched ROM. Advertise Apple Silicon support; Intel hardware is not yet verified.
Update README's download and signing status only after these steps actually pass.

## Verified November 16 candidate

Apple accepted submission `c66b37fa-342a-4b40-9840-e87e538764af` without issues. The stapled final Apple Silicon ZIP passes signature, ticket and Gatekeeper checks after extraction. See the [verification record](../tests/MacPackaging/notarization-1.0.0-verification.json). The verified download is distributed through the [1.0.0 GitHub release](https://github.com/arkitekt13/HexManiacAdvance-MacEdition/releases/tag/v1.0.0). The development app was preserved.
