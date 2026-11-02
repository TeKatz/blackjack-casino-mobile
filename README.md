# Blackjack Empire Mobile

Independent Android client for Blackjack Empire. This repository starts from the
Windows desktop application's pinned source and complete Git ancestry, then develops
a separate touch interface. The desktop repository is unchanged.

## Playable Android preview · 0.4.0

Play a complete Blackjack round using the original Core: choose a wager, Hit/Stand,
Double, Split, Surrender or decide on Insurance when offered. The dealer hole card is
hidden until settlement; results update the practice bankroll exactly once.

A persistent local profile now connects play cash, XP, levels, Coins, Blackjack
statistics, round history, challenges, the season pass and a title shop. Open rounds
and rewards save after each accepted action and resume after restarting the app.
The Collector's Album now opens earned packs, displays all 136 existing cards across
16 themes, recycles duplicates and awards completed themes. Pack reveals resume at the
saved card after restart. A Coin-only pack shop includes drop odds and purchase confirmation.
Settings include larger playing cards, surrender confirmation, a remembered wager
and reduced pack animations.

Download `BlackjackEmpire-debug-apk` from a successful **Android foundation** Actions run,
extract `BlackjackEmpire-debug.apk`, and install it on Android 8.0 or later.
0.3 introduces a stable development signing identity; installing over 0.2 may require
one uninstall. Keep 0.3 installed for later updates to preserve its new local profile.
No desktop saves are imported or changed.

The 0.3 profile/restart device test passed. The new 0.4 APK is delivered after CI validation.

See [album integration and device checklist](docs/mobile-collector-album.md), and the [profile architecture](docs/mobile-profile-progression.md).

## Build

Use .NET 10, JDK 17, Android SDK and `dotnet workload install maui-android`.

```powershell
python -m pip install Pillow==13.0.0
python tools/mobile/prepare_collection_art.py
dotnet build src/BlackjackCasino.Mobile/BlackjackCasino.Mobile.csproj -t:InstallAndroidDependencies -f net10.0-android -p:AcceptAndroidSDKLicenses=true
dotnet build src/BlackjackCasino.Mobile/BlackjackCasino.Mobile.csproj -c Debug -t:SignAndroidPackage
```

For local builds that must update the CI preview in place, use the same development
signing identity described in [tools/mobile/SIGNING.md](tools/mobile/SIGNING.md).

`BlackjackCasino.Mobile.slnx` contains only Mobile and Core. The inherited
`BlackjackCasino.sln` remains a separate desktop reference solution and needs no
Android workload. The inherited Core's Sudoku generation currently requires Windows
PowerShell, so the initial build pipeline deliberately uses Windows runners.

## Branches and next step

`main` holds the foundation; use `development` and short `mobile/*` feature branches
for subsequent Android changes. Desktop changes are imported deliberately after review,
never synchronized or pushed back automatically.

The foundation APK has passed the user's installation, launch and navigation test.
The user also confirmed complete Blackjack rounds in 0.2.0. The 0.3 device test
covers persistent rounds, profile progression and in-place updates. See [architecture and acceptance](docs/mobile-foundation.md).
