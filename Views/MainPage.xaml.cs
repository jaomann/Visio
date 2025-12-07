namespace Visio.Views;

using Visio.ViewModels;

/// <summary>
/// Página principal do aplicativo
/// </summary>
public partial class MainPage : ContentPage
{
    public MainPage(MainViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }
}
