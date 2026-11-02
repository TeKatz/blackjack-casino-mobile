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
- Blackjack: felt/table ornament, original styled card faces/backs, dealer and player
  panels, fixed action/wager dock, finite card dealing and win particles.
- Slots: original cabinet skins and source symbols, clipped moving reel strips,
  staggered stops on the saved outcome, win glow/particles, and a fixed wager/spin
  dock. Landscape uses compact reels/dock. Decorative strips never draw outcomes.
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

Compilation, regression, screenshots and the APK link are recorded after verification.
App version: 0.7.0 (7). Application ID and development signing key are unchanged.
Install over the existing 0.3–0.6 app without uninstalling. No merge to main or
changes to the original desktop repository are included.
