# Android foundation / independent repository

## Baseline and scope

Source: https://github.com/TeKatz/blackjack-casino
Pinned main commit: `ddbb708c01405637b4f19fd15365813c31133b06`
Source title: Merge Casino Empire command center rework (#25).

The original handoff's same-repository requirement was superseded by the user's choice
of a separate `TeKatz/blackjack-casino-mobile` repository. No writes to the desktop
repository are part of this work. A one-time import merge preserves source ancestry,
assets, projects and tests. Existing desktop release/updater workflows are deliberately
not activated in this repository. They remain accessible in the inherited history.
The original source README is retained as `docs/desktop-original-readme.md`.

## Analysis and decision

- .NET 10 already underpins the existing solution.
- Core already owns cards, hand evaluation, shoe, round rules, game engines and progression.
  No duplicate blackjack engine or broad extraction is required.
- The Core C# scan found no `System.Windows`, `Microsoft.Win32` or native imports.
- Core's build does invoke Windows PowerShell to generate the Sudoku catalog. This is
  a build-time platform coupling; Windows CI preserves its behavior in phase one.
- WPF presentation, navigation, sounds and persistence adapters remain desktop projects.
- The existing test project links some desktop persistence/audio source directly; it is
  retained as the regression suite rather than misrepresented as a pure Core-only suite.

.NET MAUI was selected for an independent Android UI using the existing C# Core and
first-party Android packaging. Avalonia could also host Core, but sharing or replacing
WPF UI brings no advantage for this deliberately separate mobile client.
Reference: https://learn.microsoft.com/dotnet/maui/android/deployment/publish-cli?view=net-maui-10.0

## Boundaries

Mobile -> Core only. No Mobile dependency on Desktop, Updater or Update.
Desktop -> Core as before; its solution excludes Android.

A separate Mobile solution contains Core and Mobile. No inherited Desktop or Core
source is refactored. Central packages add only the MAUI Controls dependency.
The shell uses a scrollable, width-limited layout, large buttons, normal navigation
with Android Back, and a fixed Ace/King example evaluated by the inherited Core.
No permanent animation loop, background work, network access, audio or save writing.
Portrait/landscape are not locked. Android minimum version is API 26 (Android 8.0).

## CI and delivery

`mobile.yml`: prepare/import exact source -> original desktop build and regression
suite on a runner without Android workloads -> MAUI Android build on a separate runner
-> signed Debug APK, SHA-256 and build-revision artifact.

Only preparation has contents-write permission for the one-time source import. All
validation/build jobs have read-only repository permissions. No release, updater
manifest publication, cloud sync or Play Store deployment is configured.
`BlackjackEmpire-debug-apk` contains `BlackjackEmpire-debug.apk` and provenance.
Documentation-only changes do not rebuild the app.

## Validation boundaries and acceptance

The authoring environment has no .NET SDK or Android SDK. Compilation and the inherited
regression suite must be verified in GitHub Actions; a configured workflow alone is not
proof of a successful build. A build success alone is not proof of app launch.

Hardware checklist before milestone 2:
- install the signed APK on an Android device;
- launch from its icon; verify readable lobby in portrait and landscape;
- tap CHECK THE TABLE; verify Ace/King and total 21;
- return using both the button and Android Back;
- background/resume and relaunch without errors;
- confirm the original desktop installation and its save data remain unaffected.

No new persistence or migration is introduced. Desktop save source and formats remain
inherited unchanged, but no claim of Android save compatibility is made.
Windows presentation/runtime smoke checks from the original repo are not all repeated
by this initial pipeline; the original repo's check status is independent.

## Next milestone

After the real-device checklist passes: lobby -> genuine Core round -> actions ->
settlement -> lobby. Then add local Android profile storage, coins/XP/statistics and
lifecycle-safe saving. Do not port all casino systems in the foundation milestone.

## Import provenance

Import commit: `f4ebadd7df99e5d9b45fd2f2536c6ca70cd54702`.
Its parents retain both the mobile initialization and the pinned desktop history.
`main` and `development` were created at that imported foundation.

## Verified foundation checks

Run: https://github.com/TeKatz/blackjack-casino-mobile/actions/runs/41386429178

- Original desktop solution: Release build successful, 0 warnings / 0 errors.
- Existing regression suite: 428 passed, 0 failed, 0 skipped.
- Localization parity, 136-card artwork validation and control-template contract: passed.
- Android build and signing: successful; `BlackjackEmpire-debug-apk` uploaded.
- Download: https://github.com/TeKatz/blackjack-casino-mobile/actions/runs/41386429178/artifacts/13115926963
- Artifact ZIP: approximately 83.4 MB, available until 2026-11-16.
- APK compiled from imported revision `f4ebadd7df99e5d9b45fd2f2536c6ca70cd54702`.
- Real-device installation, launch and navigation remain unverified; run the hardware checklist.
- Subsequent documentation/import-script corrections do not change the compiled app.
- Bootstrap archive exclusions were narrowed to root files, with shell syntax and archive-content checks passing.
