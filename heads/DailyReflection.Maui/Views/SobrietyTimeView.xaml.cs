using DailyReflection.Presentation.ViewModels;




namespace DailyReflection.Views;

[XamlCompilation(XamlCompilationOptions.Compile)]
public partial class SobrietyTimeView : ContentPage
{
	public SobrietyTimeView()
	{
		InitializeComponent();
		BindingContext = MauiProgram.ServiceProvider?.GetService<SobrietyTimeViewModel>();
	}

	protected override void OnAppearing()
	{
		base.OnAppearing();

		if (BindingContext is ViewModelBase vm)
		{
			vm.IsActive = true;
		}
	}
}