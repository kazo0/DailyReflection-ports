using Avalonia.Controls;
using Avalonia.Interactivity;
using DailyReflection.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DailyReflection.Avalonia.Views;

public partial class SobrietyTimeView : UserControl
{
    private SobrietyTimeViewModel? _viewModel;

    public SobrietyTimeView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_viewModel == null)
        {
            _viewModel = App.ServiceProvider?.GetService<SobrietyTimeViewModel>();
            DataContext = _viewModel;
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
}
