# Mobile 0.4.0: Collector's Album and durable pack reveals

Branch: `mobile/collector-album`, based on user-accepted 0.3.0 profile/progression.
No merge to main/development and no changes to the original desktop repository.

## Playable integration

- Original 136-card / 16-theme collection, including the four ultra-rare themes.
- Theme progress, locked/owned cards, full card details and rarity-colored frames.
- Existing art is cropped from verified source atlases into Android-safe JPEG filenames;
  mobile screens load individual images, never entire atlases.
- Packs already earned by challenges and season progression, plus original welcome packs,
  become available without regranting rewards on upgrade.
- Five-card opening with NEW/duplicate labels, one-card reveal or Reveal All, and a
  one-shot fade/scale transition. No continuous animation loop or blur layers.
- Inventory, Coin-only pack shop with purchase confirmation, original costs and guarantees.
- Odds disclosure separates the independent ultra-rare roll from conditional normal-theme
  chances and shows first-slot / other-slot weights directly from Core.
- Automatic duplicate-point recycling and original one-time theme-completion rewards.
- Resume an interrupted reveal from the lobby or album. Previously revealed slots can be reviewed.
- Reduced pack-animation preference persisted alongside the existing settings.

## Persistence and compatibility

Core CollectionService owns draws, inventory, duplicate points and theme rewards.
Mobile wraps it in the existing snapshot transaction; no drop algorithm is duplicated.
The five-card receipt and all inventory changes commit before the first reveal. The
revealed count then commits before each animation. Restarting resumes the same receipt,
card identities and reveal position, without spending another pack or paying rewards again.
Completion requires all five cards and the matching receipt ID.

The schema-1 envelope adds optional fields `RevealedCards` (default 0) and
`Settings.ReducePackAnimations` (default false). Old 0.3 snapshots remain readable;
existing profile, Blackjack hand, settings, packs and reward ledgers are retained.
A Blackjack hand and pack reveal may coexist and are written in the same snapshot.
Save failure rolls back opening, reveal position, purchases and completion.

Application ID and development signing key are unchanged; ApplicationVersion increases
to 4. Install over 0.3 in place. Do not uninstall when updating an existing persistent profile.

## Artwork preparation and builds

Before a local Android build:

```powershell
python -m pip install Pillow==13.0.0
python tools/mobile/prepare_collection_art.py
dotnet build src/BlackjackCasino.Mobile/BlackjackCasino.Mobile.csproj -c Debug -t:SignAndroidPackage
```

Use the stable signing properties from tools/mobile/SIGNING.md when updating the CI APK.
The generator checks all atlas SHA-256 hashes, crop bounds, unique pixel content,
Android resource names and all 136 decoded outputs. Generated files are ignored and
recreated in CI before project evaluation/build. `Resize=False` preserves the prepared
JPEGs without another resampling pass. Regular cards retain native resolution; large
ultra-rare crops are capped at 416px wide. Combined image payload is approximately 14 MB.
The build artifact also contains `mobile-album-art-report.json` with image hashes and sizes.

## Validation

New tests cover old-profile migration, welcome-pack non-duplication, receipts and reveal
position, duplicate recycling, purchases, theme rewards, storage-failure rollback,
invalid reveal states and simultaneous Blackjack/album restoration. Existing tests remain.
Artwork preparation and representative normal/ultra-rare image inspection completed locally.
CI and APK evidence will be recorded after validation.

## Device acceptance

- Update over 0.3 without uninstalling; verify name, Coins, XP, stats and saved Blackjack hand.
- Open Album, inspect owned/missing cards and details, and browse standard/ultra-rare themes.
- Open an earned pack, reveal two cards, force-stop and reopen; continue the same pack at 2/5.
- Finish it, restart again and verify no extra pack spent, no duplicate payment and no reroll.
- Try Reveal All, revisit numbered cards and the reduced-animation setting.
- Check inventory, purchase confirmation and odds; Coins and play cash must remain separate.
- Where complete, claim a theme reward once and verify that it stays claimed after restart.
- Check scrolling, image loading, portrait/landscape and card-size readability on the phone.

Device acceptance of 0.4 remains pending until the user tests this APK.
