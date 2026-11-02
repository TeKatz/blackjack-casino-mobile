# Mobile 0.7.0: Midnight Club presentation

The user confirmed the implemented systems work, but rejected the placeholder
visual quality of all 0.1–0.6 screens. This branch addresses presentation rather
than adding another game. Baseline: mobile/casino-floor at 385717a.

## Original artwork and visual identity

- Actual embedded Neon Vault JPEG symbols are decoded byte-for-byte from the
  original Desktop sources, using the same enum-to-art mapping as WPF.
- Actual Nine Lives cat, bell, fishbone, Wild and Bonus images are decoded from the
  original sources. Yarn comes from the packaged yarn.jpg used by Desktop.
- Midnight Reels drawings are converted from the original SlotTextures.xaml paths,
  gradients, pens and fill rules into SVG, then Android PNG resources in MAUI.
- Lobby emblems are the five existing Navigation PNGs. Stock portraits use the
  existing original images decoded at a mobile size. House Guest has a native badge.
- Nine Lives' rooftop/moon stage follows NineLivesLobbyArtwork. The inherited old
  Background0..6 encoded JPEG is malformed; no missing bytes are invented, and the
  valid original symbol images are never replaced with fallback icons.
- Original forest/cream/brass club colors, plum/gold Reels cabinet, cyan/violet Neon
  skin, seven exact Dice palettes and all six PlayingCardPresentation palettes.

prepare_presentation_art.py validates the embedded image decodes, converts vector
paths and reports asset hashes/provenance. Generated resources are ignored and
rebuilt before MAUI project evaluation. No runtime atlas parsing or image generation.
The unused primitive SlotSymbolArt implementation is removed.

## Mobile layouts and feedback

- Lobby: illustrated game tiles, player wallet, XP rail, original emblems and bottom
  navigation replace the full-width menu-button list.
- Blackjack: felt/table ornament, native card faces/backs in the six original
  palettes, dealer and player
  panels, fixed action/wager dock, finite card dealing and win particles.
- Slots: original cabinet skins and source symbols, clipped moving reel strips,
  staggered stops on the saved outcome, win glow/particles, and a fixed wager/spin
  dock. Landscape uses compact reels and a side panel for wager/spin, rebuilt only
  after native size measurement completes. Decorative strips never draw outcomes.
- Midnight Dice: persistent dice tray/roll dock, original gradient dice with held
  glow, tumbling/bounce feedback, compact score sheet and opponent tabs.
- Album: artwork-led theme covers, pack covers, rarity frames, finite foil sheen,
  card flips and pack reveal particles with fixed reveal controls.
- Profile, shop, challenges, season, settings and Vault share the club palette,
  original emblems, gradients, progress/reward components and typography. Owned
  card sets can be equipped and previewed; wallet/ownership changes remain atomic.
- Vault includes a progress dial and finite opening feedback.

Animations are finite and cosmetic. Save/settlement stays ahead of presentation.
Reduce Interface Motion disables decorative motion; existing per-game reduced
settings are respected by game animations. Pending hands, bonuses, pack receipts,
wallets, statistics and original reward formulas remain under the established Core.
The optional ReduceInterfaceMotion field defaults false for older JSON.

## Validation and delivery

CI retains the full desktop/Core regression suite and signed Android build. A new
Android emulator job installs the built APK and exercises the actual native UI:
lobby, dealt Blackjack hand, all three machines, portrait/landscape Neon, Dice,
Album, pack reveal, profile and Vault. It saves screenshots, reel-motion MP4s and
an inspection contact sheet. A unit build alone is not visual acceptance.

The separate Android visual regression workflow supports test-driver iteration
without rebuilding the application. tools/mobile/visual-smoke-source.json pins an
existing APK run and its exact source revision; the downloaded build-revision.txt
must match before installation. The driver resolves the installed launcher, retries
transient Android UI snapshot failures, waits for enabled action controls and records
real reel motion. Screenshots must be viewed, including landscape and dealt cards;
the screenshot count alone does not establish visual acceptance.

Verified source: e7aa5dbbb34505ea4763a02e5a7c6711bb2272c6.
Later branch changes are test-driver/documentation changes only.

- Full 500/500 regression suite passed; Desktop and Android builds each report
  zero warnings and zero errors in [run 41449883800](https://github.com/TeKatz/blackjack-casino-mobile/actions/runs/41449883800).
- The integrated native UI job passed. A second run with the hardened driver also
  passed against that exact APK: [visual regression 41451905530](https://github.com/TeKatz/blackjack-casino-mobile/actions/runs/41451905530).
- All 13 native screenshots were viewed, with full-size inspection of landscape
  controls, dealt/settled card faces and rolled Dice. The initial landscape
  measurement/reparenting defect was fixed, and text-presentation suit glyphs
  prevent Android's color emoji fallback from overriding card-set ink colors.
- All three reel recordings are retained. Sampled native frames confirm moving
  clipped strips followed by the receipt's final symbols; no outcome RNG is used
  by the motion. The harness confirms Blackjack settlement before machine entry.
- Original preparation validates all 36 presentation assets plus the existing
  136 Album card resources. Raw Neon/Cat/Navigation bitmap hashes are recorded.
- [Signed APK and checksum](https://github.com/TeKatz/blackjack-casino-mobile/actions/runs/41449883800/artifacts/13145335210)
  and [screenshots/MP4s/report](https://github.com/TeKatz/blackjack-casino-mobile/actions/runs/41451905530/artifacts/13146055591).
  Artifacts expire 2026-11-16. APK SHA256:
  `19d356b75b44bdb0b53209afdb286e419ad40f7d77dff197de47834846b367a7`.
- Development signing certificate SHA256 remains
  `125ac55178c8de855f9ca263bf002d463e0b904c92a3ae6d07f48221c5a24971`.

Emulator visual inspection is complete. No physical-phone appearance, animation
frame pacing or in-place upgrade test of 0.7 is claimed. The user confirmed the
existing 0.6 feature set; 0.7 device acceptance remains pending.
App version: 0.7.0 (7). Application ID and development signing key are unchanged.
Install over the existing 0.3–0.6 app without uninstalling. No merge to main or
changes to the original desktop repository are included.
