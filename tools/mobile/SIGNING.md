# Development APK identity

`development-signing-key.base64` is an intentionally PUBLIC, non-production test key.
Alias: `androiddebugkey`; store/key password: `android`. It is used only to sign Debug
previews in this repository so 0.3.0 and later previews can update in place and retain
the new local profile. Never use this key for a production release or Play Store identity.

0.1 and 0.2 used ephemeral runner debug keys. Installing 0.3 may require uninstalling
that older preview once; those versions had no durable profile. Subsequent previews
must preserve the application ID, key and increasing ApplicationVersion.

Local matching build (PowerShell): decode the base64 file to a local `.jks` file and
pass the same AndroidSigning* properties as `.github/workflows/mobile.yml`.
Generated binary keys remain outside the checkout and must not be committed.
