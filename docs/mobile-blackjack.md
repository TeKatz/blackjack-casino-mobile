# Mobile milestone 2: first complete Blackjack table

Branch: `mobile/blackjack-round`, based on `development`.
The user confirmed installation, launch and navigation of the foundation APK on Android.

## Playable scope

Lobby -> wager -> genuine Core round -> result -> another round or lobby.

- Original `BlackjackRound`, `Shoe`, `GameRules` and settlement logic; Core is unchanged.
- Six decks, dealer stands on soft 17, natural Blackjack pays 3:2, late surrender.
- Hit, Stand, Double, Split and Insurance according to Core legality and available cash.
- $1,000 starting practice bankroll; whole-dollar opening wagers $1–$1,000.
  Half-dollar results are supported for surrender, insurance and Blackjack.
- Unsettled stakes are reserved, with extra stakes required for splitting, doubling and insurance.
  Core supplies net settlement; it is credited exactly once without subtracting stakes twice.
- Persistent shoe within the process, reshuffled only between rounds at the cut card or
  when the remaining reserve cannot guarantee enough cards for all allowed hands.
- Dealer hole card and total stay concealed until settlement, including accessibility descriptions.
- Native card visuals, horizontal card scrolling, active split-hand highlighting and per-hand results.
- Back to lobby and Android Back preserve the same open round within the current app process.
- Empty practice bankroll can be refilled only between rounds, when below the minimum wager.

The practice session is deliberately in memory. Backgrounding preserves it while the
process survives; Android process termination or an app restart resets practice cash
and the round. No profile storage, desktop save migration, XP, coins, statistics,
AI seats, audio or animation system is introduced in this milestone.

## Validation

`MobileBlackjackSessionTests` exercises real Core rounds with deterministic shoes:
net settlement, natural payouts, hidden dealer cards/totals, insufficient funds,
insurance win/loss, split plus double stakes, bust, surrender, rejected wagers,
refill guards and consecutive rounds without duplicate credits.
The pure session class is linked into the existing test project; running those tests
still requires no Android or MAUI workload. UI and APK compilation run in the Android job.

Build/test results and the new APK are recorded after the CI run completes.

## Device checklist

- Install version 0.2.0; uninstall the old debug preview first if Android reports a signature mismatch.
- Enter Blackjack, choose a chip or whole-dollar wager, and deal.
- Check the dealer hole card stays face-down and the displayed dealer total shows only the upcard.
- Hit/Stand through a complete round; verify result, bankroll and another deal.
- When offered, try Double, Split, Surrender and Insurance; verify active-hand highlights.
- Return to lobby during an active hand and resume; verify identical cards and reserved stakes.
- Rotate, background/resume, and check small-screen scrolling and card rows.
- Confirm the existing desktop app and saves are unaffected.

Next: persistent Android profile and lifecycle-safe round saving, then XP/coins/statistics
and presentation polish based on device feedback.
