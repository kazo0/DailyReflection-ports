using Avalonia.Controls;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DailyReflection.Avalonia.Views;
using DailyReflection.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace DailyReflection.Avalonia.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IServiceProvider _serviceProvider;

    [ObservableProperty]
    private UserControl? _currentView;

    private DailyReflectionView? _reflectionView;
    private SobrietyTimeView? _sobrietyTimeView;
    private SettingsView? _settingsView;

    public MainViewModel(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
        
        // Start with reflection view
        Navigate("Reflection");
    }

    [RelayCommand]
    private void Navigate(string destination)
    {
        CurrentView = destination switch
        {
            "Reflection" => GetReflectionView(),
            "SoberTime" => GetSobrietyTimeView(),
            "Settings" => GetSettingsView(),
            _ => CurrentView
        };
    }

    private DailyReflectionView GetReflectionView()
    {
        if (_reflectionView == null)
        {
            _reflectionView = new DailyReflectionView
            {
                DataContext = _serviceProvider.GetRequiredService<DailyReflectionViewModel>()
            };
        }
        return _reflectionView;
    }

    private SobrietyTimeView GetSobrietyTimeView()
    {
        if (_sobrietyTimeView == null)
        {
            _sobrietyTimeView = new SobrietyTimeView
            {
                DataContext = _serviceProvider.GetRequiredService<SobrietyTimeViewModel>()
            };
        }
        return _sobrietyTimeView;
    }

    private SettingsView GetSettingsView()
    {
        if (_settingsView == null)
        {
            _settingsView = new SettingsView
            {
                DataContext = _serviceProvider.GetRequiredService<SettingsViewModel>()
            };
        }
        return _settingsView;
    }
}
