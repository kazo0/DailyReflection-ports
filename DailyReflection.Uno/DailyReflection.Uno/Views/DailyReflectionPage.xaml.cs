using DailyReflection.Presentation.ViewModels;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DailyReflection.Views;

/// <summary>
/// Page displaying the daily reflection content.
/// Migrated from MAUI DailyReflectionView.
/// </summary>
public sealed partial class DailyReflectionPage : Page
{
    public DailyReflectionViewModel ViewModel { get; }

    public DailyReflectionPage()
    {
        ViewModel = App.GetService<DailyReflectionViewModel>();
        this.InitializeComponent();
        this.DataContext = ViewModel;

        // Load reflection when page is loaded
        this.Loaded += OnLoaded;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // Initial load of the daily reflection
        if (ViewModel.GetDailyReflectionCommand.CanExecute(null))
        {
            await ViewModel.GetDailyReflectionCommand.ExecuteAsync(null);
        }
    }

    /// <summary>
    /// Shows the calendar flyout for date selection.
    /// Replaces MAUI toolbar calendar button behavior.
    /// </summary>
    private void CalendarButton_Click(object sender, RoutedEventArgs e)
    {
        DatePicker.Visibility = Visibility.Visible;
        DatePicker.IsCalendarOpen = true;
    }

    /// <summary>
    /// Handles date selection from the CalendarDatePicker.
    /// </summary>
    private async void DatePicker_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
    {
        if (args.NewDate.HasValue)
        {
            ViewModel.Date = args.NewDate.Value.DateTime;
            
            if (ViewModel.GetDailyReflectionCommand.CanExecute(null))
            {
                await ViewModel.GetDailyReflectionCommand.ExecuteAsync(null);
            }
        }
        
        // Hide the date picker after selection
        DatePicker.Visibility = Visibility.Collapsed;
    }

    /// <summary>
    /// Helper method for x:Bind to calculate content visibility.
    /// Content is visible when not loading and no error.
    /// </summary>
    public Visibility ShowContent(bool hasError, bool isLoading)
    {
        return (!hasError && !isLoading) ? Visibility.Visible : Visibility.Collapsed;
    }
}
