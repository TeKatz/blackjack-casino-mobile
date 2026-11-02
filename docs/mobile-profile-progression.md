# Mobile 0.3.0: profile, durable rounds and progression

Branch: `mobile/profile-progression`, based on the user-tested `mobile/blackjack-round`.
No changes to the original desktop repository, inherited Core source or desktop save formats.

## Integrated systems

- Private Android profile: player name, persistent play cash, XP, levels, Coins and settings.
- Original Core ProgressionService handles Blackjack statistics, XP, level Coins,
  season progression and automatic rewards. No parallel reward formulas.
- Profile statistics and the most recent 20 completed rounds (10 shown in the profile screen).
- Paginated/searchable supported Blackjack/lifetime challenges, single-claim reward ledger.
- Original season catalog: choose an active season, inspect tiers, automatically unlock rewards.
- Title shop: original prices and ownership, atomic buy/equip, equipped title in the lobby.
- Reward packs and non-title cosmetics granted by Core remain durably stored for future Album/UI work.
- Larger cards, surrender confirmation, validated player names and last accepted wager.
- Play cash and Coins stay separate. Empty play cash can be refilled without erasing profile progress.

## Durable round design

Every accepted mutation is saved before its result is shown. A full ordered shoe plus
round-start cursor and command journal reconstruct the original Core round on restart.
Replay checks command legality, affordability, shoe position and wallet agreement. It
never processes rewards again. Settled state, wallet, rewards, history and receipt are
written in one snapshot, avoiding partial settlement or duplicate XP/Coins.

Saving uses a checksummed version-1 envelope under Android FileSystem.AppDataDirectory,
an fsynced temporary file, same-directory replacement and a previous valid backup.
Broken primary files are quarantined and never replace the known-good backup. If no
valid save remains, or a newer unsupported version is found, startup preserves files
and shows an error rather than silently starting over. Storage errors roll back the
current action, including drawn cards and progression. Backup recovery can lose the
most recent action and is disclosed in the lobby.

0.2 had no durable save. 0.3 creates the first local profile with $1,000 play cash.
This is separate from desktop profiles; no desktop import or cloud sync is introduced.
Uninstalling still removes local data.

## Development APK updates

A stable PUBLIC development-only signing key is now used in CI. This supports in-place
updates from 0.3 onward while retaining the local profile. See tools/mobile/SIGNING.md.
Installing 0.3 over the ephemeral-key 0.2 preview can require one uninstall; the old
preview had no durable profile. Never use this test key for production releases.

## Validation

The existing tests plus new deterministic persistence/replay tests cover active hands,
insurance, split/double, settled restore without duplicate rewards, XP/level rewards,
season rewards, challenge claims, ownership, failed-write rollback, corrupted saves,
backup recovery, unsupported versions and bounded history. Pure Game code remains
linked into the test project without requiring Android workloads.

CI/build and APK evidence will be recorded after validation completes.

## Device acceptance

- Install 0.3, name the profile, play and verify XP/Coins/statistics.
- Force-stop and reopen midway through a hand; verify identical cards, reserved stake
  and hole-card concealment, then complete and verify one settlement.
- Force-stop again after settlement; verify cash, XP and round count do not increase.
- Try insurance and split/double when offered, including restart partway through.
- Check challenge progress and one-time claiming; browse the season track and title shop.
- Verify larger-card and surrender-confirmation settings survive relaunch.
- Keep this app installed for future in-place updates so its local profile is retained.

Audio, new games, full cosmetic artwork, Album presentation, achievements UI, Prestige,
cloud synchronization and production release signing remain later work.
