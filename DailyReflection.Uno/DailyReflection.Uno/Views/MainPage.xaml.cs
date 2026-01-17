using Microsoft.UI.Xaml.Controls;

namespace DailyReflection.Views;

/// <summary>
/// Main page hosting TabBar navigation with Region-based navigation.
/// Migrated from MAUI Shell to Uno Platform TabBar with Uno.Extensions.Navigation.
/// Based on Uno Platform Docs: NavigationShell.md, HowTo-UseTabBar.md, DefineRegions.md
/// 
/// Navigation is handled automatically by the Uno Navigation Extensions:
/// - uen:Region.Attached="True" enables region navigation on Grid and TabBar
/// - uen:Region.Navigator="Visibility" specifies the content area for visibility-based navigation
/// - uen:Region.Name on TabBarItems links to routes defined in App.xaml.cs RegisterRoutes
/// </summary>
public sealed partial class MainPage : Page
{
    public MainPage()
    {
        this.InitializeComponent();
    }
}
