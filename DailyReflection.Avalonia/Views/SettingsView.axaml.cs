using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using CommunityToolkit.Mvvm.Messaging;
using DailyReflection.Presentation.Messages;
using DailyReflection.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System.Threading.Tasks;

namespace DailyReflection.Avalonia.Views;

public partial class SettingsView : UserControl
{
    private SettingsViewModel? _viewModel;

    public SettingsView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_viewModel == null)
        {
            _viewModel = App.ServiceProvider?.GetService<SettingsViewModel>();
            DataContext = _viewModel;

            // Register for notification permission messages
            WeakReferenceMessenger.Default.Register<SettingsView, NotificationPermissionRequestMessage>(this, async (r, m) =>
            {
                // For Avalonia desktop, we'll auto-approve notification permissions
                // In a real app, you might show a dialog here
                m.Reply(Task.FromResult(true));
            });
        }

        if (_viewModel is ViewModelBase vm)
        {
            vm.IsActive = true;
        }
    }

    protected override void OnUnloaded(RoutedEventArgs e)
    {
        base.OnUnloaded(e);

        if (_viewModel is ViewModelBase vm)
        {
            vm.IsActive = false;
        }
    }

    private void NotificationTime_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Show time picker - for now we'll make it visible temporarily
        // In a full implementation, you'd use a popup or dialog
        TimePicker.IsVisible = !TimePicker.IsVisible;
    }

    private void SoberTimeDisplay_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SoberTimeDisplayPicker.IsVisible = !SoberTimeDisplayPicker.IsVisible;
        if (SoberTimeDisplayPicker.IsVisible)
        {
            SoberTimeDisplayPicker.IsDropDownOpen = true;
        }
    }

    private void SoberDate_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        SoberDatePicker.IsVisible = !SoberDatePicker.IsVisible;
        if (SoberDatePicker.IsVisible)
        {
            SoberDatePicker.IsDropDownOpen = true;
        }
    }
}
