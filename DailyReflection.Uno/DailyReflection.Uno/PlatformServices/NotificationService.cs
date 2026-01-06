using DailyReflection.Services.Notification;

namespace DailyReflection.PlatformServices;

/// <summary>
/// Stub notification service implementation.
/// Local notifications in Uno Platform require platform-specific implementations.
/// This is a placeholder that can be extended with platform-specific code.
/// </summary>
public class NotificationService : INotificationService
{
    public Task<bool> CanScheduleNotifications()
    {
        // Return false by default - platform-specific implementations should override
        return Task.FromResult(false);
    }

    public void CancelNotifications()
    {
        // Platform-specific implementation required
    }

    public void ShowNotificationSettings()
    {
        // Platform-specific implementation required
    }

    public Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true)
    {
        // Platform-specific implementation required
        return Task.FromResult(false);
    }
}
