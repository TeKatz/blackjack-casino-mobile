# Blackjack Empire Mobile

Independent Android client for Blackjack Empire. This repository starts from the
Windows desktop application's pinned source and complete Git ancestry, then develops
a separate touch interface. The desktop repository is unchanged.

## Playable Android preview · 0.6.0

Play a complete Blackjack round using the original Core: choose a wager, Hit/Stand,
Double, Split, Surrender or decide on Insurance when offered. The dealer hole card is
hidden until settlement; results update the practice bankroll exactly once.

A persistent local profile now connects play cash, XP, levels, Coins, Blackjack
statistics, round history, challenges, the season pass and a title shop. Open rounds
and rewards save after each accepted action and resume after restarting the app.
The Collector's Album now opens earned packs, displays all 136 existing cards across
16 themes, recycles duplicates and awards completed themes. Pack reveals resume at the
saved card after restart. A Coin-only pack shop includes drop odds and purchase confirmation.
Midnight Dice adds a complete second game: Casual play and casino tables against
three AI opponents, exact dice/hold recovery, thirteen-category score sheets,
standings, final net results, XP, season rewards and Dice challenges. A dedicated
DiceSet shop equips the original cosmetic themes with Coin-only purchases. Dice
casino entry fees use play cash. Paid Blackjack and Dice games run one at a time;
Casual Dice and Album can coexist with a saved Blackjack hand.

The Casino Floor adds Midnight Reels, Neon Vault and Nine Lives using the original
Core rules, weighted draws and features. Bonus spins, locked trigger wagers, Neon
energy and Security Override resume after restarting. Results, wallet, XP, season
rewards and statistics save together before finite column-reveal feedback. Machine
paytables, native vector symbols and twenty recent spin receipts are included.
The Casino Vault fills from 2% of new eligible wagers and grants the original cash,
tier bonus and card pack when full. Historical wagers establish career tiers without
retroactive Vault cash. Saved paid games and pending bonuses have explicit cash-game locks.
Settings include larger playing cards, surrender confirmation, remembered wagers
and separate reduced-animation settings for packs, dice and slots.

Download `BlackjackEmpire-debug-apk` from a successful **Android foundation** Actions run,
extract `BlackjackEmpire-debug.apk`, and install it on Android 8.0 or later.
Install 0.6 over the existing 0.3–0.5 app without uninstalling. The signing identity and
application ID are unchanged, preserving the local profile. The old 0.1/0.2 previews
used temporary signing keys and had no durable profile.
No desktop saves are imported or changed.

[Download the verified 0.6.0 preview](https://github.com/TeKatz/blackjack-casino-mobile/actions/runs/41434585865/artifacts/13135484281)
(approximately 97.6 MB ZIP, available until 2026-11-16). All 499 tests passed; both
the desktop reference and Android APK builds completed without warnings or errors.
The 0.3 profile/restart device test passed; the user reports that 0.5 runs without
major errors. Device acceptance of the new 0.6 integrations remains pending.

See [Casino Floor integration and device checklist](docs/mobile-casino-floor.md),
[Midnight Dice integration](docs/mobile-midnight-dice.md),
[Album integration](docs/mobile-collector-album.md), and the
[profile architecture](docs/mobile-profile-progression.md).

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
