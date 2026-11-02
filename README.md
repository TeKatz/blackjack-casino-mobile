# Blackjack Empire Mobile

Independent Android client for Blackjack Empire. This repository starts from the
Windows desktop application's pinned source and complete Git ancestry, then develops
a separate touch interface. The desktop repository is unchanged.

## Playable Android preview · 0.2.0

Play a complete Blackjack round using the original Core: choose a wager, Hit/Stand,
Double, Split, Surrender or decide on Insurance when offered. The dealer hole card is
hidden until settlement; results update the practice bankroll exactly once.

Practice cash starts at $1,000. Cash and open rounds survive lobby navigation and
backgrounding while the process remains alive; restarting the app process resets them.
Profile saving and progression are planned for the next milestone.

Download `BlackjackEmpire-debug-apk` from a successful **Android foundation** Actions run,
extract `BlackjackEmpire-debug.apk`, and install it on Android 8.0 or later.
The development APK includes its assemblies and does not require a debugger.
Debug signing keys may differ between runners: uninstall an older preview if Android
reports a signature mismatch. No desktop saves are touched.

See [playable scope and device checklist](docs/mobile-blackjack.md).

## Build

Use .NET 10, JDK 17, Android SDK and `dotnet workload install maui-android`.

```powershell
dotnet build src/BlackjackCasino.Mobile/BlackjackCasino.Mobile.csproj -t:InstallAndroidDependencies -f net10.0-android -p:AcceptAndroidSDKLicenses=true
dotnet build src/BlackjackCasino.Mobile/BlackjackCasino.Mobile.csproj -c Debug -t:SignAndroidPackage
```

`BlackjackCasino.Mobile.slnx` contains only Mobile and Core. The inherited
`BlackjackCasino.sln` remains a separate desktop reference solution and needs no
Android workload. The inherited Core's Sudoku generation currently requires Windows
PowerShell, so the initial build pipeline deliberately uses Windows runners.

## Branches and next step

`main` holds the foundation; use `development` and short `mobile/*` feature branches
for subsequent Android changes. Desktop changes are imported deliberately after review,
never synchronized or pushed back automatically.

The foundation APK has passed the user's installation, launch and navigation test.
The next device test covers a complete round in 0.2.0, followed by persistent Android
profiles and lifecycle-safe saving. See [architecture and acceptance](docs/mobile-foundation.md).
