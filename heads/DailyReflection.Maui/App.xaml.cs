using DailyReflection.Core.Constants;
using DailyReflection.Data.Databases;
using DailyReflection.Services.Notification;
using DailyReflection.Services.Settings;
using DailyReflection.Views;

#if IOS
using UIKit;
#endif

namespace DailyReflection;

public partial class App : Application
{
	public App()
	{
		InitializeComponent();

		Microsoft.Maui.Handlers.DatePickerHandler.Mapper.AppendToMapping("dfg", (handler, view) =>
			{
#if IOS
				handler.PlatformView.Layer.BorderWidth = 0;
				handler.PlatformView.BorderStyle = UITextBorderStyle.None;
				var picker = (handler.PlatformView.InputView as UIDatePicker);
				picker.PreferredDatePickerStyle = UIDatePickerStyle.Inline;
				picker.Mode = UIDatePickerMode.DateAndTime;
#elif ANDROID
				handler.PlatformView.Background = null;
#endif
			});

		Microsoft.Maui.Handlers.TimePickerHandler.Mapper.AppendToMapping("timepicker", (handler, view) =>
		{
#if IOS
			handler.PlatformView.Layer.BorderWidth = 0;
			handler.PlatformView.BorderStyle = UITextBorderStyle.None;
			var picker = (handler.PlatformView.InputView as UIDatePicker);
			picker.PreferredDatePickerStyle = UIDatePickerStyle.Wheels;
#elif ANDROID
			handler.PlatformView.Background = null;
#endif
		});

		VersionTracking.Track();
	}

	protected override Window CreateWindow(IActivationState? activationState)
	{
		return new Window(MauiProgram.ServiceProvider?.GetService<AppShell>());
	}

	private static async Task MigrateSettingsIfNeeded()
	{
		if (VersionTracking.IsFirstLaunchForCurrentBuild &&
			GetBuildVersion(VersionTracking.CurrentVersion) >= VersionConstants.NewSettingsVersion &&
			GetBuildVersion(VersionTracking.CurrentBuild) >= VersionConstants.NewSettingsBuild &&
			VersionTracking.PreviousBuild == null &&
			VersionTracking.PreviousVersion == null &&
			(Preferences.ContainsKey(PreferenceConstants.SoberDate) ||
			Preferences.ContainsKey(PreferenceConstants.NotificationsEnabled)))
		{
			var settingsService = MauiProgram.ServiceProvider?.GetService<ISettingsService>();
			settingsService.MigrateOldPreferences();

			if (settingsService.Get(PreferenceConstants.NotificationsEnabled, false))
			{
				var notifService = MauiProgram.ServiceProvider?.GetService<INotificationService>();
				var notifTime = settingsService.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);
				await notifService.TryScheduleDailyNotification(notifTime);
			}
		}
	}

	private static async Task RefreshDatabaseIfNeeded()
	{
		if (!VersionTracking.IsFirstLaunchEver &&
			VersionTracking.IsFirstLaunchForCurrentBuild &&
			VersionTracking.IsFirstLaunchForCurrentVersion &&
			GetBuildVersion(VersionTracking.CurrentVersion) >= VersionConstants.RefreshDatabaseVersion &&
			GetBuildVersion(VersionTracking.CurrentBuild) >= VersionConstants.RefreshDatabaseBuild &&
			GetBuildVersion(VersionTracking.PreviousBuild) < VersionConstants.RefreshDatabaseBuild &&
			GetBuildVersion(VersionTracking.PreviousVersion) < VersionConstants.RefreshDatabaseVersion)
		{
			var database = MauiProgram.ServiceProvider?.GetService<IDailyReflectionDatabase>();
			await database.RefreshDatabaseFile();
		}
	}

	private static double GetBuildVersion(string buildVersion)
	{
		if (double.TryParse(buildVersion, out var dblBuildVersion))
		{
			return dblBuildVersion;
		}

		return 0d;
	}

	protected override async void OnStart()
	{
		await MigrateSettingsIfNeeded();
		await RefreshDatabaseIfNeeded();
	}

	protected override void OnSleep()
	{
	}

	protected override void OnResume()
	{
	}

}
