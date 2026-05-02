using DailyReflection.Core.Constants;
using DailyReflection.Services.Settings;

namespace DailyReflection.PlatformServices;

public class SettingsService : ISettingsService
{
	public T Get<T>(string key, T defaultValue) => Preferences.Default.Get(key, defaultValue);

	public void Set<T>(string key, T value) => Preferences.Default.Set(key, value);

	public void MigrateOldPreferences()
	{
		var prefs = Preferences.Default;
		var soberDate = prefs.Get(PreferenceConstants.SoberDate, DateTime.Today);
		var notifsEnabled = prefs.Get(PreferenceConstants.NotificationsEnabled, false);
		var notifTime = prefs.Get(PreferenceConstants.NotificationTime, DateTime.MinValue);

		Set(PreferenceConstants.SoberDate, soberDate);
		Set(PreferenceConstants.NotificationsEnabled, notifsEnabled);
		Set(PreferenceConstants.NotificationTime, notifTime);

		prefs.Clear();
	}
}
