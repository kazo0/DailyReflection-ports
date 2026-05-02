using DailyReflection.Services.Notification;
using System;
using System.Threading.Tasks;

namespace DailyReflection.Avalonia.Services;

/// <summary>
/// Avalonia implementation of INotificationService.
/// Note: Full notification functionality requires platform-specific implementation.
/// This is a stub implementation for desktop platforms.
/// </summary>
public class NotificationService : INotificationService
{
    public Task<bool> CanScheduleNotifications()
    {
        // Desktop platforms generally don't have system notifications via Avalonia
        return Task.FromResult(false);
    }

    public void CancelNotifications()
    {
        // No-op for desktop
    }

    public void ShowNotificationSettings()
    {
        // No-op for desktop
    }

    public Task<bool> TryScheduleDailyNotification(DateTime notificationTime, bool shouldRequestPermission = true)
    {
        // No-op for desktop - return false to indicate notifications couldn't be scheduled
        return Task.FromResult(false);
    }
}
