# DailyReflection

MAUI / Uno / Avalonia ports of [kazo0/DailyReflection](https://github.com/kazo0/DailyReflection).

The original Xamarin.Forms app is checked in as a git submodule at `heads/DailyReflection.Xamarin/` for read-only reference. Run `git submodule update --init` after cloning to populate it.

## Layout

* `DailyReflection.Core/` — constants, extensions (StripHtml, the inline-markup parser used by Uno's reflection page).
* `DailyReflection.Data/` — SQLite database, the `Reflection` model, embedded `dailyreflections.db`.
* `DailyReflection.Services/` — service interfaces (`ISettingsService`, `IShareService`, `INotificationService`, `IVersionTrackingService`), the daily-reflection service, the version-gated `StartupMigrationRunner`.
* `DailyReflection.Presentation/` — view models, messages, MVVM Toolkit plumbing.
* `heads/DailyReflection.Maui/` — MAUI head (Android, iOS, macCatalyst).
* `heads/DailyReflection.Uno/` — Uno head (Android, iOS, Skia desktop on Windows / macOS / Linux).
* `heads/DailyReflection.Avalonia/` — Avalonia head (Desktop / Android / iOS / Browser).
* `heads/DailyReflection.Xamarin/` — Xamarin original (submodule, read-only).
* `docs/ANALYSIS.md` — deep gap analysis between the Xamarin original and the Uno port.
* `docs/ios-launch-experience.md` — iOS-specific design notes (minimum OS, splash, bundle keys).
* `specs/` — implementation specs that closed the gaps in `docs/ANALYSIS.md` §10.

## Bundle / package identity

| Head | ApplicationId / Bundle id |
|---|---|
| Xamarin original | `com.kazo0.dailyreflection` |
| MAUI port | `com.kazo0.dailyreflection-maui` |
| Uno port | `com.kazo0.dailyreflectionuno` |

The Uno port currently uses a distinct identifier so it can be installed alongside the original Xamarin app during pilot. **To replace the original on the App / Play Store**, change `ApplicationId` in `heads/DailyReflection.Uno/DailyReflection.Uno/DailyReflection.Uno.csproj` to `com.kazo0.dailyreflection` *and* coordinate with the existing app's release notes — App / Play Store associate ratings, IAP, and update flows with the bundle id.

## Build

```bash
# Shared libraries + tests (works in any .NET 10 environment)
dotnet build DailyReflection.Core/DailyReflection.Core.csproj
dotnet test  DailyReflection.Services.Tests
dotnet test  DailyReflection.Presentation.Tests

# Uno desktop (works without mobile workloads)
dotnet build heads/DailyReflection.Uno/DailyReflection.Uno/DailyReflection.Uno.csproj -f net10.0-desktop

# Uno mobile (requires android/ios workloads + SDKs)
dotnet build heads/DailyReflection.Uno/DailyReflection.Uno/DailyReflection.Uno.csproj -f net10.0-android
dotnet build heads/DailyReflection.Uno/DailyReflection.Uno/DailyReflection.Uno.csproj -f net10.0-ios

# MAUI build (requires the maui-android / maui-ios workloads)
dotnet build heads/DailyReflection.Maui/DailyReflection.Maui.csproj
```

The shared `DailyReflection-maui.slnf` and `DailyReflection-uno.slnf` solution filters group the relevant projects per head.
