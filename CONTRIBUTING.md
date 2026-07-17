# Contributing to ChipGauge

Thanks for your interest in ChipGauge! This guide covers the local build loop,
the coding standards CI enforces, and how the codebase is organized.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) (LTS)
- Optional, for regenerating icons: Python 3 with `cairosvg` and `Pillow`

## Build loop

```bash
dotnet restore ChipGauge.slnx
dotnet build   ChipGauge.slnx -c Release   # warnings are errors
dotnet test    ChipGauge.slnx -c Release
dotnet format  ChipGauge.slnx --verify-no-changes
```

The same four commands run in CI on Linux and Windows. If they pass locally, the
pipeline should be green.

## How the code is organized

ChipGauge is strictly layered so hardware access never leaks into the UI and
platform code never leaks into the core:

- **`ChipGauge.Core`** — platform-free domain: models, the `MonitoringEngine`,
  ring-buffer history, alerts, S.M.A.R.T. health, report export. Depends on
  nothing but the BCL. All non-trivial logic lives here and is unit-tested.
- **`ChipGauge.Hardware.*`** — providers implementing `ISensorProvider` /
  `IHardwareInfoProvider`. `Mock` is deterministic and used by tests, demos and
  CI. `Windows` (Phase 1) wraps LibreHardwareMonitor.
- **`ChipGauge.App`** (Phase 1) — the Avalonia UI. View-models talk to the engine,
  never to hardware directly.

**Rule of thumb:** put logic in `Core` behind an interface and test it there;
keep providers as thin adapters and the UI as a thin presentation layer.

## Coding standards

- Follow the repo `.editorconfig` (file-scoped namespaces, `_camelCase` private
  fields, ordered usings). `dotnet format` fixes most of it automatically.
- Warnings are errors — keep the build clean.
- Every bug fix and new behavior lands with a test. Aim to keep `Core` coverage high.
- No user-facing string literals in code (Phase 1+): they belong in resource files
  so both English and German stay in sync.

## Commit & PR

- Keep commits focused and messages descriptive.
- Ensure the four build-loop commands pass before opening a PR.
- Describe what changed and why; link any related issue.

## Cutting a release (maintainers)

Releases are automated by [`.github/workflows/release.yml`](.github/workflows/release.yml),
which builds the self-contained Windows `.exe` and publishes a GitHub Release.
Two equivalent ways to trigger it:

- **Auto-tag:** Actions → **Release** → *Run workflow*, enter a version (e.g.
  `1.0.0`). It runs the tests, creates and pushes tag `v1.0.0`, then publishes.
- **Tag push:** `git tag v1.0.0 && git push origin v1.0.0`.

The workflow runs on Linux (cheap) and cross-publishes `win-x64` + `win-arm64`.
Binaries are unsigned until a code-signing certificate is in place.

## Branding assets

SVG masters live in `assets/`. If you change one, regenerate the rasters and
commit the results:

```bash
python3 build/generate-icons.py
```
