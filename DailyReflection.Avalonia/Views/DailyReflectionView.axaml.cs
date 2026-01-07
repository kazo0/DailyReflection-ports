using Avalonia.Controls;
using Avalonia.Interactivity;
using DailyReflection.Presentation.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace DailyReflection.Avalonia.Views;

public partial class DailyReflectionView : UserControl
{
    private DailyReflectionViewModel? _viewModel;

    public DailyReflectionView()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        if (_viewModel == null)
        {
            _viewModel = App.ServiceProvider?.GetService<DailyReflectionViewModel>();
            DataContext = _viewModel;
            
            // Initialize the view model
            _ = _viewModel?.Init();
        }
    }

    private void SelectDate_Click(object? sender, RoutedEventArgs e)
    {
        // Show date picker popup
        DatePicker.IsDropDownOpen = true;
    }

    private async void DatePicker_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DatePicker.SelectedDate.HasValue && _viewModel != null)
        {
            await _viewModel.GetDailyReflectionCommand.ExecuteAsync(DatePicker.SelectedDate.Value.DateTime);
        }
    }
}
