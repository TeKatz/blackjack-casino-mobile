# Mobile 0.6.0: Casino Floor and Casino Vault

Branch: `mobile/casino-floor`, based on delivered 0.5.0 (`d9b93a7`). The user
reported 0.5 running without major errors and requested the next large integration.
No merge or writes to the original desktop repository are included.

## Playable integration

- Midnight Reels: original three-reel strips and single-payline returns.
- Neon Vault: original 5×4 all-ways machine, Blackouts, Wild expansion, jackpots,
  free spins, energy and Security Override.
- Nine Lives: original 3×3 / five-line machine, Pounce Wild, nine-spin bonuses,
  retriggers and Full Grid Frenzy.
- Dedicated paytables/features, remembered machine wagers, native vector symbols,
  saved result summaries and twenty recent spin receipts.
- Each manual tap plays one spin. Finite reel-column reveals occur after saving;
  a separate reduced-slot-animation preference disables them. No background spins
  or auto-starting saved configurations are introduced.
- Original per-machine statistics, XP, season progression and eligible challenges
  connect to the existing profile and Album pack reward pipeline.
- Casino Vault: original 2% eligible-wager contributions, five career tiers,
  full-capacity opening, tier bonus cash, card pack and retained opening receipt.

## Core reuse and durable bonuses

The existing payout, weighting and feature algorithms are reused unchanged.
NeonVaultSession and NineLivesSession gain detached, validated checkpoint APIs,
containing only the state needed between spins. Bankroll remains caller-owned.
Neon checkpoints retain free spins, trigger bet, energy and armed override; Nine
Lives checkpoints retain bonus spins and trigger bet. Reads never consume a spin.
Capture/restore adds no new scoring, payout or random-draw rules.

Optional MobileState.CasinoFloor and Settings.ReduceSlotAnimations extend the
existing schema-1 envelope. Each spin restores an engine from its checkpoint,
draws once, processes Core progression, updates the shared wallet and checkpoint,
and saves its immutable receipt, rewards and Vault contribution in one transaction.
Failed writes restore all of these, including remaining free spins. Showing a
receipt after a restart is read-only and never runs the engine or rewards again.
Spin command IDs are deduplicated within the twenty retained receipts; reusing an
ID for another machine/wager is rejected. UI busy guards prevent overlapping taps.

Slots/Vault cannot change cash during an active Blackjack hand or paid Dice table.
Settled Blackjack journals are cleared before another game's cash mutation; their
history/statistics remain. Pending slot bonuses block new Blackjack/paid Dice and
practice refills until completed. Bonus spins work at zero cash. Other slot
machines, Casual Dice and Album may be used while a bonus is paused; each machine's
bonus is retained independently. Nothing silently discards a paid bonus.

## Casino Vault upgrade and settlement

The first 0.6 load establishes ObservedEligibleWager at the existing career total.
Historical wagers retain career-tier eligibility but grant no retroactive Vault
cash. New eligible statistics synchronize inside accepted mobile transactions,
including completed Blackjack wagers, paid Dice entry fees and paid slot spins.
Free spins add no wager. Core controls capacities, overflow and next-cycle tiers.

PrepareOpen, play-cash credit, card pack, receipt and CompletePendingPayout save
in the same snapshot. A failed save restores the full Vault and original pack
inventory. Reopening a receipt never pays again. Unexpected pending two-store
receipts are rejected rather than guessing whether cash already reached a wallet.

Application ID and development signing key remain unchanged. Version is 0.6.0,
ApplicationVersion 6. Install over 0.3–0.5 without uninstalling.

## Validation

New tests cover old-profile migration with simultaneous Blackjack/Casual/pack
state, spin settlement/restart/idempotency, locked bonus wagers and retriggers,
zero-cash bonus continuation, failed paid/bonus/Vault writes, game exclusion,
separate statistics and challenge packs, bounded history, malformed saves,
real checksummed file round-trips and Core checkpoint feature equivalence.

Build, regression and artifact results will be recorded after CI completes.
The authoring environment has no configured .NET/Android SDK; CI is the compiler
and full regression authority. A successful build is not a device-launch test.

## Device acceptance

1. Update over 0.5; verify profile, Coins, cash, Album, pending pack and Dice game.
2. Play each machine with a small wager. Check grid readability in portrait and
   landscape, wager selection, net versus returned cash, paytables and history.
3. When a bonus triggers, force-stop and reopen. Verify remaining spins, trigger
   wager and Neon energy/override; continue without another paid entry or reroll.
4. Leave during a column reveal, then return. Confirm one saved result, cash and XP.
5. Check game locks with a saved Blackjack hand or paid Dice table, and bonus locks
   before starting another paid table or refilling empty play cash.
6. Verify Vault gains only from new paid wagers, full-Vault confirmation, cash,
   bonus, one Album pack and one opening after restarting (when naturally available).
7. Claim a newly completed machine challenge, open its pack, and verify no duplicate
   reward after restart. Check reduced slot animation setting and existing games.

0.6 real-device acceptance is pending until the user tests the new APK.
