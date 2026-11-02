# Mobile 0.5.0: Midnight Dice

Branch: `mobile/midnight-dice`, based on the delivered 0.4.0 Collector's Album.
The 0.4 device acceptance result has not been provided. This update adds a second
playable game without changing inherited Core or the original Desktop repository.

## Playable systems

- Casual classic five-dice game: five dice, up to three rolls, hold/release and all
  thirteen categories. Available scores update after a roll; zero entries require confirmation.
- Casino mode: original Core stakes, three distinct AI opponents, live standings,
  inspectable opponent sheets, original placement prizes and tie splitting.
- Entry confirmation shows the fee and all four prizes before spending play cash.
  Fees never spend Coins. Back saves the table; forfeiting needs confirmation and
  loses the paid fee without granting completion XP.
- Final scoreboard shows score, bonuses, placement, returned cash, net gain/loss
  and XP/season/level rewards, and remains available after a restart.
- Profile includes separate Dice statistics. Original Dice challenges grant Coins
  and Album packs exactly once through the existing progression service.
- Original DiceSet catalog with ownership, original prices, buy/equip and native
  vector pip previews. Themes change appearance only, never random values or rules.
- One-shot roll feedback, player/opponent five-of-a-kind announcements and a saved
  reduced-dice-animation setting. No timer, repeated animation loop or background polling.

Core uses its existing classic repeated-five-of-a-kind rule (+50 plus maximum in
any open box), which is disclosed in the Dice room. No parallel scoring, dice
randomness, AI strategy, payout schedule or reward formulas are introduced.

## Persistence and wallet boundaries

Optional `CasualDice`, `DiceTable`, `CasualDiceAward`, `DiceTableAward` and
`Settings.ReduceDiceAnimations` extend the existing schema-1 snapshot. Missing
fields from 0.3/0.4 default safely; identity, cash, open Blackjack hands, Album,
pack reveals and reward ledgers remain in the same atomic save envelope.

Every roll and hold is saved before displaying its final state. Score commits
finish all AI turns off the UI thread, then atomically save every score sheet,
cash, progression and reward receipts. An optimistic checkpoint comparison rejects
stale AI results. Saved casino turns never contain a half-completed AI round.
Restarting restores the exact rolled dice and holds and does not reroll or pay again.
Storage failures roll the entire action back. Core validation failures preserve
the original files instead of silently discarding a game.

Casual Dice and Album can coexist with a saved Blackjack hand. Paid Dice tables
and Blackjack are exclusive: finish the current paid game before entering the other.
Casino entry debits the wallet once and clears only the old settled Blackjack
journal, retaining its history/statistics. Casino payout credits once. Practice
refill is unavailable during either active paid game. Finishing or forfeiting Dice
unlocks Blackjack again. Casino play uses the existing play-cash wallet and Core
prizes; Coins are reserved for cosmetics and packs.

The application ID and development signing configuration are unchanged.
ApplicationDisplayVersion is 0.5.0 and ApplicationVersion is 5. Install over the
existing 0.3/0.4 app without uninstalling to retain the local profile.

## Validation

New integration tests exercise old-save upgrade, exact dice/hold restoration,
casual and casino completion idempotency, all-seat AI recovery, failed entry and
final settlement rollback, forfeit, paid-game exclusion, simultaneous Blackjack/
casual/pack saves, Coin-only cosmetics, Dice challenge packs, detached checkpoints,
invalid saves and real checksummed file round-trips.

Verified CI: https://github.com/TeKatz/blackjack-casino-mobile/actions/runs/41424722457

- Tested app revision: `b9943ba133cb9176f5e6583917c75db2df728c46`.
- All three jobs succeeded. Desktop Release and Android builds: 0 warnings / 0 errors.
- 478 tests passed, 0 failed, 0 skipped, including 15 new Mobile Dice integration tests.
- The additional overlapping-score test verifies that repeated commands adopt exactly
  one complete AI turn. Mutations are serialized before checkpoint comparison and commit.
- Localization parity, original artwork and control-template checks passed.
- Android preparation decoded all 136 unique card images; signing identity and
  application ID remain unchanged from 0.3/0.4; the APK uses version 0.5.0 (5).
- Download: https://github.com/TeKatz/blackjack-casino-mobile/actions/runs/41424722457/artifacts/13131553315
- Artifact ZIP: 97,531,601 bytes (approximately 97.5 MB), expires 2026-11-16.
- ZIP includes `BlackjackEmpire-debug.apk`, its SHA-256 checksum, build revision
  and the artwork validation report. Upload completed and the artifact is not expired.

The authoring environment has no .NET or Android SDK; compilation and signing were
verified in GitHub Actions. No 0.5 device launch or runtime visual acceptance is claimed.

## Device acceptance

1. Update over 0.4/0.3 in place. Verify name, cash, Coins, Album, open packs and hand.
2. Start Casual, roll, hold two dice, force-stop and relaunch. Verify the same dice,
   holds, roll count and previews; reroll only unheld dice. Try the third-roll limit.
3. Record a zero with confirmation; finish all 13 boxes. Verify the bonus, total,
   XP, season, stats and challenge rewards. Reopen the result after restarting.
4. Enter the 100-play-cash casino table. Verify one fee debit, legal AI turns,
   standings and opponent sheets. Restart midway, then finish and verify payout,
   ties if applicable, net display and one reward/settlement only.
5. Try insufficient fees and start-game restrictions with a saved Blackjack hand.
   Forfeit a second table; check fee remains lost and completion rewards do not increase.
6. Equip an owned DiceSet or buy one with Coins. Verify the five dice and reduced
   animation setting after restart. Check portrait/landscape on the phone.

0.5 hardware acceptance remains pending until the user tests the APK.
