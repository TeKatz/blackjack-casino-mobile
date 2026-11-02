# Blackjack Empire Mobile

This repository is the independent Android workspace. Do not push changes to
TeKatz/blackjack-casino or publish desktop update manifests from this repository.

Read docs/mobile-foundation.md for the pinned desktop origin and platform boundaries.
Use development and short mobile/* branches for subsequent feature work.
Keep WPF as the inherited reference client; Android UI belongs in BlackjackCasino.Mobile.
Reuse BlackjackCasino.Core; do not introduce a second blackjack engine.
Build Mobile explicitly using BlackjackCasino.Mobile.slnx or its csproj.
The inherited BlackjackCasino.sln is desktop-only and must not require Android tooling.
Core currently generates Sudoku resources through Windows PowerShell; initial CI uses Windows.
Run the existing regression suite when changing Core or persistence.
Do not claim a successful build, APK installation, or hardware launch without evidence.
No desktop save migration or cloud sync. Mobile 0.3 has independent persistent state;
read docs/mobile-profile-progression.md before changing persistence or progression.
Keep the application ID and PUBLIC development signing key stable and increase
ApplicationVersion for updates. That key must never sign production releases.
Preserve journal/wallet/reward atomicity and test recovery and duplicate-reward behavior.
