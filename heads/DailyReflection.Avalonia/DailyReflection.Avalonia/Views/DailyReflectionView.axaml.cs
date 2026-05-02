using Avalonia.Controls;
using Avalonia.Interactivity;
using DailyReflection.Presentation.ViewModels;
using System;

namespace DailyReflection.Avalonia.Views;

public partial class DailyReflectionView : UserControl
{
    public DailyReflectionView()
    {
        InitializeComponent();
    }
    
    protected override async void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        
        if (DataContext is DailyReflectionViewModel vm)
        {
            await vm.Init();
        }
    }

    private void DatePicker_Click(object? sender, RoutedEventArgs e)
    {
        DatePickerControl.IsDropDownOpen = true;
    }
    
    private void DatePickerControl_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DataContext is DailyReflectionViewModel vm && DatePickerControl.SelectedDate.HasValue)
        {
            var selectedDate = DatePickerControl.SelectedDate.Value.Date;
            vm.GetDailyReflectionCommand.Execute(selectedDate);
        }
    }
}
