# Signing and notarizing Mac Edition 1.0.0

The verified development app is still ad-hoc signed. Developer ID signing and Apple's
notarization are the remaining distribution steps; App Store submission is not required.
Xcode's notarytool and stapler are installed on the development Mac. No usable local
code-signing identity was found during the November 16 readiness check.

## 1. Set up the signing certificate

Open Xcode Settings and its Accounts / Apple Accounts pane. Sign in with the Apple
Developer Program account and select the intended team. Open Manage Certificates and
create a **Developer ID Application** certificate, if that option is available for the
account. This is the certificate for distributing the app outside the App Store.
An existing certificate can instead be imported together with its private key.

If creation is unavailable in Xcode, follow [Apple's Developer ID certificate guide](https://developer.apple.com/help/account/certificates/create-developer-id-certificates)
using the Account Holder's access. Do not revoke an existing certificate just to add this Mac.
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

The existing publish-mac.sh and package-mac.sh produce ad-hoc development builds. Running
them after Developer ID signing would replace the signed app. Final release packaging
must instead use the verified signed staging copy.

## 4. Notarize and distribute

Archive the signed app with ditto and submit that ZIP through notarytool using the Keychain
profile and --wait. Require Accepted and inspect the submission log. Staple the ticket to
the app, validate it with stapler, and check Gatekeeper's assessment. Recreate the final
ZIP from the stapled app and calculate its SHA-256 afterward. Apple documents this
[submit, inspect and staple sequence](https://developer.apple.com/documentation/security/customizing-the-notarization-workflow).

Test the downloaded ZIP's installation and launch before publishing the GitHub release.
Include the license, applicable notices and concise instructions. Ship patches and metadata,
never a base or patched ROM. Advertise Apple Silicon support; Intel hardware is not yet verified.
Update README's download and signing status only after these steps actually pass.
