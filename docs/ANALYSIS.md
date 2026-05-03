# DailyReflection — Migration Analysis

> Origin: [kazo0/DailyReflection](https://github.com/kazo0/DailyReflection) (Xamarin.Forms).
> This repo houses .NET MAUI and Uno Platform ports of that app, with a fourth Avalonia head (excluded from this report).
> The original Xamarin source is checked in as a git submodule at `heads/DailyReflection.Xamarin/` and is read‑only / unbuilt.

This document inventories the original Xamarin.Forms app, then walks through the two migrations on this branch (MAUI and Uno Platform), and finally enumerates the API‑level deltas, gaps, and new behaviour each port introduced. The Avalonia head is intentionally out of scope.

---

## 1. Executive Summary

| Concern | Xamarin.Forms (original) | MAUI port | Uno Platform port |
|---|---|---|---|
| Shared layer location | `heads/.../DailyReflection.{Core,Data,Services,Presentation}` (submodule, netstandard2.0) | Repo root (`/DailyReflection.{Core,Data,Services,Presentation}`, **net10.0**) | Same shared layer as MAUI |
| MVVM toolkit | `Microsoft.Toolkit.Mvvm` 7.0.0‑preview4 | `CommunityToolkit.Mvvm` 8.4.0 | `CommunityToolkit.Mvvm` 8.4.0 |
| Date math | NodaTime 3.0.3 | NodaTime 3.0.3 | NodaTime 3.0.3 |
| Database | `sqlite-net-pcl` 1.7.335 | `sqlite-net-pcl` 1.10.196‑beta | `sqlite-net-pcl` 1.10.196‑beta + `SQLitePCLRaw.bundle_e_sqlite3` 2.1.10 |
| UI framework | Xamarin.Forms 4.8 + Material Visual | Microsoft.Maui.Controls (net10.0) | WinUI 3 / Uno 6.x (Skia renderer) |
| Navigation | Shell + `<TabBar>` | Shell + `<TabBar>` | `Uno.Toolkit.UI.TabBar` + Uno.Extensions region routing |
| Preferences | `Xamarin.Essentials.IPreferences` (DI‑injected interface) | `Microsoft.Maui.Storage.Preferences.Default` | `Windows.Storage.ApplicationData.LocalSettings` + Android `SharedPreferences` mirror |
| Share | `Xamarin.Essentials.IShare` (DI‑injected interface) | `Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default` | `Windows.ApplicationModel.DataTransfer.DataTransferManager` |
| Notifications | Per‑platform: AlarmManager (Android) / `UNUserNotificationCenter` (iOS) | Same per‑platform APIs, but partial classes co‑located in the head | Same per‑platform APIs; **desktop is a no‑op** |
| Picker styling | `CustomDatePickerRenderer` / `CustomTimePickerRenderer` (custom renderers) | `DatePickerHandler.Mapper.AppendToMapping` in `App.xaml.cs` (handlers) | Native `CalendarDatePicker` / `TimePicker` flyouts (no customisation needed) |
| Target frameworks (head) | netstandard2.0 + `MonoAndroid` + `Xamarin.iOS` | `net10.0-android;net10.0-ios;net10.0-maccatalyst` | `net10.0-android;net10.0-ios;net10.0-desktop` |
| Test projects | `*.Tests` (netstandard2.0, NUnit + Moq + Xamarin.Essentials.Interfaces) | `*.Tests` (**net9.0**, NUnit + Moq) | Same shared test projects (used via Presentation reference) |

Both ports preserve the same three‑tab UX (Daily Reflection / Sober Time / Settings), the same view models and messages, and the same two persistence concerns (read‑only embedded SQLite database + key/value preferences). What differs is the platform glue — controls, navigation host, preference store, share API, and how custom picker chrome is achieved.

---

## 2. Original Xamarin.Forms app — feature inventory

### 2.1 Solution shape

`heads/DailyReflection.Xamarin/DailyReflection.sln` contains four shared netstandard2.0 libraries and three platform / test heads:

```
DailyReflection.Core           // Constants, ServiceCollectionExtensions, StringExtensions
DailyReflection.Data           // Reflection model, embedded dailyreflections.db, IDailyReflectionDatabase
DailyReflection.Services       // IDailyReflectionService, ISettingsService, IShareService, INotificationService
DailyReflection.Presentation   // ViewModelBase + 3 ViewModels, 3 Messages, ReflectionExtensions

DailyReflection                 // Xamarin.Forms shared (XAML pages, App.xaml, Startup.cs)
DailyReflection.Android         // MonoAndroid head
DailyReflection.iOS             // Xamarin.iOS head
DailyReflection.UITests
DailyReflection.Presentation.Tests
DailyReflection.Services.Tests
```

### 2.2 NuGet footprint of note

* `Xamarin.Forms` 4.8.0.1821 + `Xamarin.Forms.Visual.Material` (Material Design on Android via shell `Visual="Material"`).
* `Xamarin.Essentials` 1.7.5 + `Xamarin.Essentials.Interfaces` 1.7.4 — used through their interfaces (`IPreferences`, `IShare`) registered in DI rather than via the static `Preferences`/`Share` types. This is what made the service layer testable without a platform.
* `Xamarin.CommunityToolkit` 1.0.0‑pre5 — supplies `IntToBoolConverter`, `IsNotNullOrEmptyConverter`, `EventToCommandBehavior`.
* `Microsoft.Toolkit.Mvvm` 7.0.0‑preview4 — `ObservableRecipient`, `RelayCommand`, `WeakReferenceMessenger`.
* `Microsoft.Extensions.Hosting` 5.0 — full generic host inside a Xamarin.Forms app for DI + configuration.
* `NodaTime` 3.0.3 for sober‑period arithmetic.
* `sqlite-net-pcl` 1.7.335 for the embedded database.

### 2.3 Pages, controls, and navigation

`heads/DailyReflection.Xamarin/DailyReflection/DailyReflection/Views/`:

* **`AppShell.xaml`** — `Shell` with `<TabBar>` containing three `<Tab>` entries (Reflection, Sober Time, Settings). `Visual="Material"` for native Material on Android. Per‑platform tab‑bar colours via `OnPlatform`.
* **`DailyReflectionView.xaml`** — `ContentPage` with `ScrollView` + `StackLayout`, `ActivityIndicator`, hidden `CustomDatePicker` driven via `EventToCommandBehavior` + `DateChangedEventArgsConverter`. `Label TextType="Html"` renders the `<i>…</i>` markup that lives in the database. Toolbar items: calendar (focuses the hidden picker) and share (`ShareCommand`).
* **`SobrietyTimeView.xaml`** — `ScrollView` + `Grid` showing either *Years/Months/Days* or *Days only* depending on `SoberTimeDisplayPreference`. Pluralisation via `PluralityConverter`. Calendar glyph via `FontImage`.
* **`SettingsView.xaml`** — `TableView` (`Intent="Settings"`) with `SwitchCell` + `TextCell` rows. Hidden `CustomDatePicker` / `CustomTimePicker` / `Picker` are focused programmatically when the user taps the corresponding cell. Registers `NotificationPermissionRequestMessage` in code‑behind to surface a permission alert.

Custom controls: `CustomDatePicker` / `CustomTimePicker` are empty subclasses purely so platform renderers can target them without affecting every other Xamarin.Forms picker.

### 2.4 Cross‑cutting application code

* **`App.xaml.cs`** wires `MainPage = ServiceProvider.GetService<AppShell>()`, calls `VersionTracking.Track()` (Xamarin.Essentials), and on `OnStart` runs two version‑gated migrations: `MigrateSettingsIfNeeded` (build < 20 → re‑seat preferences via `ISettingsService.MigrateOldPreferences`) and `RefreshDatabaseIfNeeded` (build < 32 → `IDailyReflectionDatabase.RefreshDatabaseFile()`).
* **`Startup.cs`** uses `Host.CreateDefaultBuilder()`, registers `appsettings.json` via `EmbeddedFileProvider`, calls `services.AddPages()` (auto‑registers all `Page` subclasses via `AddAllSubclassesOf<Page>`), then `AddPresentationDependencies()` which walks down through Services and Data.
* **DI chain** — `Presentation.AddPresentationDependencies` ⇒ `AddAllSubclassesOf<ViewModelBase>` (Singleton) ⇒ `Services.AddServiceDependencies` ⇒ `Data.AddDataDependencies`. `Services.AddServiceDependencies` also registers `Xamarin.Essentials.ShareImplementation` and `PreferencesImplementation` so the `ISettingsService` / `IShareService` wrappers can take `IPreferences` / `IShare` constructor parameters.

### 2.5 Platform services

| Service | Android implementation | iOS implementation |
|---|---|---|
| `INotificationService` | `Services/NotificationService.cs` using `AlarmManager` + `PendingIntent` + `BroadcastReceivers/DailyNotificationReceiver` (which re‑arms the next day's alarm and posts the `NotificationCompat.Builder` notification). Permissions via `Xamarin.Essentials.Permissions` (custom `NotificationPermission : BasePlatformPermission` for Android 13+ `POST_NOTIFICATIONS`). `WakeUpAlarmReceiver` listens for `BOOT_COMPLETED` and `ScheduleExactAlarmPermissionStateChanged` to re‑arm. | `Services/NotificationService.cs` using `UNUserNotificationCenter` + `UNCalendarNotificationTrigger` (repeating). `ShowNotificationSettings` opens `UIApplication.OpenNotificationSettingsUrl`. |
| `ISettingsService` | Cross‑platform; injects `Xamarin.Essentials.IPreferences` and switches over `T` for `bool/int/double/float/long/string/DateTime`. | Same. |
| `IShareService` | Cross‑platform; injects `Xamarin.Essentials.IShare` and calls `RequestAsync(new ShareTextRequest(...))`. | Same. |

Renderers customise picker chrome:

* `CustomDatePickerRenderer` (Android) clears the underline drawable; (iOS) zeroes border, sets `UIDatePickerStyle.Inline` on iOS 13.4+.
* `CustomTimePickerRenderer` (Android) clears the underline; (iOS) zeroes border, forces `UIDatePickerStyle.Wheels` to keep the wheel UX.

### 2.6 Manifests / capabilities

`AndroidManifest.xml`: package `com.kazo0.dailyreflection`, version 3.4 (34), `minSdk 21`, `targetSdk 33`, permissions `ACCESS_NETWORK_STATE`, `INTERNET`, `VIBRATE`, `RECEIVE_BOOT_COMPLETED`, `POST_NOTIFICATIONS`.

`Info.plist`: bundle id `com.kazo0.dailyreflection`, version 3.4 (34), iPhone+iPad, min iOS 9.0, FontAwesome OTFs registered, `LaunchScreen.storyboard`.

### 2.7 Tests

`DailyReflection.Presentation.Tests` and `DailyReflection.Services.Tests` cover ViewModel behaviour and service plumbing with NUnit + Moq + `Xamarin.Essentials.Interfaces` (the testable IPreferences/IShare facades). Both projects target netstandard2.0.

---

## 3. Shared‑library evolution

The MAUI and Uno ports share **one** copy of Core/Data/Services/Presentation at the repo root. Comparing it against the Xamarin original:

| Layer | Xamarin (netstandard2.0) | This repo (net10.0) |
|---|---|---|
| `Core` | `ServiceCollectionExtensions.AddAllSubclassesOf<T>`, `StringExtensions.StripHtml`, constants. | Identical surface. Now `net10.0`; uses `System.Web.HttpUtility` via the modern BCL. |
| `Data` | `Reflection`, `IDailyReflectionDatabase`, embedded `dailyreflections.db`. | Identical. `sqlite-net-pcl` upgraded to 1.10.196‑beta. |
| `Services` | `IDailyReflectionService` + impl, `ISettingsService` + cross‑platform impl injecting `IPreferences`, `IShareService` + cross‑platform impl injecting `IShare`, `INotificationService` (interface only), `AddServiceDependencies` registers all of the above plus `Xamarin.Essentials.{Preferences,Share}Implementation`. | Same interfaces; **the cross‑platform `SettingsService` and `ShareService` implementations move out of `Services` and into the per‑head `PlatformServices/` folder.** `Services.AddServiceDependencies` now only registers `IDailyReflectionService` + the data layer. |
| `Presentation` | 3 ViewModels + base, 3 messages, `ReflectionExtensions`, `AddPresentationDependencies`. | Identical surface. `Microsoft.Toolkit.Mvvm` 7.0‑preview4 → `CommunityToolkit.Mvvm` 8.4.0 (renamed namespace, source‑generator MVVM). |

A grep across the four shared projects for `#if WINDOWS / __ANDROID__ / __IOS__ / __WASM__ / HAS_UNO` returns zero hits — the shared layer stayed truly platform‑agnostic. All platform variation lives in the head.

The ViewModel API (`DailyReflectionViewModel`, `SettingsViewModel`, `SobrietyTimeViewModel`) is unchanged: same `[ObservableProperty]` properties, same commands, same messages (`SoberDateChangedMessage`, `SoberTimeDisplayPreferenceChangedMessage`, `NotificationPermissionRequestMessage`).

> **Note on tests.** The two `*.Tests` projects target `net9.0` while the shared libraries are `net10.0`; per `CLAUDE.md` they are intentionally not part of the main solution and must be built / run separately (`dotnet test DailyReflection.Presentation.Tests`).

---

## 4. MAUI port (`heads/DailyReflection.Maui/`)

### 4.1 Project shape

* `net10.0-android;net10.0-ios;net10.0-maccatalyst`. Single project. `ApplicationId = com.kazo0.dailyreflection-maui`, version 1.0.
* Packages: `Microsoft.Maui.Controls` (via `MauiVersion`), `CommunityToolkit.Maui` 12.2.0, `Microsoft.Extensions.Configuration.{Json,Binder}` 10.0.0‑rc.1.
* Assets: `Resources/AppIcon/appicon.svg`, `Resources/Splash/splash.svg`, `Resources/Fonts/{OpenSans-*.ttf, FontAwesome*.otf}`, `Resources/Images/notif_icon.png`. `appsettings.json` is embedded.

### 4.2 App composition

* `MauiProgram.cs` builds the `MauiApp` with `UseMauiApp<App>()` + `UseMauiCommunityToolkit()`, registers fonts (`OpenSansRegular`, `OpenSansSemibold`, `FaBrandsFont`, `FaRegularFont`, `FaSolidFont`), embeds `appsettings.json` via `Assembly.GetManifestResourceStream(...)` + `ConfigurationBuilder().AddJsonStream(...)`, and exposes the resulting `MauiApp` and `IServiceProvider` as static properties on `MauiProgram`.
* DI: `services.AddPages()` (auto‑register `Page` subclasses, Singleton) → `services.AddPlatformServices()` (registers MAUI versions of `ISettingsService`, `IShareService`, `INotificationService`) → `services.AddPresentationDependencies()` (shared chain).
* `App.xaml.cs` overrides `CreateWindow` to return `new Window(serviceProvider.GetService<AppShell>())`, calls `VersionTracking.Track()`, and on `OnStart` runs the same `MigrateSettingsIfNeeded` / `RefreshDatabaseIfNeeded` logic as the original. It also customises picker handlers via `Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping(...)` / `TimePickerHandler.Mapper...` — eliminating the renderer subclasses.

### 4.3 UI APIs (vs. Xamarin)

| Concern | Xamarin | MAUI |
|---|---|---|
| Page base class | `ContentPage` | `ContentPage` (same XAML; just `Microsoft.Maui.Controls` namespace). |
| Navigation root | `Shell` + `<TabBar>` + `<Tab>` (`Visual="Material"`). | `Shell` + `<TabBar>` + `<Tab>` (`Visual="Material"`) — virtually unchanged. |
| Toolbar | `<ContentPage.ToolbarItems>` + `ToolbarItem` (Primary/Secondary). | Identical. |
| Settings rows | `TableView`/`SwitchCell`/`TextCell`. | **Identical.** MAUI still ships these, so `SettingsView.xaml` is reused with only namespace changes. |
| Hidden pickers | `CustomDatePicker`/`CustomTimePicker` subclasses + custom renderers. | Same subclasses (`Controls/CustomDatePicker.cs` empty) but **custom renderers replaced with handler `Mapper.AppendToMapping`** in `App.xaml.cs` (iOS: `BorderStyle = None`, inline date style; Android: clear background). |
| Behaviors | `xct:EventToCommandBehavior` (Xamarin Community Toolkit). | `mct:EventToCommandBehavior` (MAUI Community Toolkit, namespace flipped to `CommunityToolkit.Maui`). |
| HTML rendering | `Label TextType="Html"`. | Identical. |
| Converters | 9 converters in the head. | Identical 9 converters; same logic. |
| Styling | App.xaml resource dictionary with FontImage glyph factories, primary blue `#1976D2`. | Same dictionary, but split into `Resources/Styles/Colors.xaml` + `Resources/Styles/Styles.xaml`. Adds the standard MAUI template's neutral palette (Primary/Secondary/Tertiary/Gray100‑950) on top of the original blue, plus `Headline`/`SubHeadline` text styles. |

### 4.4 Non‑UI APIs (vs. Xamarin)

| Concern | Xamarin (Essentials) | MAUI head |
|---|---|---|
| Preferences | `IPreferences.Get<T>/Set<T>` (DI‑injected `PreferencesImplementation`). Cross‑platform impl in **`Services` library**. | **`Microsoft.Maui.Storage.Preferences.Default.Get/Set`** called directly from `PlatformServices/SettingsService.cs` in the head. No more `IPreferences` indirection in DI. |
| Share | `IShare.RequestAsync(new ShareTextRequest(body, title))`. | `Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(new ShareTextRequest(text, title))` from `PlatformServices/ShareService.cs`. |
| Version tracking | `Xamarin.Essentials.VersionTracking.Track()`. | `Microsoft.Maui.ApplicationModel.VersionTracking.Track()`. |
| Notifications (iOS) | `UNUserNotificationCenter` in `DailyReflection.iOS/Services/NotificationService.cs`. | Same APIs (`UNUserNotificationCenter`, `UNMutableNotificationContent`, `UNCalendarNotificationTrigger`) but now compiled into the single project under `PlatformServices/NotificationService.iOS.cs`. |
| Notifications (Android) | `AlarmManager.Set/SetAndAllowWhileIdle` + `PendingIntent.GetBroadcast` + `DailyNotificationReceiver` + `WakeUpAlarmReceiver`. | Identical. `PlatformServices/NotificationService.Android.cs` calls `Permissions.PostNotifications` (now via `Microsoft.Maui.ApplicationModel`). Receivers live in `Platforms/Android/BroadcastReceivers/`. The receiver reads the same `DR_Settings` `SharedPreferences` keys that `SettingsService` writes through `Preferences.Default`. |
| Permissions | `Xamarin.Essentials.Permissions` + custom `BasePlatformPermission` for `POST_NOTIFICATIONS`. | `Microsoft.Maui.ApplicationModel.Permissions.PostNotifications` (built in to MAUI Essentials). |
| DI | `Microsoft.Extensions.Hosting` generic host. | MAUI's built‑in `MauiAppBuilder` (which uses the same `Microsoft.Extensions.DependencyInjection`). |

### 4.5 Manifests

* `Platforms/Android/AndroidManifest.xml` keeps the same five permissions as Xamarin (`ACCESS_NETWORK_STATE`, `INTERNET`, `VIBRATE`, `RECEIVE_BOOT_COMPLETED`, `POST_NOTIFICATIONS`).
* iOS / macCatalyst: minimum 15.0 (was 9.0 on Xamarin). `Info.plist` registers the same FontAwesome OTFs.
* macCatalyst is genuinely a new target (the Xamarin original was iPhone+iPad only).

### 4.6 Net‑new in MAUI vs. original

* macCatalyst target.
* Handler‑based picker customisation (`AppendToMapping`) replacing the two custom renderer files.
* Picker chrome style updates (no border, inline date picker on iOS, cleared background on Android) live in *one* place rather than per‑renderer.
* `Resources/Splash/splash.svg` + `Resources/AppIcon/appicon.svg` — proper MAUI single‑project icon/splash pipeline.
* Default MAUI palette and typography styles overlay the original blue theme. Visually no redesign, but the resource dictionary surface is broader.

### 4.7 Things lost / different in MAUI vs. original

* `IPreferences` / `IShare` interfaces are **no longer injected**; the cross‑platform `SettingsService` / `ShareService` now talk to the static `Preferences.Default` / `Share.Default` directly. This removes a unit‑test seam that the original carried via `Xamarin.Essentials.Interfaces`.
* The original Xamarin `MigrateOldPreferences` would migrate from a *different* preferences container; the MAUI implementation calls `Preferences.Default.Clear()` after re‑setting the values, which is semantically the same migration but worth re‑reading on the next Android version bump.
* The MAUI head no longer has any custom renderers (renderers are deprecated in MAUI), and the `Visual="Material"` Shell attribute still appears on `AppShell.xaml`, but in MAUI this is a no‑op rather than an active styling mechanism — Android styling is now driven entirely by `Styles.xaml` + the OS Material 3 theme.

---

## 5. Uno Platform port (`heads/DailyReflection.Uno/`)

The Uno port was generated by following `prompt.md` (see `genlog.md` for the full migration log; `genlog.md` itself is the verbatim transcript of the migration session, including each Uno Docs MCP query that backed a control‑mapping decision). The port keeps the same shared layer, the same view models, the same converters' *behaviour*, and the same three‑tab UX, but rebuilds the platform glue on the Uno.Extensions stack.

### 5.1 Project shape

* `Uno.Sdk` 6.4.58 in `heads/DailyReflection.Uno/global.json` (the repo‑root `global.json` pins 6.5.31; the Uno head's local `global.json` overrides).
* `TargetFrameworks = net10.0-android;net10.0-ios;net10.0-desktop`. **Desktop is new** — Skia‑rendered Windows / macOS / Linux. There is no macCatalyst target.
* `<UnoFeatures>SkiaRenderer; Hosting; Toolkit; Configuration; Navigation;</UnoFeatures>` — pulls in `Uno.Extensions.Hosting`, `Uno.Extensions.Configuration`, `Uno.Extensions.Navigation`, and `Uno.Toolkit.UI`.
* `ApplicationId = com.kazo0.dailyreflectionuno`.
* Central package management via `Directory.Packages.props`: `CommunityToolkit.Mvvm` 8.4.0, `NodaTime` 3.0.3, `sqlite-net-pcl` 1.10.196‑beta, `SQLitePCLRaw.bundle_e_sqlite3` 2.1.10.

### 5.2 App composition

`App.xaml.cs` (recently re‑read to confirm) chains:

```csharp
var builder = this.CreateBuilder(args)
    .UseToolkitNavigation()
    .Configure(host => host
        .UseConfiguration(c => c.EmbeddedSource<App>())
        .ConfigureServices((ctx, s) =>
        {
            s.AddPlatformServices();
            s.AddPresentationDependencies();
            s.AddTransient<MainPage>();
            s.AddTransient<DailyReflectionPage>();
            s.AddTransient<SettingsPage>();
            s.AddTransient<SobrietyTimePage>();
        })
        .UseNavigation(RegisterRoutes));
MainWindow = builder.Window;
MainWindow.SetWindowIcon();
Host = await builder.NavigateAsync<MainPage>();
```

`RegisterRoutes` declares one root route (`""` → `MainPage`) with three nested routes (`Reflection` (default), `SoberTime`, `Settings`). The pages are registered as Transient (in contrast to MAUI's `AddPages()` which registers them Singleton); the ViewModels remain Singleton via the shared `AddAllSubclassesOf<ViewModelBase>` call.

### 5.3 UI APIs (vs. MAUI)

| Concern | MAUI | Uno |
|---|---|---|
| Page base | `Microsoft.Maui.Controls.ContentPage`. | `Microsoft.UI.Xaml.Controls.Page` (WinUI). |
| Navigation root | `Shell` + `<TabBar>` + `<Tab>`. | `Uno.Toolkit.UI.TabBar` + `Uno.Extensions.Navigation` *regions*. `MainPage.xaml` declares an outer `Grid` with `uen:Region.Attached="True"`, an inner content `Grid` with `uen:Region.Navigator="Visibility"`, and three `<utu:TabBarItem uen:Region.Name="…">` entries that map to the nested routes. There is no `Frame` and no Shell. |
| Tab content lifetime | Each tab page held by Shell in memory. | The `Visibility` region navigator toggles `Visibility` on the materialised tab, so all three tab pages stay in memory (similar lifetime). |
| Toolbar | `ContentPage.ToolbarItems`. | `CommandBar` + `AppBarButton` (with `CommandBar.SecondaryCommands` for the Share overflow). The page title moves into `CommandBar.Content` because there is no Shell title to bind. |
| Date selection | Hidden `CustomDatePicker` focused on toolbar tap. | `AppBarButton.Flyout = DatePickerFlyout` (modal flyout) with a `DatePicked` handler. |
| Settings rows | `TableView` / `SwitchCell` / `TextCell`. | Plain `Grid`s styled to look like cards (using `CardBackgroundFillColorDefaultBrush`). Tapping reveals a previously‑collapsed `TimePicker` / `CalendarDatePicker` / `ComboBox`. **There is no `TableView` equivalent in WinUI/Uno**, so the layout was hand‑rolled. |
| Toggle | `SwitchCell`. | `ToggleSwitch`. |
| Picker | `Picker` + `SoberTimeDisplayEnumConverter`. | `ComboBox` bound directly to the enum list. |
| Date picker | `DatePicker` (custom subclass for renderer/handler hooks). | `CalendarDatePicker` (no customisation needed). |
| Time picker | `TimePicker` (custom subclass). | `TimePicker` (WinUI primitive). |
| Activity indicator | `ActivityIndicator`. | `ProgressRing`. |
| `ScrollView` | `ScrollView`. | `ScrollViewer`. |
| `StackLayout` | `StackLayout`. | `StackPanel`. |
| `Label` | `Label` with `TextType="Html"` for the `<i>` reflection markup. | `TextBlock` + a new **`HtmlToTextConverter`** that strips tags via `StringExtensions.StripHtml`. The reading is rendered in plain text with `FontStyle="Italic"` applied at the control level — the original `<i>` markup no longer flows through. |
| Bindings | `{Binding}` (with classic data binding). | `{x:Bind …, Mode=OneWay}` (compiled bindings) with helper methods on the page (`FormatPageDate`, `FormatNotificationTime`, `ShowContent`) for inline formatting that classic binding could do via `StringFormat=`. |
| Converters | 9 converters. | 11 converters: keeps all 9 (with WinUI `IValueConverter`), adds **`NullToBoolConverter`**, **`BoolToVisibilityConverter`** (with an `Invert` property), **`AllFalseBoolToVisibilityConverter`**, and **`HtmlToTextConverter`**. The MAUI `IsDisplayPreferenceToBoolConverter` is preserved by name. The Xamarin Community Toolkit converters that were previously merged into App.xaml (`IntToBoolConverter`, `IsNotNullOrEmptyConverter`) become first‑class types in `Converters/`. |
| FontImage | `FontImage` resource (`FaSolidFont` etc.). | `FontIcon` element with `FontFamily="{StaticResource FontAwesomeSolid}"` + `Glyph="…"`; the FontFamily references are declared in `App.xaml`. |
| Theme | `AppThemeBinding` + Material visual on Shell. | `ThemeResource` lookups against `XamlControlsResources` (WinUI defaults) merged with `Uno.Toolkit.ToolkitResources` and the local `Colors.xaml`/`Styles.xaml`. The `#1976D2` primary is preserved. |

### 5.4 Non‑UI APIs (vs. MAUI)

| Concern | MAUI head | Uno head |
|---|---|---|
| Preferences | `Microsoft.Maui.Storage.Preferences.Default`. | `Windows.Storage.ApplicationData.Current.LocalSettings` in `PlatformServices/SettingsService.cs`. **`DateTime` is round‑tripped via `DateTime.ToBinary()` / `DateTime.FromBinary()`** because `LocalSettings` only persists a closed list of primitives. **Android also writes through to `SharedPreferences`** in `SettingsService.Android.cs` (`partial void MirrorSet<T>`) so that `DailyNotificationReceiver` / `WakeUpAlarmReceiver` can read settings without booting `ApplicationData`. |
| Share | `Share.Default.RequestAsync`. | `Windows.ApplicationModel.DataTransfer.DataTransferManager.GetForCurrentView()` + `DataRequested` handler. **`DataTransferManager.IsSupported()` is checked first; on unsupported platforms (notably Linux X11 and Android in some contexts) `ShareText` returns `Task.CompletedTask`** rather than throwing. |
| Notifications (iOS) | `UNUserNotificationCenter` in head. | Same APIs in `PlatformServices/NotificationService.iOS.cs`. |
| Notifications (Android) | `AlarmManager` + `DailyNotificationReceiver` + `WakeUpAlarmReceiver`. | Same APIs in `PlatformServices/NotificationService.Android.cs` + receivers under `Platforms/Android/BroadcastReceivers/`. The receivers read the mirrored Android `SharedPreferences` instead of `Preferences.Default`. |
| Notifications (desktop) | (no desktop target) | `PlatformServices/NotificationService.cs` (the un‑guarded file) is a **no‑op stub** — `CanScheduleNotifications` returns false, `TryScheduleDailyNotification` returns false. This is the single biggest behavioural gap in the desktop port. |
| Permissions | `Microsoft.Maui.ApplicationModel.Permissions`. | Equivalent helpers from Uno; on Android 13+ checks `Manifest.Permission.PostNotifications`. |
| Version tracking | `VersionTracking.Track()` + version‑gated migrations in `App.OnStart`. | **Not present in the Uno port.** `App.OnLaunched` does not call `VersionTracking.Track()` and there is no `MigrateSettingsIfNeeded` / `RefreshDatabaseIfNeeded` step. New Uno installs work because the database is extracted on first run regardless, but a future schema migration would have to re‑introduce the gate. |
| DI / configuration | `MauiAppBuilder` + `builder.Configuration.AddConfiguration(...)`. | `Uno.Extensions.Hosting` `IHostBuilder` with `UseConfiguration(c => c.EmbeddedSource<App>())`. Same `appsettings.json` layout. |
| Navigation API | `Shell.Current.GoToAsync`. | `INavigator.NavigateRouteAsync` from `Uno.Extensions.Navigation` (currently unused for declarative tab switching, since `TabBar` regions handle it directly). |

### 5.5 Manifests & capabilities

* `Platforms/Android/AndroidManifest.xml` keeps the five MAUI permissions and **adds three more**: `SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `WAKE_LOCK`. These are required for `setExactAndAllowWhileIdle` on Android 12+ to behave like the original on devices where exact alarms are not pre‑granted; Xamarin and MAUI got away without them because they were targeting older API levels.
* iOS `Info.plist`: iPhone+iPad, the same FontAwesome OTFs registered, `UIApplicationSupportsIndirectInputEvents` true. No `LaunchScreen.storyboard`; Uno's splash is driven by `Assets/Splash/splash_screen.svg` via `Uno.Resizetizer`.
* `Package.appxmanifest` (Windows): targets `Windows.Universal` + `Windows.Desktop`, min 10.0.17763.0, declares `runFullTrust`. Used only for the Windows desktop target (Uno desktop on Windows).
* Desktop entry point in `Platforms/Desktop/Program.cs` chains `.UseX11().UseLinuxFrameBuffer().UseMacOS().UseWin32()` so a single `net10.0-desktop` build runs on Win32, macOS, X11, and Linux framebuffer.

### 5.6 Net‑new in Uno vs. MAUI / original

* **Desktop target** (Windows Win32 + macOS + Linux X11/framebuffer) via Skia. MAUI doesn't ship a desktop story without macCatalyst or WinUI3.
* **Compiled (`x:Bind`) bindings** throughout the views, plus formatter helpers in code‑behind (`FormatPageDate`, `FormatNotificationTime`, `FormatSoberDate`).
* `HtmlToTextConverter`, `NullToBoolConverter`, `BoolToVisibilityConverter` (with `Invert`), and `AllFalseBoolToVisibilityConverter` to cover the converter gaps WinUI doesn't ship.
* Dual settings store on Android (`LocalSettings` + `SharedPreferences` mirror) to support the broadcast receivers without dragging in `Windows.Storage` from a background context.
* Three additional Android manifest permissions for exact alarm scheduling.
* `Uno.Resizetizer` SVG asset pipeline for icon/splash.
* Localisation scaffolding (`Strings/en/Resources.resw`) — currently only carries `ApplicationName`, but the project structure is now ready for additional locales (MAUI ships none).

### 5.7 Things lost / different in Uno vs. MAUI / original

* **Desktop notifications are a no‑op.** The reflection still loads, the share sheet on Windows still works (via `DataTransferManager`), but the daily reminder feature simply does not run on `net10.0-desktop`. Linux/macOS toast notifications are not implemented.
* **No version tracking / preference migration.** `VersionTracking.Track()` and the two `*IfNeeded` migrations from `App.OnStart` did not survive the port. If the Uno head ships and the embedded database is later updated, there is no version gate to call `IDailyReflectionDatabase.RefreshDatabaseFile()`.
* **HTML rendering downgraded to text.** Where MAUI/Xamarin used `Label TextType="Html"` to render `<i>…</i>` inline, the Uno port strips all tags via `HtmlToTextConverter` and applies italic at the `TextBlock` level. The Reflection's `Title` and `Thought` fields therefore lose any inline emphasis (they are not currently italicised). This is the only feature where the visible UI differs from the original.
* **`TableView` replaced by hand‑rolled cards.** The Settings page no longer matches the OS‑native settings look the original got from `TableView Intent="Settings"`; visually it's a `Grid`/`StackPanel` mosaic. Functionally it is equivalent.
* **No `EventToCommandBehavior`.** The MAUI implementation routed `DateChangedEventArgs` to a command via `mct:EventToCommandBehavior` + `DateChangedEventArgsConverter`. The Uno port wires the `DatePickerFlyout.DatePicked` handler in code‑behind and updates the VM's `Date` directly. `DateChangedEventArgsConverter` exists in the converter folder but is no longer referenced by XAML.
* **Pages registered as Transient** rather than Singleton (MAUI's `AddPages()` uses Singleton). Combined with the `Visibility` region navigator this is fine because the views materialise once per region, but it is a behavioural difference if any page accidentally relied on singleton state.
* **Auto‑registration narrowed.** `AddPages()` (the reflection scan over `Page` subclasses) is replaced with explicit `AddTransient<MainPage>()` etc. in `App.OnLaunched`. `AddAllSubclassesOf<ViewModelBase>` is still used.
* `prompt.md`'s instructions said "no visual redesign", but the Uno port unavoidably changed three things to compile: (1) HTML stripping, (2) `TableView` → grids, (3) toolbar items → `CommandBar`. None of these are reversible without bespoke control work.

---

## 6. UI API mapping (side‑by‑side)

| Original (Xamarin.Forms) | MAUI | Uno Platform | Notes |
|---|---|---|---|
| `Shell` + `<TabBar>` | `Shell` + `<TabBar>` | `Page` (`MainPage`) hosting `utu:TabBar` + `Uno.Extensions.Navigation` regions | Visibility navigator keeps tabs in memory like Shell. |
| `ContentPage` | `ContentPage` | `Page` | Same XAML idea, different namespace. |
| `ContentPage.ToolbarItems` | same | `CommandBar` + `AppBarButton`(+ `CommandBar.SecondaryCommands`) | Page title moves into `CommandBar.Content`. |
| `TableView` (`Intent="Settings"`) + `SwitchCell` / `TextCell` | same | Hand‑rolled `Grid`s + `ToggleSwitch` / labels | No WinUI/Uno equivalent. |
| `Picker` | same | `ComboBox` | Bound to enum list directly. |
| `DatePicker` (`CustomDatePicker` subclass) | same + handler `Mapper` customisation | `CalendarDatePicker` and/or `DatePickerFlyout` | Uno picks the modal flyout for the toolbar UX. |
| `TimePicker` | same | `TimePicker` (WinUI) | No subclass needed. |
| `ActivityIndicator` | same | `ProgressRing` | |
| `ScrollView` / `StackLayout` / `Label` | same | `ScrollViewer` / `StackPanel` / `TextBlock` | |
| `Label TextType="Html"` | same | `TextBlock` + `HtmlToTextConverter` | Loses inline `<i>`. |
| `xct:EventToCommandBehavior` | `mct:EventToCommandBehavior` | (none) — code‑behind handler updates VM | |
| `FontImage` resource (Xamarin) | same | `FontIcon` element + `FontFamily` static resource | |
| `OnPlatform` colour overrides | same | `ThemeResource` + theme dictionaries | |
| App theme via `AppThemeBinding` | same | `ThemeResource` + WinUI theme dictionaries | Light/dark behaviour preserved. |
| Custom renderers (`CustomDatePickerRenderer`) | Handler `Mapper.AppendToMapping` | (not needed) | |

## 7. Non‑UI API mapping (side‑by‑side)

| Concern | Xamarin.Forms (original) | MAUI | Uno Platform |
|---|---|---|---|
| Preferences | `Xamarin.Essentials.IPreferences` (DI‑injected). | `Microsoft.Maui.Storage.Preferences.Default`. | `Windows.Storage.ApplicationData.Current.LocalSettings` (+ Android `SharedPreferences` mirror). |
| Share | `Xamarin.Essentials.IShare.RequestAsync(ShareTextRequest)`. | `Microsoft.Maui.ApplicationModel.DataTransfer.Share.Default.RequestAsync(...)`. | `Windows.ApplicationModel.DataTransfer.DataTransferManager.ShowShareUI()` + `DataRequested` event. |
| Version tracking | `Xamarin.Essentials.VersionTracking.Track()`. | `Microsoft.Maui.ApplicationModel.VersionTracking.Track()`. | **Missing.** |
| Settings migrations | `MigrateSettingsIfNeeded` + `RefreshDatabaseIfNeeded` in `App.OnStart`. | Same logic, same triggers. | **Missing.** |
| Permissions | `Xamarin.Essentials.Permissions` + custom `BasePlatformPermission` for Android 13+. | `Permissions.PostNotifications` (built in). | Equivalent Android permission helpers + `POST_NOTIFICATIONS` runtime check. |
| Notifications (Android) | `AlarmManager` + `PendingIntent` + `DailyNotificationReceiver` + `WakeUpAlarmReceiver`. | Same. | Same, plus `SCHEDULE_EXACT_ALARM` / `USE_EXACT_ALARM` / `WAKE_LOCK` permissions. |
| Notifications (iOS) | `UNUserNotificationCenter` + `UNCalendarNotificationTrigger` (daily repeat). | Same. | Same. |
| Notifications (desktop) | n/a | n/a | **No‑op.** |
| Database | `sqlite-net-pcl` 1.7.335, embedded `dailyreflections.db`, extracted to `LocalApplicationData` on first run, opened `ReadOnly`. | 1.10.196‑beta, otherwise identical. | Identical to MAUI, plus `SQLitePCLRaw.bundle_e_sqlite3` 2.1.10 explicitly referenced. |
| Date math | NodaTime `Period.Between` for sober period. | Same. | Same. |
| MVVM | `Microsoft.Toolkit.Mvvm` 7.0‑preview4 (`ObservableRecipient`, manual `[Property]`). | `CommunityToolkit.Mvvm` 8.4.0 (`[ObservableProperty]` + `[RelayCommand]` source generators). | Same as MAUI. |
| Messaging | `WeakReferenceMessenger` (3 messages). | Same. | Same. |
| DI | `Microsoft.Extensions.Hosting`. | `MauiAppBuilder`. | `Uno.Extensions.Hosting` `IHostBuilder`. |
| Configuration | `EmbeddedFileProvider` over `appsettings.json`. | `ConfigurationBuilder().AddJsonStream(manifestStream)`. | `UseConfiguration(c => c.EmbeddedSource<App>())`. |

---

## 8. Gaps and new features per migration

### 8.1 MAUI port

**Gaps vs. original:** none of substance. Behaviour parity is preserved.

**Behavioural changes to be aware of:**

* `IPreferences` / `IShare` are no longer registered in DI. Tests that previously mocked them must now mock `ISettingsService` / `IShareService` instead. The shared `DailyReflection.Presentation.Tests` already does this, so the existing test suite still applies, but the static `Preferences.Default` / `Share.Default` callers in the head are not unit‑testable without a MAUI host.
* macCatalyst is a new platform target; nothing currently differentiates it from iOS at runtime, but it inherits all of iOS's behaviour through shared partials.
* Custom renderers are gone. Picker chrome lives in `App.xaml.cs` handler customisation. If you previously relied on `OnElementChanged` overrides, port them to `Mapper.AppendToMapping`.

**Net‑new features:**

* macCatalyst target.
* MAUI single‑project resource pipeline (`Resources/AppIcon`, `Resources/Splash`, `Resources/Fonts`, `Resources/Images`).
* MAUI Community Toolkit `EventToCommandBehavior` (replacing the Xamarin Community Toolkit one).
* Default MAUI palette (Primary/Secondary/Tertiary/Gray100‑Gray950) merged on top of the original blue.

### 8.2 Uno Platform port

**Gaps vs. MAUI / original:**

1. **Desktop notifications** — `NotificationService.cs` (un‑guarded) is a no‑op. The Settings UI on desktop will toggle, but no reminder will ever fire. This is the biggest open item.
2. **Version tracking + preference / database migrations** — neither `VersionTracking.Track()` nor the `MigrateSettingsIfNeeded` / `RefreshDatabaseIfNeeded` calls are present. Any future schema bump will need this re‑plumbed.
3. **HTML rendering** — `<i>…</i>` markup in the database is stripped via `HtmlToTextConverter`. Reading is italicised at the control level, but inline emphasis in the Title/Thought fields is lost. WinUI has no built‑in HTML label; closing the gap means a custom `RichTextBlock` parser or storing pre‑italicised plain text in the DB.
4. **`TableView` look** — the Settings page is a hand‑built card list; it does not match the OS‑native settings appearance the original got for free on iOS / Android.
5. **macCatalyst target** — not produced by the Uno port. macOS desktop is reachable via `net10.0-desktop` (Skia), but that does not run as a Catalyst app.
6. **`EventToCommandBehavior`** on `DatePickerFlyout` — replaced with a `DatePicked` handler in code‑behind. Functional, but adds a code‑behind dependency that the original avoided.

**Behavioural changes to be aware of:**

* Pages are `AddTransient` rather than Singleton. Region navigation still keeps the materialised view alive, but the registration semantics differ from MAUI.
* `DateTime` settings round‑trip through `DateTime.ToBinary()` because of `LocalSettings` constraints. This is fine on its own; it does mean preferences written by the Uno port are not byte‑for‑byte compatible with the MAUI port's `Preferences.Default` storage on the same device.
* On Android, settings are mirrored to a `SharedPreferences` file named `DR_Settings` (the constant is shared via `PreferenceConstants.PreferenceSharedName`) so that the broadcast receivers can read them off the UI thread / outside the app process.
* `AndroidManifest.xml` carries three additional permissions (`SCHEDULE_EXACT_ALARM`, `USE_EXACT_ALARM`, `WAKE_LOCK`) that MAUI / Xamarin did not declare.

**Net‑new features:**

* Desktop target (`net10.0-desktop` running on Win32 / macOS / X11 / Linux framebuffer).
* Skia rendering pipeline (consistent visual behaviour across all targets that use it).
* `Uno.Toolkit.UI` controls (`TabBar`, `SafeArea`).
* `Uno.Extensions.Hosting` + `Uno.Extensions.Navigation` + `Uno.Extensions.Configuration` stack — the same kind of generic‑host wiring the Xamarin original used, but native to Uno.
* Compiled `x:Bind` bindings throughout, with helper methods for inline formatting.
* Three new converters (`NullToBoolConverter`, `BoolToVisibilityConverter`, `AllFalseBoolToVisibilityConverter`) and `HtmlToTextConverter` for stripping DB markup.
* Localisation scaffolding (`Strings/en/Resources.resw`).
* `Uno.Resizetizer` SVG icon/splash pipeline.

---

## 9. Recommendations / outstanding work

The items below are not asks of this report; they are the concrete deltas that would bring each port to full parity with the original.

**Uno port — to reach feature parity with MAUI:**

1. Implement desktop notifications (Windows toast notifications via `Microsoft.Toolkit.Uwp.Notifications` or platform‑specific schedulers; macOS via `NSUserNotification`; Linux via `libnotify`). At minimum, surface a clear "notifications not supported on this platform" message on desktop targets where the gap will remain.
2. Re‑introduce `VersionTracking.Track()` + the two `*IfNeeded` migrations in `App.OnLaunched`. Uno does not expose the MAUI/Essentials `VersionTracking` directly, so this likely needs a small wrapper persisting build/version into `LocalSettings`.
3. Decide whether HTML emphasis in the DB matters; if it does, replace `HtmlToTextConverter` + `TextBlock` with a `RichTextBlock` and a small inline‑markup parser.
4. Move the `DatePickerFlyout.DatePicked` wiring from code‑behind onto a behaviour or `EventToCommand` from `Uno.Toolkit.UI` to match the original's pure‑XAML pattern.

**MAUI port — opportunistic improvements:**

1. The macCatalyst head currently inherits iOS verbatim; consider adding a Catalyst‑only window size hint or menu wiring to make it feel like a Mac app rather than an iPad app on macOS.
2. The `Visual="Material"` attribute on `AppShell.xaml` is a no‑op in MAUI; either remove it or replace with explicit Material 3 theming if that is the desired look.
3. Consider re‑introducing `IPreferences` / `IShare` as DI‑registered abstractions (or using the Uno port's split between cross‑platform interface in `Services` and per‑head implementation) so that the head's `SettingsService` / `ShareService` are unit‑testable again.

---

*Report generated by reading the three implementations directly and cross‑checking with `heads/DailyReflection.Uno/genlog.md`. The Avalonia head was not analysed.*
